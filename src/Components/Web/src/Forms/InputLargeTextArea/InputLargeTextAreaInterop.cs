// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms
{
    internal static class InputLargeTextAreaInterop
    {
        private const string JsFunctionsPrefix = "Blazor._internal.InputLargeTextArea.";

        public const string Init = JsFunctionsPrefix + "init";

        public const string GetText = JsFunctionsPrefix + "getText";

        public const string SetText = JsFunctionsPrefix + "setText";
    }
}
