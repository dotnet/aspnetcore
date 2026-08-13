// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

// Shared between Microsoft.AspNetCore.Antiforgery (token-based validation) and Microsoft.AspNetCore
// (cross-origin CSRF protection middleware). Both producers write the request result through the public
// IAntiforgeryValidationFeature so downstream consumers (MVC, minimal APIs, Razor Components, FormFeature)
// can react uniformly. The error is typed as Exception so this file does not depend on the concrete
// exception type's assembly.
internal sealed class AntiforgeryValidationFeature(bool isValid, Exception? error) : IAntiforgeryValidationFeature
{
    public static readonly IAntiforgeryValidationFeature Valid = new AntiforgeryValidationFeature(true, null);

    public bool IsValid { get; } = isValid;
    public Exception? Error { get; } = error;
}
