// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.DataProtection;

internal static class Error
{
    public static InvalidOperationException CertificateXmlEncryptor_CertificateNotFound(string thumbprint)
    {
        var message = Resources.FormatCertificateXmlEncryptor_CertificateNotFound(thumbprint);
        return new InvalidOperationException(message);
    }

    public static ArgumentException Common_ArgumentCannotBeNullOrEmpty(string parameterName)
    {
        return new ArgumentException(Resources.Common_ArgumentCannotBeNullOrEmpty, parameterName);
    }

    public static ArgumentException Common_BufferIncorrectlySized(string parameterName, int actualSize, int expectedSize)
    {
        var message = Resources.FormatCommon_BufferIncorrectlySized(actualSize, expectedSize);
        return new ArgumentException(message, parameterName);
    }

    public static CryptographicException CryptCommon_GenericError(Exception? inner = null)
    {
        return new CryptographicException(Resources.CryptCommon_GenericError, inner);
    }

    public static CryptographicException CryptCommon_PayloadInvalid()
    {
        var message = Resources.CryptCommon_PayloadInvalid;
        return new CryptographicException(message);
    }

    public static InvalidOperationException Common_PropertyCannotBeNullOrEmpty(string propertyName)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Resources.Common_PropertyCannotBeNullOrEmpty, propertyName);
        return new InvalidOperationException(message);
    }

    public static InvalidOperationException Common_PropertyMustBeNonNegative(string propertyName)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Resources.Common_PropertyMustBeNonNegative, propertyName);
        return new InvalidOperationException(message);
    }

    public static CryptographicException Common_EncryptionFailed(Exception? inner = null)
    {
        return new CryptographicException(Resources.Common_EncryptionFailed, inner);
    }

    public static CryptographicException Common_KeyNotFound(Guid id)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Resources.Common_KeyNotFound, id);
        return new CryptographicException(message);
    }

    public static CryptographicException Common_KeyRevoked(Guid id)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Resources.Common_KeyRevoked, id);
        return new CryptographicException(message);
    }

    public static ArgumentOutOfRangeException Common_ValueMustBeNonNegative(string paramName)
    {
        return new ArgumentOutOfRangeException(paramName, Resources.Common_ValueMustBeNonNegative);
    }

    public static CryptographicException DecryptionFailed(Exception inner)
    {
        return new CryptographicException(Resources.Common_DecryptionFailed, inner);
    }

    public static CryptographicException ProtectionProvider_BadMagicHeader()
    {
        return new CryptographicException(Resources.ProtectionProvider_BadMagicHeader);
    }

    public static CryptographicException ProtectionProvider_BadVersion()
    {
        return new CryptographicException(Resources.ProtectionProvider_BadVersion);
    }

    public static InvalidOperationException XmlKeyManager_DuplicateKey(Guid keyId)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Resources.XmlKeyManager_DuplicateKey, keyId);
        return new InvalidOperationException(message);
    }

    public static InvalidOperationException KeyRingProvider_DefaultKeyRevoked(Guid id)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Resources.KeyRingProvider_DefaultKeyRevoked, id);
        return new InvalidOperationException(message);
    }

    public static InvalidOperationException KeyRingProvider_RefreshFailedOnOtherThread(Exception? inner)
    {
        return new InvalidOperationException(Resources.KeyRingProvider_RefreshFailedOnOtherThread, inner);
    }

    public static NotSupportedException XmlKeyManager_DoesNotSupportKeyDeletion()
    {
        return new NotSupportedException(Resources.XmlKeyManager_DoesNotSupportKeyDeletion);
    }
}
