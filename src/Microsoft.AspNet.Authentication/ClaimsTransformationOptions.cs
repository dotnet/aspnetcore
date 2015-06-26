// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Authentication
{
    public class ClaimsTransformationOptions
    {
        public IClaimsTransformer Transformer { get; set; }
    }

    public interface IClaimsTransformer
    {
        Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal);
        ClaimsPrincipal Transform(ClaimsPrincipal principal);
    }

    public class ClaimsTransformer : IClaimsTransformer
    {
        public Func<ClaimsPrincipal, Task<ClaimsPrincipal>> TransformAsyncDelegate { get; set; }
        public Func<ClaimsPrincipal, ClaimsPrincipal> TransformSyncDelegate { get; set; }

        public virtual ClaimsPrincipal Transform(ClaimsPrincipal principal)
        {
            if (TransformSyncDelegate != null)
            {
                return TransformSyncDelegate(principal);
            }
            return principal;
        }

        public virtual Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (TransformAsyncDelegate != null)
            {
                return TransformAsyncDelegate(principal);
            }
            return Task.FromResult(Transform(principal));
        }
    }
}
