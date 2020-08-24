// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    internal struct ClientComponentMarker
    {
        public const string ClientMarkerType = "client";

        public ClientComponentMarker(string type, string assembly, string typeName, string parameterDefinitions, string parameterValues, string prereenderId) =>
            (Type, Assembly, TypeName, ParameterDefinitions, ParameterValues, PrerenderId) = (type, assembly, typeName, parameterDefinitions, parameterValues, prereenderId);

        public string Type { get; set; }

        public string Assembly { get; set; }

        public string TypeName { get; set; }

        public string ParameterDefinitions { get; set; }

        public string ParameterValues { get; set; }

        public string PrerenderId { get; set; }

        internal static ClientComponentMarker NonPrerendered(string assembly, string typeName, string parameterDefinitions, string parameterValues) =>
            new ClientComponentMarker(ClientMarkerType, assembly, typeName, parameterDefinitions, parameterValues, null);

        internal static ClientComponentMarker Prerendered(string assembly, string typeName, string parameterDefinitions, string parameterValues) =>
            new ClientComponentMarker(ClientMarkerType, assembly, typeName, parameterDefinitions, parameterValues, Guid.NewGuid().ToString("N"));

        public ClientComponentMarker GetEndRecord()
        {
            if (PrerenderId == null)
            {
                throw new InvalidOperationException("Can't get an end record for non-prerendered components.");
            }

            return new ClientComponentMarker(null, null, null, null, null, PrerenderId);
        }
    }
}
