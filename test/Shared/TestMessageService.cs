// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity.Test
{
    public class TestMessageService : IIdentityMessageService
    {
        public IdentityMessage Message { get; set; }

        public Task SendAsync(IdentityMessage message, CancellationToken cancellationToken = default(CancellationToken))
        {
            Message = message;
            return Task.FromResult(0);
        }
    }

}