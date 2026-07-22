using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PlayhousePlugin.Webhooks;
using LabLogger = LabApi.Features.Console.Logger;

namespace PlayhousePlugin.Patches;

[HarmonyPatch]
public static class StaffChatLogPatch
{
    private const string StaffChatPrefix = "[AC ";

    // Per-user rate limit.
    private const int MaximumMessagesPerUser = 5;
    private static readonly TimeSpan UserRateLimitWindow =
        TimeSpan.FromSeconds(10);

    // Global rate limit protects the webhook from coordinated spam.
    private const int MaximumGlobalMessages = 20;
    private static readonly TimeSpan GlobalRateLimitWindow =
        TimeSpan.FromSeconds(10);

    // Prevent the same message from being logged repeatedly.
    private static readonly TimeSpan DuplicateMessageWindow =
        TimeSpan.FromSeconds(3);

    // Periodically remove inactive rate-limit entries.
    private static readonly TimeSpan CleanupInterval =
        TimeSpan.FromMinutes(5);

    private static readonly object RateLimitLock = new();

    private static readonly Dictionary<string, RateLimitEntry>
        UserRateLimits =
            new(StringComparer.OrdinalIgnoreCase);

    private static readonly Queue<DateTime> GlobalMessageTimes = new();

    private static readonly Dictionary<string, DateTime>
        RecentMessages =
            new(StringComparer.Ordinal);

    private static DateTime lastCleanupUtc = DateTime.UtcNow;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        Type? serverLogsType =
            AccessTools.TypeByName("ServerLogs");

        if (serverLogsType is null)
        {
            LabLogger.Error(
                "StaffChatLogPatch could not find the ServerLogs type.");

            return Array.Empty<MethodBase>();
        }

        MethodBase[] methods = serverLogsType
            .GetMethods(
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)
            .Where(method =>
                method.Name.Equals(
                    "AddLog",
                    StringComparison.Ordinal))
            .Cast<MethodBase>()
            .ToArray();

        LabLogger.Info(
            $"StaffChatLogPatch found {methods.Length} " +
            "ServerLogs.AddLog overload(s).");

