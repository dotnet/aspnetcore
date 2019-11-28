// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    /// <summary>
    /// Options used to configure certificate authentication.
    /// </summary>
    public class CertificateAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Value indicating the types of certificates accepted by the authentication middleware.
        /// </summary>
        public CertificateTypes AllowedCertificateTypes { get; set; } = CertificateTypes.Chained;

        /// <summary>
        /// Collection of X509 certificates which are trusted components of the certificate chain.
        /// </summary>
        public X509Certificate2Collection CustomTrustStore { get; set; } = new X509Certificate2Collection();

        /// <summary>
        /// Method used to validate certificate chains against <see cref="CustomTrustStore"/>.
        /// </summary>
        /// <remarks>This property must be set to <see cref="X509ChainTrustMode.CustomRootTrust"/> to enable <see cref="CustomTrustStore"/> to be used in certificate chain validation.</remarks>
        public X509ChainTrustMode ChainTrustValidationMode { get; set; } = X509ChainTrustMode.System;

        /// <summary>
        /// Flag indicating whether the client certificate must be suitable for client
        /// authentication, either via the Client Authentication EKU, or having no EKUs
        /// at all. If the certificate chains to a root CA all certificates in the chain must be validated
        /// for the client authentication EKU.
        /// </summary>
        public bool ValidateCertificateUse { get; set; } = true;

        /// <summary>
        /// Flag indicating whether the client certificate validity period should be checked.
        /// </summary>
        public bool ValidateValidityPeriod { get; set; } = true;

        /// <summary>
        /// Specifies which X509 certificates in the chain should be checked for revocation.
        /// </summary>
        public X509RevocationFlag RevocationFlag { get; set; } = X509RevocationFlag.ExcludeRoot;

        /// <summary>
        /// Specifies conditions under which verification of certificates in the X509 chain should be conducted.
        /// </summary>
        public X509RevocationMode RevocationMode { get; set; } = X509RevocationMode.Online;

        /// <summary>
        /// The object provided by the application to process events raised by the certificate authentication middleware.
        /// The application may implement the interface fully, or it may create an instance of CertificateAuthenticationEvents
        /// and assign delegates only to the events it wants to process.
        /// </summary>
        public new CertificateAuthenticationEvents Events
        {
            get { return (CertificateAuthenticationEvents)base.Events; }

            set { base.Events = value; }
        }
    }
}
