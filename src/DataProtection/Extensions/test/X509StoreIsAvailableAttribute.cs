// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.DataProtection;

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
