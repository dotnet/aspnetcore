// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal
{
    internal struct ViewCompilationInfo
    {
        public ViewCompilationInfo(
            ViewFileInfo viewFileInfo,
            RazorCSharpDocument cSharpDocument)
        {
            ViewFileInfo = viewFileInfo;
            CSharpDocument = cSharpDocument;
        }

        public ViewFileInfo ViewFileInfo { get; }

        public RazorCSharpDocument CSharpDocument { get; }
    }
}
