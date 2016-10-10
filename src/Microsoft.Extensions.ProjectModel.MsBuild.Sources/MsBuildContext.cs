// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.ProjectModel.Internal;

namespace Microsoft.Extensions.ProjectModel
{
    /// <summary>
    /// Represents the msbuild context used to parse a project model
    /// </summary>
    internal class MsBuildContext
    {
        public string MsBuildExecutableFullPath { get; private set; }
        public string ExtensionsPath { get; private set; }

        public static MsBuildContext FromCurrentDotNetSdk()
        {
            var sdk = DotNetCoreSdkResolver.DefaultResolver.ResolveLatest();
            return FromDotNetSdk(sdk);
        }

        public static MsBuildContext FromDotNetSdk(DotNetCoreSdk sdk)
        {
            if (sdk == null)
            {
                throw new ArgumentNullException(nameof(sdk));
            }

            return new MsBuildContext
            {
                // might change... See https://github.com/Microsoft/msbuild/issues/1136
                MsBuildExecutableFullPath = Path.Combine(sdk.BasePath, "MSBuild.exe"),
                ExtensionsPath = sdk.BasePath
            };
        }
    }
}