// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public interface IRazorDirectiveFeature : IRazorEngineFeature
{
    ICollection<DirectiveDescriptor> Directives { get; }
}
