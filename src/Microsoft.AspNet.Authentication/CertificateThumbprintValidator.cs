// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Authentication
{
    /// <summary>
    /// Provides pinned certificate validation based on the certificate thumbprint.
    /// </summary>
    public class CertificateThumbprintValidator : ICertificateValidator
    {
        private readonly HashSet<string> _validCertificateThumbprints;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateThumbprintValidator"/> class.
        /// </summary>
        /// <param name="validThumbprints">A set of thumbprints which are valid for an HTTPS request.</param>
        public CertificateThumbprintValidator([NotNull] IEnumerable<string> validThumbprints)
        {
            _validCertificateThumbprints = new HashSet<string>(validThumbprints, StringComparer.OrdinalIgnoreCase);

            if (_validCertificateThumbprints.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(validThumbprints));
            }
        }

        /// <summary>
        /// Validates that the certificate thumbprints in the signing chain match at least one whitelisted thumbprint.
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation.</param>
        /// <param name="certificate">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
        /// <param name="sslPolicyErrors">One or more errors associated with the remote certificate.</param>
        /// <returns>A Boolean value that determines whether the specified certificate is accepted for authentication.</returns>
        public bool Validate(object sender, X509Certificate certificate, [NotNull] X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                return false;
            }

            if (chain.ChainElements.Count < 2)
            {
                // Self signed.
                return false;
            }

            foreach (var chainElement in chain.ChainElements)
            {
                string thumbprintToCheck = chainElement.Certificate.Thumbprint;

                if (thumbprintToCheck == null)
                {
                    continue;
                }

                if (_validCertificateThumbprints.Contains(thumbprintToCheck))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
