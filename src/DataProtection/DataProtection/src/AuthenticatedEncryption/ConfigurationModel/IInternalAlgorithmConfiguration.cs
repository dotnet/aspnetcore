// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

/// <summary>
/// A type that knows how to create instances of an <see cref="IAuthenticatedEncryptorDescriptor"/>
/// given specific secret key material.
/// </summary>
/// <remarks>
/// This type is not public because we don't want to lock ourselves into a contract stating
/// that a descriptor is simply a configuration plus a single serializable, reproducible secret.
/// </remarks>
internal interface IInternalAlgorithmConfiguration
{
    /// <summary>
    /// Creates a new <see cref="IAuthenticatedEncryptorDescriptor"/> instance from this configuration
    /// given specific secret key material.
    /// </summary>
    IAuthenticatedEncryptorDescriptor CreateDescriptorFromSecret(ISecret secret);

    /// <summary>
    /// Performs a self-test of the algorithm specified by the configuration object.
    /// </summary>
    void Validate();
}
