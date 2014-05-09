// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Security.Principal;

namespace Microsoft.AspNet.Mvc
{
    // Can extract unique identifers for a claims-based identity
    public interface IClaimUidExtractor
    {
        string ExtractClaimUid(ClaimsIdentity identity);
    }
}