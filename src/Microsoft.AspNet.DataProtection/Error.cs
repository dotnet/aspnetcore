// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Security.Cryptography;

namespace Microsoft.AspNet.DataProtection
{
    internal static class Error
    {
        public static ArgumentException Common_BufferIncorrectlySized(string parameterName, int actualSize, int expectedSize)
        {
            string message = String.Format(CultureInfo.CurrentCulture, Resources.Common_BufferIncorrectlySized, actualSize, expectedSize);
            return new ArgumentException(message, parameterName);
        }

        public static CryptographicException CryptCommon_GenericError(Exception inner = null)
        {
            return new CryptographicException(Resources.CryptCommon_GenericError, inner);
        }

        public static CryptographicException CryptCommon_PayloadInvalid()
        {
            string message = Resources.CryptCommon_PayloadInvalid;
            return new CryptographicException(message);
        }

        public static InvalidOperationException Common_PropertyCannotBeNullOrEmpty(string propertyName)
        {
            string message = String.Format(CultureInfo.CurrentCulture, Resources.Common_PropertyCannotBeNullOrEmpty, propertyName);
            throw new InvalidOperationException(message);
        }

        public static CryptographicException Common_EncryptionFailed(Exception inner = null)
        {
            return new CryptographicException(Resources.Common_EncryptionFailed, inner);
        }

        public static CryptographicException Common_KeyNotFound(Guid id)
        {
            string message = String.Format(CultureInfo.CurrentCulture, Resources.Common_KeyNotFound, id);
            return new CryptographicException(message);
        }

        public static CryptographicException Common_KeyRevoked(Guid id)
        {
            string message = String.Format(CultureInfo.CurrentCulture, Resources.Common_KeyRevoked, id);
            return new CryptographicException(message);
        }

        public static CryptographicException Common_NotAValidProtectedPayload()
        {
            return new CryptographicException(Resources.Common_NotAValidProtectedPayload);
        }

        public static CryptographicException Common_PayloadProducedByNewerVersion()
        {
            return new CryptographicException(Resources.Common_PayloadProducedByNewerVersion);
        }

        public static CryptographicException DecryptionFailed(Exception inner)
        {
            return new CryptographicException(Resources.Common_DecryptionFailed, inner);
        }

        public static CryptographicException TimeLimitedDataProtector_PayloadExpired(ulong utcTicksExpiration)
        {
            DateTimeOffset expiration = new DateTimeOffset((long)utcTicksExpiration, TimeSpan.Zero).ToLocalTime();
            string message = String.Format(CultureInfo.CurrentCulture, Resources.TimeLimitedDataProtector_PayloadExpired, expiration);
            return new CryptographicException(message);
        }
    }
}
