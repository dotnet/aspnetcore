// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Default implementation of <see cref="IFormFileCollection"/>.
/// </summary>
public class FormFileCollection : List<IFormFile>, IFormFileCollection
{
    /// <inheritdoc />
    public IFormFile? this[string name] => GetFile(name);

    /// <inheritdoc />
    public IFormFile? GetFile(string name)
    {
        foreach (var file in this)
        {
            if (string.Equals(name, file.Name, StringComparison.OrdinalIgnoreCase))
            {
                return file;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public IReadOnlyList<IFormFile> GetFiles(string name)
    {
        var files = new List<IFormFile>();

        foreach (var file in this)
        {
            if (string.Equals(name, file.Name, StringComparison.OrdinalIgnoreCase))
            {
                files.Add(file);
            }
        }

        return files;
    }
}
