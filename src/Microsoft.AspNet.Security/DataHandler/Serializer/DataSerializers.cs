// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Abstractions.Security;

namespace Microsoft.AspNet.Security.DataHandler.Serializer
{
    public static class DataSerializers
    {
        static DataSerializers()
        {
            Properties = new PropertiesSerializer();
            Ticket = new TicketSerializer();
        }

        public static IDataSerializer<AuthenticationProperties> Properties { get; set; }

        public static IDataSerializer<AuthenticationTicket> Ticket { get; set; }
    }
}
