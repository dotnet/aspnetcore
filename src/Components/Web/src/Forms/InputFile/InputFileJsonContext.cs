// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Forms;

// Generated-only dispatch resolves callback parameters from source-generated metadata only, and
// BrowserFile is internal, so applications cannot provide its metadata themselves.
[JsonSerializable(typeof(BrowserFile[]))]
internal sealed partial class InputFileJsonContext : JsonSerializerContext
{
#pragma warning disable CA2255 // The ModuleInitializer attribute should not be used in libraries
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Register()
    {
        // Trimming removes the registration when generated-only dispatch is disabled.
        if (GeneratedInteropPrototype.GeneratedOnly)
        {
            GeneratedInteropPrototype.AddTypeInfoResolver(Default);
        }
    }
}
