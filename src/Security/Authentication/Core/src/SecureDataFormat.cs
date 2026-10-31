// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// An implementation for <see cref="ISecureDataFormat{TData}"/>.
/// </summary>
/// <typeparam name="TData"></typeparam>
public class SecureDataFormat<TData> : ISecureDataFormat<TData>
{
    private readonly IDataSerializer<TData> _serializer;
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes a new instance of <see cref="SecureDataFormat{TData}"/>.
    /// </summary>
    /// <param name="serializer">The <see cref="IDataSerializer{TModel}"/>.</param>
    /// <param name="protector">The <see cref="IDataProtector"/>.</param>
    public SecureDataFormat(IDataSerializer<TData> serializer, IDataProtector protector)
    {
        _serializer = serializer;
        _protector = protector;
    }

    /// <inheritdoc />
    public string Protect(TData data)
    {
        return Protect(data, purpose: null);
    }

    /// <inheritdoc />
    public string Protect(TData data, string? purpose)
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

    /// <inheritdoc />
    public TData? Unprotect(string? protectedText)
    {
        return Unprotect(protectedText, purpose: null);
    }

    /// <inheritdoc />
    public TData? Unprotect(string? protectedText, string? purpose)
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
