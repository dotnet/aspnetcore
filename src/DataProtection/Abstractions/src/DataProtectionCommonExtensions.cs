// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.DataProtection.Abstractions;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Helpful extension methods for data protection APIs.
/// </summary>
public static class DataProtectionCommonExtensions
{
    /// <summary>
    /// Creates an <see cref="IDataProtector"/> given a list of purposes.
    /// </summary>
    /// <param name="provider">The <see cref="IDataProtectionProvider"/> from which to generate the purpose chain.</param>
    /// <param name="purposes">The list of purposes which contribute to the purpose chain. This list must
    /// contain at least one element, and it may not contain null elements.</param>
    /// <returns>An <see cref="IDataProtector"/> tied to the provided purpose chain.</returns>
    /// <remarks>
    /// This is a convenience method which chains together several calls to
    /// <see cref="IDataProtectionProvider.CreateProtector(string)"/>. See that method's
    /// documentation for more information.
    /// </remarks>
    public static IDataProtector CreateProtector(this IDataProtectionProvider provider, IEnumerable<string> purposes)
    {
        ArgumentNullThrowHelper.ThrowIfNull(provider);
        ArgumentNullThrowHelper.ThrowIfNull(purposes);

        bool collectionIsEmpty = true;
        IDataProtectionProvider retVal = provider;
        foreach (string purpose in purposes)
        {
            if (purpose == null)
            {
                throw new ArgumentException(Resources.DataProtectionExtensions_NullPurposesCollection, nameof(purposes));
            }
            retVal = retVal.CreateProtector(purpose) ?? CryptoUtil.Fail<IDataProtector>("CreateProtector returned null.");
            collectionIsEmpty = false;
        }

        if (collectionIsEmpty)
        {
            throw new ArgumentException(Resources.DataProtectionExtensions_NullPurposesCollection, nameof(purposes));
        }

        Debug.Assert(retVal is IDataProtector); // CreateProtector is supposed to return an instance of this interface
        return (IDataProtector)retVal;
    }

    /// <summary>
    /// Creates an <see cref="IDataProtector"/> given a list of purposes.
    /// </summary>
    /// <param name="provider">The <see cref="IDataProtectionProvider"/> from which to generate the purpose chain.</param>
    /// <param name="purpose">The primary purpose used to create the <see cref="IDataProtector"/>.</param>
    /// <param name="subPurposes">An optional list of secondary purposes which contribute to the purpose chain.
    /// If this list is provided it cannot contain null elements.</param>
    /// <returns>An <see cref="IDataProtector"/> tied to the provided purpose chain.</returns>
    /// <remarks>
    /// This is a convenience method which chains together several calls to
    /// <see cref="IDataProtectionProvider.CreateProtector(string)"/>. See that method's
    /// documentation for more information.
    /// </remarks>
    public static IDataProtector CreateProtector(this IDataProtectionProvider provider, string purpose, params string[] subPurposes)
    {
        ArgumentNullThrowHelper.ThrowIfNull(provider);
        ArgumentNullThrowHelper.ThrowIfNull(purpose);

        // The method signature isn't simply CreateProtector(this IDataProtectionProvider, params string[] purposes)
        // because we don't want the code provider.CreateProtector() [parameterless] to inadvertently compile.
        // The actual signature for this method forces at least one purpose to be provided at the call site.

        IDataProtector? protector = provider.CreateProtector(purpose);
        if (subPurposes != null && subPurposes.Length > 0)
        {
            protector = protector?.CreateProtector((IEnumerable<string>)subPurposes);
        }
        return protector ?? CryptoUtil.Fail<IDataProtector>("CreateProtector returned null.");
    }

    /// <summary>
    /// Retrieves an <see cref="IDataProtectionProvider"/> from an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="services">The service provider from which to retrieve the <see cref="IDataProtectionProvider"/>.</param>
    /// <returns>An <see cref="IDataProtectionProvider"/>. This method is guaranteed never to return null.</returns>
    /// <exception cref="InvalidOperationException">If no <see cref="IDataProtectionProvider"/> service exists in <paramref name="services"/>.</exception>
    public static IDataProtectionProvider GetDataProtectionProvider(this IServiceProvider services)
    {
        ArgumentNullThrowHelper.ThrowIfNull(services);

        // We have our own implementation of GetRequiredService<T> since we don't want to
        // take a dependency on DependencyInjection.Interfaces.
        var provider = (IDataProtectionProvider?)services.GetService(typeof(IDataProtectionProvider));
        if (provider == null)
        {
            throw new InvalidOperationException(Resources.FormatDataProtectionExtensions_NoService(typeof(IDataProtectionProvider).FullName));
        }
        return provider;
    }

