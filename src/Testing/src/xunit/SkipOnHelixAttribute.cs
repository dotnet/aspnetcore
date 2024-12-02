// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.InternalTesting;

/// <summary>
/// Skip test if running on helix (or a particular helix queue).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class SkipOnHelixAttribute : Attribute, ITestCondition
{
    public SkipOnHelixAttribute(string issueUrl)
    {
        ArgumentThrowHelper.ThrowIfNullOrEmpty(issueUrl);
        IssueUrl = issueUrl;
    }

    public string IssueUrl { get; }

    public bool IsMet
    {
        get
        {
            var skip = OnHelix() && ShouldSkip();
            return !skip;
        }
    }

    // Queues that should be skipped on, i.e. "Windows.Amd64.Server2022.Open;OSX.1015.Amd64.Open"
    public string Queues { get; set; }

    public string SkipReason
    {
        get
        {
            return "This test is skipped on helix";
        }
    }

    private bool ShouldSkip()
    {
        if (Queues == null)
        {
            return true;
        }

        var targetQueue = GetTargetHelixQueue().ToLowerInvariant();

        if (Queues.Contains("All.OSX") && targetQueue.StartsWith("osx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Queues.Contains("All.Ubuntu") && targetQueue.StartsWith("ubuntu", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Queues.Contains("All.Linux") && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return true;
        }

        return Queues.ToLowerInvariant().Split(';').Contains(targetQueue);
    }

    public static bool OnHelix() => HelixHelper.OnHelix();

    public static string GetTargetHelixQueue() => HelixHelper.GetTargetHelixQueue();
}
