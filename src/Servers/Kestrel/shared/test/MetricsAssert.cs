// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.InternalTesting;

internal static class MetricsAssert
{
    public static void Equal(ConnectionEndReason expectedReason, string errorType)
    {
        Assert.Equal(KestrelMetrics.GetErrorType(expectedReason), errorType);
    }

    public static void Equal(ConnectionEndReason expectedReason, IReadOnlyDictionary<string, object> tags)
    {
        Equal(expectedReason, (string) tags[KestrelMetrics.ErrorTypeAttributeName]);
    }

    public static void NoError(IReadOnlyDictionary<string, object> tags)
    {
        if (tags.TryGetValue(KestrelMetrics.ErrorTypeAttributeName, out var error))
        {
            Assert.Fail($"Tag collection contains {KestrelMetrics.ErrorTypeAttributeName} with value {error}.");
        }
    }
}
