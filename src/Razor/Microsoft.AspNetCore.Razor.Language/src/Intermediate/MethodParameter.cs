// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class MethodParameter
{
    public IList<string> Modifiers { get; } = new List<string>();

    public string TypeName { get; set; }

    public string ParameterName { get; set; }
}
