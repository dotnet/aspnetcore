// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Http;

internal class ProblemDetailsResponseMetadata : IProducesErrorResponseMetadata
{
    public Type? Type => typeof(ProblemDetails);
}
