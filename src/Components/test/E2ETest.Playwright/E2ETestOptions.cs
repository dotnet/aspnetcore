// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.E2ETesting;

internal class E2ETestOptions
{
    public static readonly E2ETestOptions Instance = new E2ETestOptions();

    public int DefaultWaitTimeoutInSeconds { get; set; } = 15;

    public double DefaultAfterFailureWaitTimeoutInSeconds { get; set; } = 10;

    public bool SauceTest => false;

    public SauceOptions Sauce { get; } = new();

    public class SauceOptions
    {
        public string HostName => throw new NotImplementedException();
    }
}
