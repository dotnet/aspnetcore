// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;

namespace Microsoft.Extensions.SecretManager.Tools.Tests;

internal class TemporaryFileProvider : IDisposable
{
    public TemporaryFileProvider()
    {
        Root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "tmpfiles", Guid.NewGuid().ToString())).FullName;
    }

    public string Root { get; }

    public void Add(string filename, string contents)
    {
        File.WriteAllText(Path.Combine(Root, filename), contents, Encoding.UTF8);
    }

    public void Dispose()
    {
        Directory.Delete(Root, recursive: true);
    }
}
