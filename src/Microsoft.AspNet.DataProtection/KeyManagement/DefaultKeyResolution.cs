// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    internal struct DefaultKeyResolution
    {
        /// <summary>
        /// The default key, may be null if no key is a good default candidate.
        /// </summary>
        public IKey DefaultKey;

        /// <summary>
        /// 'true' if a new key should be persisted to the keyring, 'false' otherwise.
        /// This value may be 'true' even if a valid default key was found.
        /// </summary>
        public bool ShouldGenerateNewKey;
    }
}
