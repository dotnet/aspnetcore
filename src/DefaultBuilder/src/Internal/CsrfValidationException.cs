// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// The <see cref="Exception"/> recorded on <see cref="IAntiforgeryValidationFeature"/> when the
/// cross-origin CSRF protection middleware marks a request as invalid.
/// </summary>
internal sealed class CsrfValidationException : Exception
{
    public CsrfValidationException(string message)
        : base(message)
    {
    }
}
