using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LabApi.Features.Console;

namespace PlayhousePlugin.Webhooks;

public sealed class WebhookService : IDisposable
{
    private const int DiscordMaximumContentLength = 2000;
    private const int MaximumRetries = 3;

    private readonly WebhookConfig config;
    private readonly HttpClient client;
    private readonly ConcurrentQueue<QueuedMessage> queue = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task worker;

    private bool disposed;

    public WebhookService(WebhookConfig config)
    {
        this.config = config ??
            throw new ArgumentNullException(nameof(config));

        client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15),
        };

        worker = Task.Run(ProcessQueueAsync);

        Logger.Info(
            $"WebhookService started. Enabled={config.IsEnabled}.");
    }

    public void Enqueue(
        WebhookDestination destination,
        string content)
    {
        if (disposed)
        {
            Logger.Warn(
                $"Cannot enqueue {destination} webhook because " +
                "WebhookService has been disposed.");

            return;
        }

        if (!config.IsEnabled)
        {
            Logger.Debug(
                $"Webhook ignored because webhooks are disabled. " +
                $"Destination={destination}.");

            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            Logger.Warn(
                $"Webhook ignored because its content was empty. " +
                $"Destination={destination}.");

            return;
        }

        string url = GetUrl(destination);

        if (string.IsNullOrWhiteSpace(url))
        {
            Logger.Warn(
                $"Webhook URL is missing. Destination={destination}.");

            return;
        }

        foreach (string part in SplitMessage(content))
        {
            queue.Enqueue(
                new QueuedMessage(
                    destination,
                    part));
        }

        Logger.Debug(
            $"Webhook queued. Destination={destination}, " +
            $"ContentLength={content.Length}.");
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        cancellation.Cancel();

        try
        {
            worker.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException exception)
        {
            foreach (Exception innerException in
                     exception.Flatten().InnerExceptions)
            {
                if (innerException is not OperationCanceledException)
                {
                    Logger.Warn(
                        $"Webhook worker stopped with an error: " +
                        $"{innerException}");
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Warn(
                $"Unable to cleanly stop the webhook worker: {exception}");
        }

        cancellation.Dispose();
        client.Dispose();

        Logger.Info("WebhookService disposed.");
    }

    private async Task ProcessQueueAsync()
    {
        CancellationToken token = cancellation.Token;

        Logger.Info("Webhook queue worker started.");

        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!queue.TryDequeue(out QueuedMessage message))
                {
                    await Task.Delay(250, token)
                        .ConfigureAwait(false);

                    continue;
                }

                Logger.Info(
                    $"Webhook worker dequeued message. " +
                    $"Destination={message.Destination}.");

                await SendMessageAsync(message, token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Webhook worker encountered an error but will continue: " +
                    $"{exception}");

                try
                {
                    await Task.Delay(1000, token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        Logger.Info("Webhook queue worker stopped.");
    }

    private async Task SendMessageAsync(
        QueuedMessage message,
        CancellationToken token)
    {
        string url = GetUrl(message.Destination);

        Logger.Info(
            $"Attempting Discord webhook request. " +
            $"Destination={message.Destination}, " +
            $"UrlConfigured={!string.IsNullOrWhiteSpace(url)}.");

        if (string.IsNullOrWhiteSpace(url))
        {
            Logger.Warn(
                $"Webhook URL became unavailable. " +
                $"Destination={message.Destination}.");

            return;
        }

        for (int attempt = 1; attempt <= MaximumRetries; attempt++)
        {
            try
            {
                string payload = BuildPayload(message.Content);

                using var body = new StringContent(
                    payload,
                    Encoding.UTF8,
                    "application/json");

                using HttpResponseMessage response =
                    await client.PostAsync(url, body, token)
                        .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    Logger.Info(
                        $"Webhook sent successfully. " +
                        $"Destination={message.Destination}, " +
                        $"StatusCode={(int)response.StatusCode}.");

                    return;
                }

                string responseContent =
                    await response.Content.ReadAsStringAsync()
                        .ConfigureAwait(false);

                int statusCode = (int)response.StatusCode;

                Logger.Warn(
                    $"Webhook returned HTTP {statusCode} " +
                    $"({response.ReasonPhrase}). " +
                    $"Destination={message.Destination}, " +
                    $"Attempt={attempt}/{MaximumRetries}, " +
                    $"Response='{responseContent}'.");

                // Discord rate limit.
                if (statusCode == 429)
                {
                    TimeSpan retryDelay =
                        GetRetryDelay(responseContent, attempt);

                    await Task.Delay(retryDelay, token)
                        .ConfigureAwait(false);

                    continue;
                }

                // Retry temporary server errors.
                if (statusCode >= 500 &&
                    attempt < MaximumRetries)
                {
                    await Task.Delay(
                            TimeSpan.FromSeconds(attempt * 2),
                            token)
                        .ConfigureAwait(false);

                    continue;
                }

                return;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (TaskCanceledException exception)
            {
                Logger.Warn(
                    $"Webhook request timed out. " +
                    $"Destination={message.Destination}, " +
                    $"Attempt={attempt}/{MaximumRetries}: {exception.Message}");
            }
            catch (HttpRequestException exception)
            {
                Logger.Warn(
                    $"Webhook HTTP request failed. " +
                    $"Destination={message.Destination}, " +
                    $"Attempt={attempt}/{MaximumRetries}: {exception}");
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Unable to send webhook. " +
                    $"Destination={message.Destination}: {exception}");

                return;
            }

            if (attempt < MaximumRetries)
            {
                await Task.Delay(
                        TimeSpan.FromSeconds(attempt * 2),
                        token)
                    .ConfigureAwait(false);
            }
        }

        Logger.Error(
            $"Webhook failed after {MaximumRetries} attempts. " +
            $"Destination={message.Destination}.");
    }

    private static TimeSpan GetRetryDelay(
        string responseContent,
        int attempt)
    {
        if (!string.IsNullOrWhiteSpace(responseContent))
        {
            const string propertyName = "\"retry_after\"";

            int propertyIndex = responseContent.IndexOf(
                propertyName,
                StringComparison.OrdinalIgnoreCase);

            if (propertyIndex >= 0)
            {
                int colonIndex = responseContent.IndexOf(
                    ':',
                    propertyIndex + propertyName.Length);

                if (colonIndex >= 0)
                {
                    int valueStart = colonIndex + 1;

                    while (valueStart < responseContent.Length &&
                           char.IsWhiteSpace(responseContent[valueStart]))
                    {
                        valueStart++;
                    }

                    int valueEnd = valueStart;

                    while (valueEnd < responseContent.Length &&
                           (
                               char.IsDigit(responseContent[valueEnd]) ||
                               responseContent[valueEnd] == '.' ||
                               responseContent[valueEnd] == '-'
                           ))
                    {
                        valueEnd++;
                    }

                    string value = responseContent.Substring(
                        valueStart,
                        valueEnd - valueStart);

                    if (double.TryParse(
                            value,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double retryAfter) &&
                        retryAfter > 0)
                    {
                        return TimeSpan.FromSeconds(
                            Math.Min(retryAfter + 0.25, 60));
                    }
                }
            }
        }

        return TimeSpan.FromSeconds(Math.Min(attempt * 2, 10));
    }

    private string BuildPayload(string content)
    {
        var builder = new StringBuilder();

        builder.Append('{');
        builder.Append("\"content\":\"");
        builder.Append(EscapeJson(content));
        builder.Append('\"');

        if (!string.IsNullOrWhiteSpace(config.Username))
        {
            builder.Append(",\"username\":\"");
            builder.Append(EscapeJson(config.Username));
            builder.Append('\"');
        }

        if (!string.IsNullOrWhiteSpace(config.AvatarUrl))
        {
            builder.Append(",\"avatar_url\":\"");
            builder.Append(EscapeJson(config.AvatarUrl));
            builder.Append('\"');
        }

        builder.Append(",\"allowed_mentions\":{\"parse\":[]}");
        builder.Append('}');

        return builder.ToString();
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length + 16);

        foreach (char character in value)
        {
            switch (character)
            {
                case '\"':
                    builder.Append("\\\"");
                    break;

                case '\\':
                    builder.Append("\\\\");
                    break;

                case '\b':
                    builder.Append("\\b");
                    break;

                case '\f':
                    builder.Append("\\f");
                    break;

                case '\n':
                    builder.Append("\\n");
                    break;

                case '\r':
                    builder.Append("\\r");
                    break;

                case '\t':
                    builder.Append("\\t");
                    break;

                default:
                    if (character < 32)
                    {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    private string GetUrl(WebhookDestination destination)
    {
        return destination switch
        {
            WebhookDestination.GameLogs => config.GameLogsUrl,
            WebhookDestination.PvpLogs => config.PvpLogsUrl,
            WebhookDestination.StaffChat => config.StaffChatUrl,
            WebhookDestination.DetainedKills => config.DetainedKillsUrl,
            _ => string.Empty,
        };
    }

    private static string[] SplitMessage(string content)
    {
        if (content.Length <= DiscordMaximumContentLength)
            return new[] { content };

        int partCount =
            (int)Math.Ceiling(
                content.Length /
                (double)DiscordMaximumContentLength);

        var parts = new string[partCount];

        for (int index = 0; index < partCount; index++)
        {
            int start = index * DiscordMaximumContentLength;

            int length = Math.Min(
                DiscordMaximumContentLength,
                content.Length - start);

            parts[index] = content.Substring(start, length);
        }

        return parts;
    }

    private readonly struct QueuedMessage
    {
        public QueuedMessage(
            WebhookDestination destination,
            string content)
        {
            Destination = destination;
            Content = content;
        }

        public WebhookDestination Destination { get; }

        public string Content { get; }
    }
}