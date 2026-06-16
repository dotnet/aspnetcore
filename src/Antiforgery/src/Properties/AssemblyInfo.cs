// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Antiforgery;

// AntiforgeryValidationException was moved to Microsoft.AspNetCore.Http.Abstractions so that it can be
// shared with the cross-origin CSRF protection middleware (which lives in Microsoft.AspNetCore and cannot
// reference this assembly). The forward preserves binary compatibility for existing consumers.
#pragma warning disable RS0016 // Type is forwarded, not declared in this assembly
[assembly: TypeForwardedTo(typeof(AntiforgeryValidationException))]
#pragma warning restore RS0016
