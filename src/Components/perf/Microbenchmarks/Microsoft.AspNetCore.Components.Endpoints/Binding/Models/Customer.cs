// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

public class Customer
{
    public string CompanyName { get; set; }

    public string ContactName { get; set; }
    public string ContactTitle { get; set; }

    public Address Address { get; set; }

    public string Phone { get; set; }

    public string Fax { get; set; }
}
