// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Security.OpenIdConnect
{
    public interface INonceCache
    {
        string AddNonce(string nonce);
        bool TryRemoveNonce(string nonce);
        bool HasNonce(string nonce);
    }
}