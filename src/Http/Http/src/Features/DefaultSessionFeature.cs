// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// This type exists only for the purpose of unit testing where the user can directly set the
    /// <see cref="HttpContext.Session"/> property without the need for creating a <see cref="ISessionFeature"/>.
    /// </summary>
    public class DefaultSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; }
    }
}
