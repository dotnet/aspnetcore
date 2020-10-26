// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    // Represents the serialized invocation to a component.
    // We serialize this marker into a comment in the generated
    // HTML.
    internal struct ServerComponentMarker
    {
        public const string ServerMarkerType = "server";

        private ServerComponentMarker(string type, string descriptor, int? sequence, string prerenderId) : this()
        {
            Type = type;
            PrerenderId = prerenderId;
            Descriptor = descriptor;
            Sequence = sequence;
        }

        // The order in which this component was rendered/produced
        // on the server. It matches the number on the descriptor
        // and is used to prevent an infinite amount of components
        // from being rendered from the client-side.
        public int? Sequence { get; set; }

        // The marker type. Right now "server" is the only valid value.
        public string Type { get; set; }

        // A string to allow the clients to differentiate between prerendered
        // and non prerendered components and to uniquely identify start and end
        // markers in prererendered components.
        public string PrerenderId { get; set; }

        // A data-protected payload that allows the server to validate the legitimacy
        // of the invocation.
        public string Descriptor { get; set; }

        // Creates a marker for a prerendered component.
        public static ServerComponentMarker Prerendered(int sequence, string descriptor) =>
            new ServerComponentMarker(ServerMarkerType, descriptor, sequence, Guid.NewGuid().ToString("N"));

        // Creates a marker for a non prerendered component
        public static ServerComponentMarker NonPrerendered(int sequence, string descriptor) =>
            new ServerComponentMarker(ServerMarkerType, descriptor, sequence, null);

        // Creates the end marker for a prerendered component.
        public ServerComponentMarker GetEndRecord()
        {
            if (PrerenderId == null)
            {
                throw new InvalidOperationException("Can't get an end record for non-prerendered components.");
            }

            return new ServerComponentMarker(null, null, null, PrerenderId);
        }
    }
}
