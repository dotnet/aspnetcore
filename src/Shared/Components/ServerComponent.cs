// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

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
            Guid invocationId) =>
            (Sequence, AssemblyName, TypeName, InvocationId) = (sequence, assemblyName, typeName, invocationId);

        // The order in which this component was rendered
        public int Sequence { get; set; }

        // The assembly name for the rendered component.
        public string AssemblyName { get; set; }

        // The type name of the component.
        public string TypeName { get; set; }

        // An id that uniquely identifies all components generated as part of a single HTTP response.
        public Guid InvocationId { get; set; }
    }
}
