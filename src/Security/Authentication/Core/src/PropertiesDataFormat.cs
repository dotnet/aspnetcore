// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Authentication
{
    public class PropertiesDataFormat : SecureDataFormat<AuthenticationProperties>
    {
        public PropertiesDataFormat(IDataProtector protector)
            : base(new PropertiesSerializer(), protector)
        {
        }
    }
}
