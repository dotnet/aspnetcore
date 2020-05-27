// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    [Obsolete("In Razor 2.1 and newer, use RazorCodeDocument.GetCodeGenerationOptions().")]
    public interface IRazorCodeGenerationOptionsFeature : IRazorEngineFeature
    {
        RazorCodeGenerationOptions GetOptions();
    }
}
