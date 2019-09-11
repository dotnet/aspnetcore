// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.Infrastructure
{
    /// <summary>
    /// Provides version hash for a specified file.
    /// </summary>
    internal class DefaultFileVersionProvider : IFileVersionProvider
    {
        private const string VersionKey = "v";
        private static readonly char[] QueryStringAndFragmentTokens = new [] { '?', '#' };

        public DefaultFileVersionProvider(
            IHostingEnvironment hostingEnvironment,
            TagHelperMemoryCacheProvider cacheProvider)
        {
            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment));
            }

            if (cacheProvider == null)
            {
                throw new ArgumentNullException(nameof(cacheProvider));
            }

            FileProvider = hostingEnvironment.WebRootFileProvider;
            Cache = cacheProvider.Cache;
        }

        public IFileProvider FileProvider { get; }

        public IMemoryCache Cache { get; }

        public string AddFileVersionToPath(PathString requestPathBase, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var resolvedPath = path;

            var queryStringOrFragmentStartIndex = path.IndexOfAny(QueryStringAndFragmentTokens);
            if (queryStringOrFragmentStartIndex != -1)
            {
                resolvedPath = path.Substring(0, queryStringOrFragmentStartIndex);
            }

            if (Uri.TryCreate(resolvedPath, UriKind.Absolute, out var uri) && !uri.IsFile)
            {
                // Don't append version if the path is absolute.
                return path;
            }

            if (Cache.TryGetValue(path, out string value))
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
            value = Cache.Set(path, value, cacheEntryOptions);
            return value;
        }

        private static string GetHashForFile(IFileInfo fileInfo)
        {
            using (var sha256 = CryptographyAlgorithms.CreateSHA256())
            {
                using (var readStream = fileInfo.CreateReadStream())
                {
                    var hash = sha256.ComputeHash(readStream);
                    return WebEncoders.Base64UrlEncode(hash);
                }
            }
        }
    }
}