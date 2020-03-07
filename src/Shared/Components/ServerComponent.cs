// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components
{
    // The DTO that we data-protect and include into any
    // generated component marker and that allows the client
    // to bootstrap a blazor server-side application.
    internal struct ServerComponent
    {
        public ServerComponent(
            int sequence,
            string assemblyName,
            string typeName,
            IList<ComponentParameter> parametersDefinitions,
            IList<object> parameterValues,
            Guid invocationId) =>
            (Sequence, AssemblyName, TypeName, ParameterDefinitions, ParameterValues, InvocationId) =
            (sequence, assemblyName, typeName, parametersDefinitions, parameterValues, invocationId);

        // The order in which this component was rendered
        public int Sequence { get; set; }

        // The assembly name for the rendered component.
        public string AssemblyName { get; set; }

        // The type name of the component.
        public string TypeName { get; set; }

        // The definition for the parameters for the component.
        public IList<ComponentParameter> ParameterDefinitions { get; set; }

        // The values for the parameters for the component.
        public IList<object> ParameterValues { get; set; }

        // An id that uniquely identifies all components generated as part of a single HTTP response.
        public Guid InvocationId { get; set; }
    }
}
