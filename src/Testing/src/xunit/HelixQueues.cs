// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Testing;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HelixQueues : Attribute, ITestCondition
{
    public HelixQueues()
    {
    }

    public string Includes { get; set; }
    public string Excludes { get; set; }

    public bool IsMet
    {
        get
        {
            return HelixHelper.OnHelix() && IsQueueMatch(Includes) && !IsQueueMatch(Excludes);
        }
    }

    public string SkipReason => "Queue is filtered by Includes/Excludes.";

    private bool IsQueueMatch(string queues)
    {
        if (string.IsNullOrEmpty(queues))
        {
            return false;
        }

        if (queues == "*")
        {
            return true;
        }

        var targetQueue = HelixHelper.GetTargetHelixQueue();

        if (queues.Contains("All.Windows") && targetQueue.StartsWith("windows", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (queues.Contains("All.OSX") && targetQueue.StartsWith("osx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (queues.Contains("All.Ubuntu") && targetQueue.StartsWith("ubuntu", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return queues.ToLowerInvariant().Split(';').Contains(targetQueue);
    }

    public static bool OnHelix() => HelixHelper.OnHelix();
}
