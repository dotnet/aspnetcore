// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Security
{
    public interface IAuthorizationHandler
    {
        Task HandleAsync(AuthorizationContext context);
        //void Handle(AuthorizationContext context);
    }
}
