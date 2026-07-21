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

    private static string EscapeDiscord(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("@everyone", "@\u200Beveryone")
            .Replace("@here", "@\u200Bhere")
            .Replace("```", "``\u200B`");
    }
}