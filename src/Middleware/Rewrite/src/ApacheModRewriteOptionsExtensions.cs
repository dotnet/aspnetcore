// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite;

/// <summary>
/// Extensions for adding Apache mod_rewrite rules to <see cref="RewriteOptions"/>
/// </summary>
public static class ApacheModRewriteOptionsExtensions
{
    /// <summary>
    /// Add rules from an Apache mod_rewrite file
    /// </summary>
    /// <param name="options">The <see cref="RewriteOptions"/></param>
    /// <param name="fileProvider">The <see cref="IFileProvider"/> </param>
    /// <param name="filePath">The path to the file containing mod_rewrite rules.</param>
    public static RewriteOptions AddApacheModRewrite(this RewriteOptions options, IFileProvider fileProvider, string filePath)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fileProvider);

        var fileInfo = fileProvider.GetFileInfo(filePath);
        using (var stream = fileInfo.CreateReadStream())
        {
            return options.AddApacheModRewrite(new StreamReader(stream));
        }
    }

    /// <summary>
    /// Add rules from an Apache mod_rewrite file
    /// </summary>
    /// <param name="options">The <see cref="RewriteOptions"/></param>
    /// <param name="reader">A stream of mod_rewrite rules.</param>
    public static RewriteOptions AddApacheModRewrite(this RewriteOptions options, TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(reader);
        var rules = FileParser.Parse(reader);

        foreach (var rule in rules)
        {
            options.Rules.Add(rule);
        }
        return options;
    }
}
