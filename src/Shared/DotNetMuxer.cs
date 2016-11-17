// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Tools.Internal
{
    public static class DotNetMuxer
    {
        private const string MuxerName = "dotnet";

        static DotNetMuxer()
        {
            MuxerPath = TryFindMuxerPath();
        }

        public static string MuxerPath { get; }

        public static string MuxerPathOrDefault()
            => MuxerPath ?? MuxerName;

        private static string TryFindMuxerPath()
        {
            var fileName = MuxerName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName += ".exe";
            }

            var fxDepsFile = AppContext.GetData("FX_DEPS_FILE") as string;

            if (string.IsNullOrEmpty(fxDepsFile))
            {
                return null;
            }

            var muxerDir = new FileInfo(fxDepsFile) // Microsoft.NETCore.App.deps.json
                .Directory? // (version)
                .Parent? // Microsoft.NETCore.App
                .Parent? // shared
                .Parent; // DOTNET_HOME

            if (muxerDir == null)
            {
                return null;
            }

            var muxer = Path.Combine(muxerDir.FullName, fileName);
            return File.Exists(muxer)
                ? muxer
                : null;
        }
    }
}