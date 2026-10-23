// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

// A Web IDL enum projected as a struct over its string, so a value the library does not define still round-trips.
internal interface IWebIdlEnum<TSelf> where TSelf : struct, IWebIdlEnum<TSelf>
{
    static abstract TSelf Create(string value);

    string Value { get; }
}
