// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

internal static class InputFileInterop
{
    private const string JsFunctionsPrefix = "Blazor._internal.InputFile.";

    public const string Init = JsFunctionsPrefix + "init";

    public const string ReadFileData = JsFunctionsPrefix + "readFileData";

    public const string ToImageFile = JsFunctionsPrefix + "toImageFile";
}
