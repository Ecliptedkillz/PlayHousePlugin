using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LabApi.Features.Console;

namespace PlayhousePlugin.External;

public sealed class StatisticsBridge : IDisposable
{
    private readonly Uri endpoint;
    private readonly ConcurrentQueue<string> queue = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task worker;

    public StatisticsBridge(string endpoint)
    {
        this.endpoint = new Uri(endpoint, UriKind.Absolute);
        worker = Task.Run(RunAsync);
    }

    public void Enqueue(string userId, string nickname, string statistic, int amount)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        queue.Enqueue($"{Normalize(userId)}%&@&{Sanitize(nickname)}%&@&{Sanitize(statistic)}%&@&{amount}\n");
    }

    public void Dispose()
    {
        cancellation.Cancel();
        try { worker.Wait(TimeSpan.FromSeconds(2)); }
        catch (AggregateException) { }
        cancellation.Dispose();
    }

    private async Task RunAsync()
    {
        ClientWebSocket? socket = null;
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                if (!queue.TryDequeue(out string record))
                {
                    await Task.Delay(250, cancellation.Token).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    if (socket is null || socket.State != WebSocketState.Open)
                    {
                        socket?.Dispose();
                        socket = new ClientWebSocket();
                        await socket.ConnectAsync(endpoint, cancellation.Token).ConfigureAwait(false);
                        Logger.Info($"Connected to statistics bridge at {endpoint}.");
                    }

                    byte[] payload = Encoding.UTF8.GetBytes(record);
                    await socket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true,
                        cancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
                catch (Exception exception)
                {
                    queue.Enqueue(record);
                    Logger.Warn($"Statistics bridge unavailable ({exception.Message}); retrying.");
                    socket?.Dispose();
                    socket = null;
                    await Task.Delay(2000, cancellation.Token).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            if (socket?.State == WebSocketState.Open)
            {
                try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Plugin disabled", CancellationToken.None).ConfigureAwait(false); }
                catch { }
            }
            socket?.Dispose();
        }
    }

    private static string Normalize(string userId)
    {
        int separator = userId.IndexOf('@');
        return Sanitize(separator < 0 ? userId : userId.Substring(0, separator));
    }

    private static string Sanitize(string value) =>
        (value ?? string.Empty).Replace("%&@&", string.Empty).Replace("\r", " ").Replace("\n", " ");
}
