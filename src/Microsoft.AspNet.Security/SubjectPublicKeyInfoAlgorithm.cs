// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Security
{
    /// <summary>
    /// The algorithm used to generate the subject public key information blob hashes.
    /// </summary>
    public enum SubjectPublicKeyInfoAlgorithm
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sha", Justification = "It is correct.")] Sha1,
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sha", Justification = "It is correct.")] Sha256
    }
}
