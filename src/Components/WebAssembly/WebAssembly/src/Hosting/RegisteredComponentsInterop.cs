// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

internal sealed partial class RegisteredComponentsInterop
{
    private const string Prefix = "Blazor._internal.registeredComponents.";

    [JSImport(Prefix + "getRegisteredComponentsCount")]
    public static partial int GetRegisteredComponentsCount();

    [JSImport(Prefix + "getId")]
    public static partial int GetId(int index);

    [JSImport(Prefix + "getAssembly")]
    public static partial string GetAssembly(int id);

    [JSImport(Prefix + "getTypeName")]
    public static partial string GetTypeName(int id);

    [JSImport(Prefix + "getParameterDefinitions")]
    public static partial string GetParameterDefinitions(int id);

    [JSImport(Prefix + "getParameterValues")]
    public static partial string GetParameterValues(int id);
}
