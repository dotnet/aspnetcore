// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Extensions.CommandLineUtils
{
    public class DotNetMuxerTests
    {
        [Fact]
        public void FindsTheMuxer()
        {

            var muxerPath = DotNetMuxer.TryFindMuxerPath(GetDotnetPath());
            Assert.NotNull(muxerPath);
            Assert.True(File.Exists(muxerPath), "The file did not exist");
            Assert.True(Path.IsPathRooted(muxerPath), "The path should be rooted");
            Assert.Equal("dotnet", Path.GetFileNameWithoutExtension(muxerPath), ignoreCase: true);

            static string GetDotnetPath()
            {
                // Process.MainModule is app[.exe] and not `dotnet`. We can instead calculate the dotnet SDK path
                // by looking at the shared fx directory instead.
                // depsFile = /dotnet/shared/Microsoft.NETCore.App/6.0-preview2/Microsoft.NETCore.App.deps.json
                var depsFile = (string)AppContext.GetData("FX_DEPS_FILE");
                return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(depsFile), "..", "..", "..", "dotnet" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "")));
            }
        }

        [Fact]
        public void ReturnsNullIfMainModuleIsNotDotNet()
        {
            var muxerPath = DotNetMuxer.TryFindMuxerPath(@"d:\some-path\testhost.exe");
            Assert.Null(muxerPath);
        }
    }
}
#endif
