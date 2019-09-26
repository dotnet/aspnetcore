// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components
{
    // **Component descriptor protocol**
    // MVC serializes one or more components as comments in HTML.
    // Each comment is in the form <!-- Blazor:<<Json>>--> for example { "type": "client", "typeName": "<<type>>", "assembly": "<<assembly>>", "parameterDefinitions": [{"name":"<<name>>", typeName="<<type>>", "assembly": "<<assembly>>"}], "parameterValues": [<<value>>] }
    // Where <<Json>> has the following properties:
    // 'type' indicates the marker type. It's value for client-side components is 'client'.
    // 'typeName' the full type name for the rendered component.
    // 'assembly' the assembly name for the rendered component.
    // 'parameterDefinitions' an array that contains the definitions for the parameters including their names and types and assemblies.
    // 'parameterValues' an array containing the parameter values.
    // 'prerenderId' a unique identifier that uniquely identifies the marker to match start and end markers.
    internal struct ClientComponentMarker
    {
        public const string ClientMarkerType = "client";

        public ClientComponentMarker(string type, string assembly, string typeName, IList<ComponentParameter> parameterDefinitions, IList<object> parameterValues, string prereenderId) =>
            (Type, Assembly, TypeName, ParameterDefinitions, ParameterValues, PrerenderId) = (type, assembly, typeName, parameterDefinitions, parameterValues, prereenderId);

        // The type of the marker
        public string Type { get; set; }

        // The name of the assembly
        public string Assembly { get; set; }

        // The name of the type
        public string TypeName { get; set; }

        // The definition for the parameters, contains its name and its typename and assembly if the parameter is not null.
        public IList<ComponentParameter> ParameterDefinitions { get; set; }

        // The values for the parameters, match 1 to 1 to the definitions.
        public IList<object> ParameterValues { get; set; }

        // A unique id used in prerendering to match the start and end of the components.
        public string PrerenderId { get; set; }

        internal static object NonPrerendered(string assembly, string typeName, IList<ComponentParameter> parameterDefinitions, IList<object> parameterValues) =>
            new ClientComponentMarker(ClientMarkerType, assembly, typeName, parameterDefinitions, parameterValues, null);

        internal static ClientComponentMarker Prerendered(string assembly, string typeName, IList<ComponentParameter> parameterDefinitions, IList<object> parameterValues) =>
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
