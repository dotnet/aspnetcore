// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal partial class IISHttpContext : IHttpRequestIdentifierFeature
{
    string IHttpRequestIdentifierFeature.TraceIdentifier
    {
        get
        {
            if (TraceIdentifier == null)
            {
                InitializeHttpRequestIdentifierFeature();
            }

            return TraceIdentifier;
        }
        set => TraceIdentifier = value;
    }

    [MemberNotNull(nameof(TraceIdentifier))]
    private unsafe void InitializeHttpRequestIdentifierFeature()
    {
        // Copied from WebListener
        // This is the base GUID used by HTTP.SYS for generating the activity ID.
        // HTTP.SYS overwrites the first 8 bytes of the base GUID with RequestId to generate ETW activity ID.
        // The requestId should be set by the NativeRequestContext
        var guid = new Guid(0xffcb4c93, 0xa57f, 0x453c, 0xb6, 0x3f, 0x84, 0x71, 0xc, 0x79, 0x67, 0xbb);
        *((ulong*)&guid) = RequestId;

        // TODO: Also make this not slow
        TraceIdentifier = guid.ToString();
    }
}
