// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorUnitedApp.Data;

public class Customer
{
    public string Name { get; set; } = string.Empty;
    public Address BillingAddress { get; set; } = new Address();
}
