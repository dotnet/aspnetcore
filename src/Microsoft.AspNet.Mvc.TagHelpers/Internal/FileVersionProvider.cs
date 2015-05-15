// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// Provides version hash for a specified file.
    /// </summary>
    public class FileVersionProvider
    {
        private const string VersionKey = "v";
        private readonly IFileProvider _fileProvider;
        private readonly IMemoryCache _cache;
        private readonly PathString _requestPathBase;

        /// <summary>
        /// Creates a new instance of <see cref="FileVersionProvider"/>.
        /// </summary>
        /// <param name="fileProvider">The file provider to get and watch files.</param>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="cache"><see cref="IMemoryCache"/> where versioned urls of files are cached.</param>
        public FileVersionProvider(
            [NotNull] IFileProvider fileProvider,
            [NotNull] IMemoryCache cache,
            [NotNull] PathString requestPathBase)
        {
            _fileProvider = fileProvider;
            _cache = cache;
            _requestPathBase = requestPathBase;
        }

        /// <summary>
        /// Adds version query parameter to the specified file path.
        /// </summary>
        /// <param name="path">The path of the file to which version should be added.</param>
        /// <returns>Path containing the version query string.</returns>
        /// <remarks>
        /// The version query string is appended as with the key "v".
        /// </remarks>
        public string AddFileVersionToPath([NotNull] string path)
        {
            var resolvedPath = path;
            var fileInfo = _fileProvider.GetFileInfo(resolvedPath);
            if (!fileInfo.Exists)
            {
                if (_requestPathBase.HasValue &&
                    resolvedPath.StartsWith(_requestPathBase.Value, StringComparison.OrdinalIgnoreCase))
                {
                    resolvedPath = resolvedPath.Substring(_requestPathBase.Value.Length);
                    fileInfo = _fileProvider.GetFileInfo(resolvedPath);
                }

                if (!fileInfo.Exists)
                {
                    // if the file is not in the current server.
                    return path;
                }
            }

            string value;
            if(!_cache.TryGetValue(path, out value))
            {
                value = QueryHelpers.AddQueryString(path, VersionKey, GetHashForFile(fileInfo));
                _cache.Set(
                    path,
                    value,
                    new MemoryCacheEntryOptions().AddExpirationTrigger(_fileProvider.Watch(resolvedPath)));
            }

            return value;
        }

        private string GetHashForFile(IFileInfo fileInfo)
        {
            using (var sha256 = SHA256.Create())
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