// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.Infrastructure;

/// <summary>
/// Provides version hash for a specified file.
/// </summary>
internal sealed class DefaultFileVersionProvider : IFileVersionProvider
{
    private const string VersionKey = "v";

    public DefaultFileVersionProvider(
        IWebHostEnvironment hostingEnvironment,
        TagHelperMemoryCacheProvider cacheProvider)
    {
        ArgumentNullException.ThrowIfNull(hostingEnvironment);
        ArgumentNullException.ThrowIfNull(cacheProvider);

        FileProvider = hostingEnvironment.WebRootFileProvider;
        Cache = cacheProvider.Cache;
    }

    public IFileProvider FileProvider { get; }

    public IMemoryCache Cache { get; }

    public string AddFileVersionToPath(PathString requestPathBase, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var resolvedPath = path;

        var queryStringOrFragmentStartIndex = path.AsSpan().IndexOfAny('?', '#');
        if (queryStringOrFragmentStartIndex != -1)
        {
            resolvedPath = path.Substring(0, queryStringOrFragmentStartIndex);
        }

        if (Uri.TryCreate(resolvedPath, UriKind.Absolute, out var uri) && !uri.IsFile)
        {
            // Don't append version if the path is absolute.
            return path;
        }

        if (Cache.TryGetValue<string>(path, out var value) && value is not null)
        {
            return value;
        }

        var cacheEntryOptions = new MemoryCacheEntryOptions();
        cacheEntryOptions.AddExpirationToken(FileProvider.Watch(resolvedPath));
        var fileInfo = FileProvider.GetFileInfo(resolvedPath);

        if (!fileInfo.Exists &&
            requestPathBase.HasValue &&
            resolvedPath.StartsWith(requestPathBase.Value, StringComparison.OrdinalIgnoreCase))
        {
            var requestPathBaseRelativePath = resolvedPath.Substring(requestPathBase.Value.Length);
            cacheEntryOptions.AddExpirationToken(FileProvider.Watch(requestPathBaseRelativePath));
            fileInfo = FileProvider.GetFileInfo(requestPathBaseRelativePath);
        }

        if (fileInfo.Exists)
        {
            value = QueryHelpers.AddQueryString(path, VersionKey, GetHashForFile(fileInfo));
        }
        else
        {
            // if the file is not in the current server.
            value = path;
        }

        cacheEntryOptions.SetSize(value.Length * sizeof(char));
        Cache.Set(path, value, cacheEntryOptions);
        return value;
    }

    private static string GetHashForFile(IFileInfo fileInfo)
    {
        using (var readStream = fileInfo.CreateReadStream())
        {
            var hash = SHA256.HashData(readStream);
            return WebEncoders.Base64UrlEncode(hash);
        }
    }
}
