// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Security.Cookies;

namespace Microsoft.AspNet.Identity
{
    public interface ISecurityStampValidator
    {
        Task Validate(CookieValidateIdentityContext context, ClaimsIdentity identity);
    }
}