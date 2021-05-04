// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Provides access to configuration for the data protection system, which allows the
    /// developer to configure default cryptographic algorithms, key storage locations,
    /// and the mechanism by which keys are protected at rest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the developer changes the at-rest key protection mechanism, it is intended that
    /// they also change the key storage location, and vice versa. For instance, a call to
    /// <see cref="DataProtectionBuilderExtensions.ProtectKeysWithCertificate(IDataProtectionBuilder,string)" /> should generally be accompanied by
    /// a call to <see cref="DataProtectionBuilderExtensions.PersistKeysToFileSystem(IDataProtectionBuilder,DirectoryInfo)"/>, or exceptions may
    /// occur at runtime due to the data protection system not knowing where to persist keys.
    /// </para>
    /// <para>
    /// Similarly, when a developer modifies the default protected payload cryptographic
    /// algorithms, they should also set an explicit key storage location.
    /// A call to <see cref="DataProtectionBuilderExtensions.UseCryptographicAlgorithms(IDataProtectionBuilder,AuthenticatedEncryptorConfiguration)"/>
    /// should therefore generally be paired with a call to <see cref="DataProtectionBuilderExtensions.PersistKeysToFileSystem(IDataProtectionBuilder,DirectoryInfo)"/>,
    /// for example.
    /// </para>
    /// <para>
    /// When the default cryptographic algorithms or at-rest key protection mechanisms are
    /// changed, they only affect <strong>new</strong> keys in the repository. The repository may
    /// contain existing keys that use older algorithms or protection mechanisms.
    /// </para>
    /// </remarks>
    public interface IDataProtectionBuilder
    {
        /// <summary>
        /// Provides access to the <see cref="IServiceCollection"/> passed to this object's constructor.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
