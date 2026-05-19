// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

/// <summary>
/// A factory for producing <see cref="IAuthenticatedEncryptorDescriptor"/>.
/// </summary>
public abstract class AlgorithmConfiguration
{
    internal const int KDK_SIZE_IN_BYTES = 512 / 8;

    /// <summary>
    /// Creates a new <see cref="IAuthenticatedEncryptorDescriptor"/> instance based on this
    /// configuration. The newly-created instance contains unique key material and is distinct
    /// from all other descriptors created by the <see cref="CreateNewDescriptor"/> method.
    /// </summary>
    /// <returns>A unique <see cref="IAuthenticatedEncryptorDescriptor"/>.</returns>
    public abstract IAuthenticatedEncryptorDescriptor CreateNewDescriptor();
}
