// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    internal class ClientComponentMarker
    {
        public ClientComponentMarker(int id, string assembly, string typeName, string parameterDefinitions, string parameterValues)
        {
            Id = id;
            Assembly = assembly;
            TypeName = typeName;
            ParameterDefinitions = parameterDefinitions;
            ParameterValues = parameterValues;
        }

        public int Id { get; }
        public string Assembly { get; }
        public string TypeName { get; }
        public string ParameterDefinitions { get; }
        public string ParameterValues { get; }
    }
}
