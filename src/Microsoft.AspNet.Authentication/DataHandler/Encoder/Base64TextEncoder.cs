// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;

namespace Microsoft.AspNet.Authentication.DataHandler.Encoder
{
    public class Base64TextEncoder : ITextEncoder
    {
        public string Encode(byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        public byte[] Decode(string text)
        {
            return Convert.FromBase64String(text);
        }
    }
}