        return methods;
    }

    private static void Prefix(object[] __args)
    {
        try
        {
            if (__args is null || __args.Length == 0)
                return;

            string[] stringArguments = __args
                .OfType<string>()
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .ToArray();

            if (TryFindFormattedMessage(
                    stringArguments,
                    out string formattedMessage))
            {
                ProcessFormattedMessage(formattedMessage);
                return;
            }

            TryProcessSeparateArguments(
                __args,
                stringArguments);
        }
        catch (Exception exception)
        {
            LabLogger.Error(
                $"Unable to process a possible staff-chat log: " +
                $"{exception}");
        }
    }

    private static bool TryFindFormattedMessage(
        IEnumerable<string> arguments,
        out string message)
    {
        message = arguments.FirstOrDefault(value =>
            value.TrimStart().StartsWith(
                StaffChatPrefix,
                StringComparison.Ordinal)) ?? string.Empty;

        return !string.IsNullOrWhiteSpace(message);
    }

    private static void ProcessFormattedMessage(
        string logMessage)
    {
        logMessage = logMessage.Trim();

        int headerEnd = logMessage.IndexOf(
            "] ",
            StringComparison.Ordinal);

        if (headerEnd < 0)
            return;

        string senderSection = logMessage.Substring(
            StaffChatPrefix.Length,
            headerEnd - StaffChatPrefix.Length);

        string message = logMessage
            .Substring(headerEnd + 2)
            .Trim();

        if (string.IsNullOrWhiteSpace(message))
            return;

        ParseSender(
            senderSection,
            out string senderName,
            out string senderUserId);

        SendToWebhook(
            senderName,
            senderUserId,
            message);
    }

    private static void TryProcessSeparateArguments(
        object[] arguments,
        string[] stringArguments)
    {
        bool looksLikeStaffChat = arguments.Any(argument =>
        {
            if (argument is null)
                return false;

            string value = argument.ToString() ?? string.Empty;

            return value.Equals(
                       "AC",
                       StringComparison.OrdinalIgnoreCase) ||
                   value.IndexOf(
                       "AdminChat",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf(
                       "StaffChat",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        });

        if (!looksLikeStaffChat)
            return;

        if (stringArguments.Length == 0)
            return;

        string message = stringArguments
            .Last()
            .Trim();

        if (string.IsNullOrWhiteSpace(message))
            return;

        string senderText = stringArguments.Length >= 2
            ? stringArguments[stringArguments.Length - 2].Trim()
            : "Unknown staff member";

        ParseSender(
            senderText,
            out string senderName,
            out string senderUserId);

        SendToWebhook(
            senderName,
            senderUserId,
            message);
    }

    private static void ParseSender(
        string senderSection,
        out string senderName,
        out string senderUserId)
    {
        senderSection = senderSection
            .Replace("[AC ", string.Empty)
            .Trim()
            .TrimEnd(']');

        senderName = senderSection;
        senderUserId = string.Empty;

        int userIdStart = senderSection.LastIndexOf(
            " (",
            StringComparison.Ordinal);

        if (userIdStart < 0 ||
            !senderSection.EndsWith(
                ")",
                StringComparison.Ordinal))
        {
            return;
        }

        senderName = senderSection
            .Substring(0, userIdStart)
            .Trim();

        senderUserId = senderSection
            .Substring(
                userIdStart + 2,
                senderSection.Length - userIdStart - 3)
            .Trim();
    }

    private static void SendToWebhook(
        string senderName,
        string senderUserId,
        string message)
    {
        if (!TryPassRateLimit(
                senderName,
                senderUserId,
                message,
                out string rejectionReason))
        {
            LabLogger.Warn(
                $"Staff-chat webhook message was rate limited. " +
                $"Sender='{senderName}', Reason='{rejectionReason}'.");

            return;
        }

        WebhookService? webhook =
            PlayhousePlugin.Instance?.Webhooks;

        if (webhook is null)
        {
            LabLogger.Warn(
                "Staff chat was detected, but WebhookService " +
                "was unavailable.");

            return;
        }

        string safeSender =
            EscapeDiscord(senderName);

        string safeUserId =
            EscapeDiscord(senderUserId);

        string safeMessage =
            EscapeDiscord(message);

        string webhookContent =
            string.IsNullOrWhiteSpace(senderUserId)
                ? $"**[STAFF CHAT] {safeSender}:** {safeMessage}"
                : $"**[STAFF CHAT] {safeSender}** `{safeUserId}`: " +
                  safeMessage;

        LabLogger.Info(
            $"Staff chat captured for webhook. " +
            $"Sender='{senderName}', Message='{message}'.");

        webhook.Enqueue(
            WebhookDestination.StaffChat,
            webhookContent);
    }

    private static bool TryPassRateLimit(
        string senderName,
        string senderUserId,
        string message,
        out string rejectionReason)
    {
        rejectionReason = string.Empty;

        DateTime nowUtc = DateTime.UtcNow;

        string senderKey =
            !string.IsNullOrWhiteSpace(senderUserId)
                ? senderUserId.Trim()
                : senderName.Trim();

        if (string.IsNullOrWhiteSpace(senderKey))
            senderKey = "unknown";

        string duplicateKey =
            senderKey + "\n" + message.Trim();

        lock (RateLimitLock)
        {
            CleanupExpiredEntries(nowUtc);

            // Block exact duplicate messages for a brief period.
            if (RecentMessages.TryGetValue(
                    duplicateKey,
                    out DateTime previousMessageTime) &&
                nowUtc - previousMessageTime <
                DuplicateMessageWindow)
            {
                rejectionReason = "duplicate message";
                return false;
            }

            // Remove expired global timestamps.
            while (GlobalMessageTimes.Count > 0 &&
                   nowUtc - GlobalMessageTimes.Peek() >=
                   GlobalRateLimitWindow)
            {
                GlobalMessageTimes.Dequeue();
            }

            if (GlobalMessageTimes.Count >=
                MaximumGlobalMessages)
            {
                rejectionReason =
                    $"global limit of {MaximumGlobalMessages} " +
                    $"messages per " +
                    $"{GlobalRateLimitWindow.TotalSeconds:0} seconds";

                return false;
            }

            if (!UserRateLimits.TryGetValue(
                    senderKey,
                    out RateLimitEntry? entry))
            {
                entry = new RateLimitEntry();
                UserRateLimits[senderKey] = entry;
            }

            while (entry.MessageTimes.Count > 0 &&
                   nowUtc - entry.MessageTimes.Peek() >=
                   UserRateLimitWindow)
            {
                entry.MessageTimes.Dequeue();
            }

            if (entry.MessageTimes.Count >=
                MaximumMessagesPerUser)
            {
                rejectionReason =
                    $"user limit of {MaximumMessagesPerUser} " +
                    $"messages per " +
                    $"{UserRateLimitWindow.TotalSeconds:0} seconds";

                return false;
            }

            entry.MessageTimes.Enqueue(nowUtc);
            entry.LastActivityUtc = nowUtc;

            GlobalMessageTimes.Enqueue(nowUtc);
            RecentMessages[duplicateKey] = nowUtc;

            return true;
        }
    }

    private static void CleanupExpiredEntries(
        DateTime nowUtc)
    {
        if (nowUtc - lastCleanupUtc < CleanupInterval)
            return;

        lastCleanupUtc = nowUtc;

        DateTime userExpiration =
            nowUtc - UserRateLimitWindow;

        string[] expiredUsers = UserRateLimits
            .Where(pair =>
                pair.Value.LastActivityUtc < userExpiration)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (string user in expiredUsers)
            UserRateLimits.Remove(user);

        DateTime duplicateExpiration =
            nowUtc - DuplicateMessageWindow;

        string[] expiredMessages = RecentMessages
            .Where(pair =>
                pair.Value < duplicateExpiration)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (string duplicate in expiredMessages)
            RecentMessages.Remove(duplicate);
    }

    private static string EscapeDiscord(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("@everyone", "@\u200Beveryone")
            .Replace("@here", "@\u200Bhere")
            .Replace("```", "``\u200B`");
    }

    private sealed class RateLimitEntry
    {
        public Queue<DateTime> MessageTimes { get; } = new();

        public DateTime LastActivityUtc { get; set; } =
            DateTime.UtcNow;
    }
}