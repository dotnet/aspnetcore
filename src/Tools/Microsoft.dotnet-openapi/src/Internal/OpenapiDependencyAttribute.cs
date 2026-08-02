// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.OpenApi;

namespace Microsoft.DotNet.Openapi.Tools.Internal;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class OpenApiDependencyAttribute : Attribute
{
    public OpenApiDependencyAttribute(string name, string version, string codeGenerators)
    {
        Name = name;
        Version = version;
        CodeGenerators = codeGenerators.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(Enum.Parse<CodeGenerator>).ToArray();
    }

    public string Name { get; set; }
    public string Version { get; set; }
    public IEnumerable<CodeGenerator> CodeGenerators { get; set; }
}
