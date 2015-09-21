// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.DataProtection;

namespace Microsoft.AspNet.Authentication
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
            byte[] userData = _serializer.Serialize(data);
            byte[] protectedData = _protector.Protect(userData);
            string protectedText = Base64UrlTextEncoder.Encode(protectedData);
            return protectedText;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception will be traced")]
        public TData Unprotect(string protectedText)
        {
            try
            {
                if (protectedText == null)
                {
                    return default(TData);
                }

                byte[] protectedData = Base64UrlTextEncoder.Decode(protectedText);
                if (protectedData == null)
                {
                    return default(TData);
                }

                byte[] userData = _protector.Unprotect(protectedData);
                if (userData == null)
                {
                    return default(TData);
                }

                TData model = _serializer.Deserialize(userData);
                return model;
            }
            catch
            {
                // TODO trace exception, but do not leak other information
                return default(TData);
            }
        }
    }
}