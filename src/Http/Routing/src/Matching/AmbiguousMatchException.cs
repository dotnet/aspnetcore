// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Routing.Matching;

/// <summary>
/// An exception which indicates multiple matches in endpoint selection.
/// </summary>
[Serializable]
internal sealed class AmbiguousMatchException : Exception
{
    public AmbiguousMatchException(string message)
        : base(message)
    {
    }

    [Obsolete]
    internal AmbiguousMatchException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
