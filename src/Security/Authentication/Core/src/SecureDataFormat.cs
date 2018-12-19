// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Authentication
{
    public class SecureDataFormat<TData> : ISecureDataFormat<TData>
    {
        private readonly IDataSerializer<TData> _serializer;
        private readonly IDataProtector _protector;

        public SecureDataFormat(IDataSerializer<TData> serializer, IDataProtector protector)
        {
            _serializer = serializer;
            _protector = protector;
        }

        public string Protect(TData data)
        {
            return Protect(data, purpose: null);
        }

        public string Protect(TData data, string purpose)
        {
            var userData = _serializer.Serialize(data);

            var protector = _protector;
            if (!string.IsNullOrEmpty(purpose))
            {
                protector = protector.CreateProtector(purpose);
            }

            var protectedData = protector.Protect(userData);
            return Base64UrlTextEncoder.Encode(protectedData);
        }

        public TData Unprotect(string protectedText)
        {
            return Unprotect(protectedText, purpose: null);
        }

        public TData Unprotect(string protectedText, string purpose)
        {
            try
            {
                if (protectedText == null)
                {
                    return default(TData);
                }

                var protectedData = Base64UrlTextEncoder.Decode(protectedText);
                if (protectedData == null)
                {
                    return default(TData);
                }

                var protector = _protector;
                if (!string.IsNullOrEmpty(purpose))
                {
                    protector = protector.CreateProtector(purpose);
                }

                var userData = protector.Unprotect(protectedData);
                if (userData == null)
                {
                    return default(TData);
                }

                return _serializer.Deserialize(userData);
            }
            catch
            {
                // TODO trace exception, but do not leak other information
                return default(TData);
            }
        }
    }
}