// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Linq;

namespace TestServer;

/// <summary>
/// Represents detailed information about an unobserved task exception, including the original call stack.
/// </summary>
public class UnobservedExceptionDetails
{
    /// <summary>
    /// The original exception that was unobserved.
    /// </summary>
    public Exception Exception { get; init; }

    /// <summary>
    /// The timestamp when the exception was observed.
    /// </summary>
    public DateTime ObservedAt { get; init; }

    /// <summary>
    /// The current call stack when the exception was observed (may show finalizer thread).
    /// </summary>
    public string ObservedCallStack { get; init; }

    /// <summary>
    /// Detailed breakdown of inner exceptions and their stack traces.
    /// </summary>
    public string DetailedExceptionInfo { get; init; }

    /// <summary>
    /// The managed thread ID where the exception was observed.
    /// </summary>
    public int ObservedThreadId { get; init; }

    /// <summary>
    /// Whether this exception was observed on the finalizer thread.
    /// </summary>
    public bool IsFromFinalizerThread { get; init; }

    public UnobservedExceptionDetails(Exception exception)
    {
        Exception = exception;
        ObservedAt = DateTime.UtcNow;
        ObservedCallStack = Environment.StackTrace;
        DetailedExceptionInfo = BuildDetailedExceptionInfo(exception);
        ObservedThreadId = Environment.CurrentManagedThreadId;
        IsFromFinalizerThread = Thread.CurrentThread.IsThreadPoolThread && Thread.CurrentThread.IsBackground;
    }

    private static string BuildDetailedExceptionInfo(Exception exception)
    {
        var sb = new StringBuilder();
        var currentException = exception;
        var depth = 0;

        while (currentException is not null)
        {
            var indent = new string(' ', depth * 2);
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Exception Type: {currentException.GetType().FullName}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Message: {currentException.Message}");

            if (currentException.Data.Count > 0)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Data:");
                foreach (var key in currentException.Data.Keys)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}  {key}: {currentException.Data[key]}");
                }
            }

            if (!string.IsNullOrEmpty(currentException.StackTrace))
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Stack Trace:");
                sb.AppendLine(currentException.StackTrace);
            }

            // Handle AggregateException specially to extract all inner exceptions
            if (currentException is AggregateException aggregateException)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Aggregate Exception contains {aggregateException.InnerExceptions.Count} inner exceptions:");
                for (int i = 0; i < aggregateException.InnerExceptions.Count; i++)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}  Inner Exception {i + 1}:");
                    sb.AppendLine(BuildDetailedExceptionInfo(aggregateException.InnerExceptions[i]));
                }
                break; // Don't process InnerException for AggregateException as we've handled all inner exceptions
            }

            currentException = currentException.InnerException;
            depth++;

            if (currentException is not null)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}--- Inner Exception ---");
            }
        }

        return sb.ToString();
    }
}

public class UnobservedTaskExceptionObserver
{
    private readonly ConcurrentQueue<UnobservedExceptionDetails> _exceptions = new();
    private int _circularRedirectCount;

    public void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        var details = new UnobservedExceptionDetails(e.Exception);
        _exceptions.Enqueue(details);
        e.SetObserved(); // Mark as observed to prevent the process from crashing during tests
    }

    public bool HasExceptions => !_exceptions.IsEmpty;

    /// <summary>
    /// Gets the detailed exception information including original call stacks.
    /// </summary>
    public IReadOnlyCollection<UnobservedExceptionDetails> GetExceptionDetails() => _exceptions.ToArray();

    /// <summary>
    /// Gets the raw exceptions for backward compatibility.
    /// </summary>
    public IReadOnlyCollection<Exception> GetExceptions() => _exceptions.ToArray().Select(d => d.Exception).ToList();

    public void Clear()
    {
        _exceptions.Clear();
        _circularRedirectCount = 0;
    }

    public int GetCircularRedirectCount()
    {
        return _circularRedirectCount;
    }

    public int GetAndIncrementCircularRedirectCount()
    {
        return Interlocked.Increment(ref _circularRedirectCount) - 1;
    }
}
