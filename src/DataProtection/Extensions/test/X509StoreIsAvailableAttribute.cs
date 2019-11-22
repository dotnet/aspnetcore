// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.DataProtection
{
    [AttributeUsage(AttributeTargets.Method)]
    public class X509StoreIsAvailableAttribute : Attribute, ITestCondition
    {
        public X509StoreIsAvailableAttribute(StoreName name, StoreLocation location)
        {
            Name = name;
            Location = location;
        }

        public bool IsMet
        {
            get
            {
                try
                {
                    using (var store = new X509Store(Name, Location))
                    {
                        store.Open(OpenFlags.ReadWrite);
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public string SkipReason => $"Skipping because the X509Store({Name}/{Location}) is not available on this machine.";

        public StoreName Name { get; }
        public StoreLocation Location { get; }
    }
}