    /// <summary>
    /// Retrieves an <see cref="IDataProtector"/> from an <see cref="IServiceProvider"/> given a list of purposes.
    /// </summary>
    /// <param name="services">An <see cref="IServiceProvider"/> which contains the <see cref="IDataProtectionProvider"/>
    /// from which to generate the purpose chain.</param>
    /// <param name="purposes">The list of purposes which contribute to the purpose chain. This list must
    /// contain at least one element, and it may not contain null elements.</param>
    /// <returns>An <see cref="IDataProtector"/> tied to the provided purpose chain.</returns>
    /// <remarks>
    /// This is a convenience method which calls <see cref="GetDataProtectionProvider(IServiceProvider)"/>
    /// then <see cref="CreateProtector(IDataProtectionProvider, IEnumerable{string})"/>. See those methods'
    /// documentation for more information.
    /// </remarks>
    public static IDataProtector GetDataProtector(this IServiceProvider services, IEnumerable<string> purposes)
    {
        ArgumentNullThrowHelper.ThrowIfNull(services);
        ArgumentNullThrowHelper.ThrowIfNull(purposes);

        return services.GetDataProtectionProvider().CreateProtector(purposes);
    }

    /// <summary>
    /// Retrieves an <see cref="IDataProtector"/> from an <see cref="IServiceProvider"/> given a list of purposes.
    /// </summary>
    /// <param name="services">An <see cref="IServiceProvider"/> which contains the <see cref="IDataProtectionProvider"/>
    /// from which to generate the purpose chain.</param>
    /// <param name="purpose">The primary purpose used to create the <see cref="IDataProtector"/>.</param>
    /// <param name="subPurposes">An optional list of secondary purposes which contribute to the purpose chain.
    /// If this list is provided it cannot contain null elements.</param>
    /// <returns>An <see cref="IDataProtector"/> tied to the provided purpose chain.</returns>
    /// <remarks>
    /// This is a convenience method which calls <see cref="GetDataProtectionProvider(IServiceProvider)"/>
    /// then <see cref="CreateProtector(IDataProtectionProvider, string, string[])"/>. See those methods'
    /// documentation for more information.
    /// </remarks>
    public static IDataProtector GetDataProtector(this IServiceProvider services, string purpose, params string[] subPurposes)
    {
        ArgumentNullThrowHelper.ThrowIfNull(services);
        ArgumentNullThrowHelper.ThrowIfNull(purpose);

        return services.GetDataProtectionProvider().CreateProtector(purpose, subPurposes);
    }

    /// <summary>
    /// Cryptographically protects a piece of plaintext data.
    /// </summary>
    /// <param name="protector">The data protector to use for this operation.</param>
    /// <param name="plaintext">The plaintext data to protect.</param>
    /// <returns>The protected form of the plaintext data.</returns>
    public static string Protect(this IDataProtector protector, string plaintext)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protector);
        ArgumentNullThrowHelper.ThrowIfNull(plaintext);

        try
        {
            byte[] plaintextAsBytes = EncodingUtil.SecureUtf8Encoding.GetBytes(plaintext);
            byte[] protectedDataAsBytes = protector.Protect(plaintextAsBytes);
            return WebEncoders.Base64UrlEncode(protectedDataAsBytes);
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize exceptions to CryptographicException
            throw Error.CryptCommon_GenericError(ex);
        }
    }

    /// <summary>
    /// Cryptographically unprotects a piece of protected data.
    /// </summary>
    /// <param name="protector">The data protector to use for this operation.</param>
    /// <param name="protectedData">The protected data to unprotect.</param>
    /// <returns>The plaintext form of the protected data.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown if <paramref name="protectedData"/> is invalid or malformed.
    /// </exception>
    public static string Unprotect(this IDataProtector protector, string protectedData)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protector);
        ArgumentNullThrowHelper.ThrowIfNull(protectedData);

        try
        {
            byte[] protectedDataAsBytes = WebEncoders.Base64UrlDecode(protectedData);
            byte[] plaintextAsBytes = protector.Unprotect(protectedDataAsBytes);
            return EncodingUtil.SecureUtf8Encoding.GetString(plaintextAsBytes);
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize exceptions to CryptographicException
            throw Error.CryptCommon_GenericError(ex);
        }
    }
}
