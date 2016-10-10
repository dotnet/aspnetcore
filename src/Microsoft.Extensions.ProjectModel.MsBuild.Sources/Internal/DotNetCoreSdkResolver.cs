// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Newtonsoft.Json;
using NuGet.Versioning;

namespace Microsoft.Extensions.ProjectModel.Internal
{
    internal class DotNetCoreSdkResolver
    {
        private readonly string _installationDir;

        /// <summary>
        /// Represents a resolver that uses the currently executing <see cref="Muxer"/> to find the .NET Core SDK installation
        /// </summary>
        public static readonly DotNetCoreSdkResolver DefaultResolver = new DotNetCoreSdkResolver(Path.GetDirectoryName(new Muxer().MuxerPath));

        /// <summary>
        /// Instantiates a resolver that locates the SDK
        /// </summary>
        /// <param name="installationDir">The directory containing dotnet muxer, aka DOTNET_HOME</param>
        public DotNetCoreSdkResolver(string installationDir)
        {
            _installationDir = installationDir;
        }

        /// <summary>
        /// Find the latest SDK installation (uses SemVer 1.0 to determine what is "latest")
        /// </summary>
        /// <returns>Path to SDK root directory</returns>
        public DotNetCoreSdk ResolveLatest()
        {
            var latest = FindInstalled()
                .Select(d => new { path = d, version = SemanticVersion.Parse(Path.GetFileName(d)) })
                .OrderByDescending(sdk => sdk.version)
                .FirstOrDefault();

            if (latest == null)
            {
                throw CreateSdkNotInstalledException();
            }

            return new DotNetCoreSdk
            {
                BasePath = latest.path,
                Version = latest.version.ToFullString()
            };
        }

        public DotNetCoreSdk ResolveProjectSdk(string projectDir)
        {
            var sdkVersion = ResolveGlobalJsonSdkVersion(projectDir);
            if (string.IsNullOrEmpty(sdkVersion))
            {
                return ResolveLatest();
            }

            var sdk = FindInstalled()
                .Where(p => Path.GetFileName(p).Equals(sdkVersion, StringComparison.OrdinalIgnoreCase))
                .Select(d => new { path = d, version = SemanticVersion.Parse(Path.GetFileName(d)) })
                .FirstOrDefault();

            if (sdk == null)
            {
                throw CreateSdkNotInstalledException();
            }

            return new DotNetCoreSdk
            {
                BasePath = sdk.path,
                Version = sdk.version.ToFullString()
            };
        }

        private Exception CreateSdkNotInstalledException()
        {
            return new DirectoryNotFoundException($"Could not find an installation of the .NET Core SDK in '{_installationDir}'");
        }

        private IEnumerable<string> FindInstalled()
            => Directory.EnumerateDirectories(Path.Combine(_installationDir, "sdk"));

        private string ResolveGlobalJsonSdkVersion(string start)
        {
            var dir = new DirectoryInfo(start);
            FileInfo fileInfo = null;
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "global.json");
                if (File.Exists(candidate))
                {
                    fileInfo = new FileInfo(candidate);
                    break;
                }
                dir = dir.Parent;
            }
            if (fileInfo == null)
            {
                return null;
            }
            try
            {
                var contents = File.ReadAllText(fileInfo.FullName);
                var globalJson = JsonConvert.DeserializeObject<GlobalJsonStub>(contents);
                return globalJson?.sdk?.version;
            }
            catch (JsonException)
            {
                // TODO log
                return null;
            }
        }

        private class GlobalJsonStub
        {
            public GlobalJsonSdkStub sdk { get; set; }

            public class GlobalJsonSdkStub
            {
                public string version { get; set; }
            }
        }
    }
}