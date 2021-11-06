// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands;

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
