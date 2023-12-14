// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite;

/// <summary>
/// Extensions for adding IIS Url Rewrite rules to <see cref="RewriteOptions"/>
/// </summary>
public static class IISUrlRewriteOptionsExtensions
{
    /// <summary>
    /// Add rules from a IIS config file containing Url Rewrite rules
    /// </summary>
    /// <param name="options">The <see cref="RewriteOptions"/></param>
    /// <param name="fileProvider">The <see cref="IFileProvider"/> </param>
    /// <param name="filePath">The path to the file containing UrlRewrite rules.</param>
    /// <param name="alwaysUseManagedServerVariables">Server variables are by default sourced from the server if it supports the <see cref="IServerVariablesFeature"/> feature. Use <c>true</c> to disable that behavior</param>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required for backwards compatability")]
    public static RewriteOptions AddIISUrlRewrite(this RewriteOptions options, IFileProvider fileProvider, string filePath, bool alwaysUseManagedServerVariables = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fileProvider);

        var file = fileProvider.GetFileInfo(filePath);

        using (var stream = file.CreateReadStream())
        {
            return AddIISUrlRewrite(options, new StreamReader(stream), alwaysUseManagedServerVariables);
        }
    }

    /// <summary>
    /// Add rules from a IIS config file containing Url Rewrite rules
    /// </summary>
    /// <param name="options">The <see cref="RewriteOptions"/></param>
    /// <param name="reader">The text reader stream.</param>
    /// <param name="alwaysUseManagedServerVariables">Server variables are by default sourced from the server if it supports the <see cref="IServerVariablesFeature"/> feature. Use <c>true</c> to disable that behavior</param>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "<Pending>")]
    public static RewriteOptions AddIISUrlRewrite(this RewriteOptions options, TextReader reader, bool alwaysUseManagedServerVariables = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(reader);

        var rules = new UrlRewriteFileParser().Parse(reader, alwaysUseManagedServerVariables);

        foreach (var rule in rules)
        {
            options.Rules.Add(rule);
        }

        return options;
    }
}
