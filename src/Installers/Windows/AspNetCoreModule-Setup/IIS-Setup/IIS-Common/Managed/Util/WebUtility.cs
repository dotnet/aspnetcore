// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Net;

namespace Microsoft.Web.Utility
{
    internal static class WebUtility
    {
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification="We want to count any error as host doesn't exist")]
        public static bool IsLocalMachine(string serverName, bool useDns)
        {
            if (String.Equals(serverName, Environment.MachineName, StringComparison.CurrentCultureIgnoreCase) ||
                String.Equals(serverName, "localhost", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(serverName, "127.0.0.1") ||
                String.Equals(serverName, "::1"))
            {
                return true;
            }

            if (useDns)
            {
                try
                {
                    ArrayList serverAddressesList = new ArrayList();
                    ArrayList currentMachineAddressesList = new ArrayList();

                    IPAddress ownAddress = IPAddress.Parse("127.0.0.1");

                    // All the IP addresses of the hostname specified by the user
                    IPAddress[] serverAddress = Dns.GetHostAddresses(serverName);
                    serverAddressesList.AddRange(serverAddress);

                    /// All the IP addresses of the current machine
                    IPAddress[] currentMachineAddress = Dns.GetHostAddresses(Environment.MachineName);
                    currentMachineAddressesList.AddRange(currentMachineAddress);

                    // The address 127.0.0.1 also refers to the current machine
                    currentMachineAddressesList.Add(ownAddress);

                    // If any of the addresses for the current machine is the same
                    // as the address for the hostname specified by the user
                    // then use a local connection
                    foreach (IPAddress address in currentMachineAddressesList)
                    {
                        if (serverAddressesList.Contains(address))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // If the Dns class throws an exception the host propbably does not 
                    // exist so we return false
                }
            }

            return false;
        }
    }
}
