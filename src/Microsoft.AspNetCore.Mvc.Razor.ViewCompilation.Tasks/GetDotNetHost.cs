// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.BuildTools
{
    public class GetDotNetHost : Task
    {
        [Output]
        public string MuxerPath { get; set; }

        public override bool Execute()
        {
            MuxerPath = DotNetMuxer.MuxerPathOrDefault();
            return true;
        }
    }
}
