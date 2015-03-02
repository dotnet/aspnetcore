// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    public interface INonceCache
    {
        string AddNonce(string nonce);
        bool TryRemoveNonce(string nonce);
        bool HasNonce(string nonce);
    }
}