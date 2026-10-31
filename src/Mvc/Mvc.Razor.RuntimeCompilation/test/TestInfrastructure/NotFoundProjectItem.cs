// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal class NotFoundProjectItem : RazorProjectItem
{
    public NotFoundProjectItem(string basePath, string path)
    {
        BasePath = basePath;
        FilePath = path;
    }

    public override string BasePath { get; }

    public override string FilePath { get; }

    public override bool Exists => false;

    public override string PhysicalPath => throw new NotSupportedException();

    public override Stream Read() => throw new NotSupportedException();
}
