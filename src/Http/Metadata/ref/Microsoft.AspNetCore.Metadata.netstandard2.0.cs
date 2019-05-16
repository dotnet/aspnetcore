// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authorization
{
    public partial interface IAllowAnonymous
    {
    }
    public partial interface IAuthorizeData
    {
        string AuthenticationSchemes { get; set; }
        string Policy { get; set; }
        string Roles { get; set; }
    }
}
