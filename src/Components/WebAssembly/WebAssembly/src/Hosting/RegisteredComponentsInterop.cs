// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

internal sealed class RegisteredComponentsInterop
{
    private const string Prefix = "Blazor._internal.registeredComponents.";

    public const string GetRegisteredComponentsCount = Prefix + "getRegisteredComponentsCount";

    public const string GetId = Prefix + "getId";

    public const string GetAssembly = Prefix + "getAssembly";

    public const string GetTypeName = Prefix + "getTypeName";

    public const string GetParameterDefinitions = Prefix + "getParameterDefinitions";

    public const string GetParameterValues = Prefix + "getParameterValues";
}
