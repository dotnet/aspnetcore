// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Forms
{
    internal static class InputFileInterop
    {
        private const string JsFunctionsPrefix = "Blazor._internal.InputFile.";

        public const string Init = JsFunctionsPrefix + "init";

        public const string ReadFileData = JsFunctionsPrefix + "readFileData";

        public const string ToImageFile = JsFunctionsPrefix + "toImageFile";
    }
}
