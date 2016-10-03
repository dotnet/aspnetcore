// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.ProjectModel.Internal;
using NuGet.Versioning;

namespace Microsoft.Extensions.ProjectModel
{
    public class MsBuildFixture
    {
        private readonly SemanticVersion _minMsBuildVersion = SemanticVersion.Parse("1.0.0-preview3-00000");

        internal MsBuildContext GetMsBuildContext()
        {
            // for CI
            var sdk = DotNetCoreSdkResolver.DefaultResolver.ResolveLatest();

            // for dev work in VS
            if (SemanticVersion.Parse(sdk.Version) < _minMsBuildVersion)
            {
                var home = Environment.GetEnvironmentVariable("USERPROFILE")
                    ?? Environment.GetEnvironmentVariable("HOME");
                var dotnetHome = Path.Combine(home, ".dotnet");
                var resovler = new DotNetCoreSdkResolver(dotnetHome);
                sdk = resovler.ResolveLatest();
            }

            if (SemanticVersion.Parse(sdk.Version) < _minMsBuildVersion)
            {
                throw new InvalidOperationException($"Version of .NET Core SDK found in '{sdk.BasePath}' is not new enough for these tests.");
            }

            return MsBuildContext.FromDotNetSdk(sdk);
        }
    }
}