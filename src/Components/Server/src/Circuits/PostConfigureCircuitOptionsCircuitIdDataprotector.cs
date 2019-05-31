// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class PostConfigureCircuitOptionsCircuitIdDataprotector : IPostConfigureOptions<CircuitOptions>
    {
        private const string CircuitIdProtectorPurpose = "Microsoft.AspNetCore.Components.Server";
        private readonly IDataProtectionProvider _dataProtectionProvider;

        public PostConfigureCircuitOptionsCircuitIdDataprotector(IDataProtectionProvider dataProtectionProvider)
        {
            _dataProtectionProvider = dataProtectionProvider;
        }
        public void PostConfigure(string name, CircuitOptions options)
        {
            options.CircuitIdProtector ??= _dataProtectionProvider.CreateProtector(CircuitIdProtectorPurpose);
        }
    }
}
