// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#if NET45
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;

namespace Microsoft.AspNet.Security
{
    /// <summary>
    /// Implements a cert pinning validator passed on 
    /// http://datatracker.ietf.org/doc/draft-ietf-websec-key-pinning/?include_text=1
    /// </summary>
    public class CertificateSubjectPublicKeyInfoValidator : ICertificateValidator
    {
        private readonly HashSet<string> _validBase64EncodedSubjectPublicKeyInfoHashes;

        private readonly SubjectPublicKeyInfoAlgorithm _algorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateSubjectPublicKeyInfoValidator"/> class.
        /// </summary>
        /// <param name="validBase64EncodedSubjectPublicKeyInfoHashes">A collection of valid base64 encoded hashes of the certificate public key information blob.</param>
        /// <param name="algorithm">The algorithm used to generate the hashes.</param>
        public CertificateSubjectPublicKeyInfoValidator([NotNull] IEnumerable<string> validBase64EncodedSubjectPublicKeyInfoHashes, SubjectPublicKeyInfoAlgorithm algorithm)
        {
            _validBase64EncodedSubjectPublicKeyInfoHashes = new HashSet<string>(validBase64EncodedSubjectPublicKeyInfoHashes);

            if (_validBase64EncodedSubjectPublicKeyInfoHashes.Count == 0)
            {
                throw new ArgumentOutOfRangeException("validBase64EncodedSubjectPublicKeyInfoHashes");
            }

            if (_algorithm != SubjectPublicKeyInfoAlgorithm.Sha1 && _algorithm != SubjectPublicKeyInfoAlgorithm.Sha256)
            {
                throw new ArgumentOutOfRangeException("algorithm");
            }

            _algorithm = algorithm;
        }

        /// <summary>
        /// Validates at least one SPKI hash is known.
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
                return false;
            }

            using (HashAlgorithm algorithm = CreateHashAlgorithm())
            {
                foreach (var chainElement in chain.ChainElements)
                {
                    X509Certificate2 chainedCertificate = chainElement.Certificate;
                    string base64Spki = Convert.ToBase64String(algorithm.ComputeHash(ExtractSpkiBlob(chainedCertificate)));
                    if (_validBase64EncodedSubjectPublicKeyInfoHashes.Contains(base64Spki))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static byte[] ExtractSpkiBlob(X509Certificate2 certificate)
        {
            // Get a native cert_context from the managed X590Certificate2 instance.
            var certContext = (NativeMethods.CERT_CONTEXT)Marshal.PtrToStructure(certificate.Handle, typeof(NativeMethods.CERT_CONTEXT));

            // Pull the CERT_INFO structure from the context.
            var certInfo = (NativeMethods.CERT_INFO)Marshal.PtrToStructure(certContext.pCertInfo, typeof(NativeMethods.CERT_INFO));

            // And finally grab the key information, public key, algorithm and parameters from it.
            NativeMethods.CERT_PUBLIC_KEY_INFO publicKeyInfo = certInfo.SubjectPublicKeyInfo;

            // Now start encoding to ASN1.
            // First see how large the ASN1 representation is going to be.
            UInt32 blobSize = 0;
            var structType = new IntPtr(NativeMethods.X509_PUBLIC_KEY_INFO);
            if (!NativeMethods.CryptEncodeObject(NativeMethods.X509_ASN_ENCODING, structType, ref publicKeyInfo, null, ref blobSize))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }

            // Allocate enough space.
            var blob = new byte[blobSize];

            // Finally get the ASN1 representation.
            if (!NativeMethods.CryptEncodeObject(NativeMethods.X509_ASN_ENCODING, structType, ref publicKeyInfo, blob, ref blobSize))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }

            return blob;
        }

        [SuppressMessage("Microsoft.Security.Cryptography", "CA5354:SHA1CannotBeUsed", Justification = "Only used to verify cert hashes.")]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller is responsible for disposal.")]
        private HashAlgorithm CreateHashAlgorithm()
        {
            return _algorithm == SubjectPublicKeyInfoAlgorithm.Sha1 ? (HashAlgorithm)new SHA1CryptoServiceProvider() : new SHA256CryptoServiceProvider();
        }
    }
}
#endif