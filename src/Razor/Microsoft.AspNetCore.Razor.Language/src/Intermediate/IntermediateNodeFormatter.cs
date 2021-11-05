// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public abstract class IntermediateNodeFormatter
{
    public abstract void WriteChildren(IntermediateNodeCollection children);

    public abstract void WriteContent(string content);

    public abstract void WriteProperty(string key, string value);
}
