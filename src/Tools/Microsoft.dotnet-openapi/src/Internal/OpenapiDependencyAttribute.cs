// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.OpenApi;

namespace Microsoft.DotNet.Openapi.Tools.Internal
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal class OpenApiDependencyAttribute : Attribute
    {
        public OpenApiDependencyAttribute(string name, string version, string codeGenerators)
        {
            Name = name;
            Version = version;
            CodeGenerators = codeGenerators.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(c => Enum.Parse<CodeGenerator>(c)).ToArray();
        }

        public string Name { get; set; }
        public string Version { get; set; }
        public IEnumerable<CodeGenerator> CodeGenerators { get; set; }
    }
}
