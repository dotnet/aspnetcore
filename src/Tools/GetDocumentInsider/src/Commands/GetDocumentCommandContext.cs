// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands
{
    [Serializable]
    public class GetDocumentCommandContext
    {
        public string AssemblyName { get; set; }

        public string AssemblyPath { get; set; }

        public string FileListPath { get; set; }

        public string OutputDirectory { get; set; }

        public string ProjectName { get; set; }

        public IReporter Reporter { get; set; }
    }
}
