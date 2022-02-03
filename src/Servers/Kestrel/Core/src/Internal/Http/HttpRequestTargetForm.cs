// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal enum HttpRequestTarget
{
    Unknown = -1,
    // origin-form is the most common
    OriginForm,
    AbsoluteForm,
    AuthorityForm,
    AsteriskForm
}
