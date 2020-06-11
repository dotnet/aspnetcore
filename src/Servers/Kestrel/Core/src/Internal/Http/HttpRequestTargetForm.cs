// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal enum HttpRequestTarget
    {
        Unknown = -1,
        // origin-form is the most common
        OriginForm,
        AbsoluteForm,
        AuthorityForm,
        AsteriskForm
    }
}
