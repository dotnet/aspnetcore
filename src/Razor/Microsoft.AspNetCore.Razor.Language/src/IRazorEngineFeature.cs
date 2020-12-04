// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public interface IRazorEngineFeature : IRazorFeature
    {
#pragma warning disable CS0618
        RazorEngine Engine { get; set; }
#pragma warning restore CS0618
    }
}
