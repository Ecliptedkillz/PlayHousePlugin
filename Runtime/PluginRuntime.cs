using System;
using System.Collections.Generic;
using UnityEngine;
using LabLogger = LabApi.Features.Console.Logger;

namespace PlayhousePlugin.Runtime;

public sealed class PluginRuntime : MonoBehaviour
{
    private readonly List<ScheduledWork> work = new();

    public static PluginRuntime Create()
    {
        var gameObject = new GameObject("PlayhousePlugin.Runtime");

        DontDestroyOnLoad(gameObject);

        return gameObject.AddComponent<PluginRuntime>();
    }

    public ScheduledHandle Schedule(
        float delaySeconds,
        Action action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        var handle = new ScheduledHandle();

        float delay = Mathf.Max(0f, delaySeconds);

        work.Add(
            new ScheduledWork(
                Time.realtimeSinceStartup + delay,
                0f,
                action,
                handle));

        return handle;
    }

    public ScheduledHandle Repeat(
        float intervalSeconds,
        Action action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        if (intervalSeconds <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalSeconds),
                intervalSeconds,
                "The repeat interval must be greater than zero.");
        }

        var handle = new ScheduledHandle();

        work.Add(
            new ScheduledWork(
                Time.realtimeSinceStartup + intervalSeconds,
                intervalSeconds,
                action,
                handle));

        return handle;
    }

    public void StopAll()
    {
        foreach (ScheduledWork scheduled in work)
            scheduled.Handle.Cancel();

        work.Clear();
    }

    private void Update()
    {
        float now = Time.realtimeSinceStartup;

        for (int index = work.Count - 1;
             index >= 0;
             index--)
        {
            ScheduledWork scheduled = work[index];

            if (scheduled.Handle.IsCancelled)
            {
                work.RemoveAt(index);
                continue;
            }

            if (scheduled.NextRun > now)
                continue;

            try
            {
                scheduled.Action();
            }
            catch (Exception exception)
            {
                LabLogger.Error(
                    $"Scheduled task failed: {exception}");
            }

            if (scheduled.Handle.IsCancelled ||
                scheduled.Interval <= 0f)
            {
                work.RemoveAt(index);
                continue;
            }

            scheduled.NextRun =
                now + scheduled.Interval;

            work[index] = scheduled;
        }
    }

    private void OnDestroy()
    {
        StopAll();

        LabLogger.Info("PluginRuntime destroyed.");
    }

    private struct ScheduledWork
    {
        public ScheduledWork(
            float nextRun,
            float interval,
            Action action,
            ScheduledHandle handle)
        {
            NextRun = nextRun;
            Interval = interval;
            Action = action;
            Handle = handle;
        }

        public float NextRun;

        public readonly float Interval;

        public readonly Action Action;

        public readonly ScheduledHandle Handle;
    }
}

public sealed class ScheduledHandle : IDisposable
{
    public bool IsCancelled { get; private set; }

    public void Cancel()
    {
        IsCancelled = true;
    }

    public void Dispose()
    {
        Cancel();
    }
}