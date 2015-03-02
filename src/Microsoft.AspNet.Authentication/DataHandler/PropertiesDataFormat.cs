// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Authentication.DataHandler.Encoder;
using Microsoft.AspNet.Authentication.DataHandler.Serializer;

namespace Microsoft.AspNet.Authentication.DataHandler
{
    public class PropertiesDataFormat : SecureDataFormat<AuthenticationProperties>
    {
        public PropertiesDataFormat(IDataProtector protector)
            : base(DataSerializers.Properties, protector, TextEncodings.Base64Url)
        {
        }
    }
}
