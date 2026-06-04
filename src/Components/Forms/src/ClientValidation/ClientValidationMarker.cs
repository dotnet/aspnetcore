// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Infrastructure type used by the framework to coordinate client-side validation activation
/// between validator components (in <c>Microsoft.AspNetCore.Components.Forms</c>) and the
/// rendering pipeline (in <c>Microsoft.AspNetCore.Components.Web</c>). Validators write a
/// non-null value into <see cref="EditContext.Properties"/> keyed by
/// <c>typeof(ClientValidationMarker)</c> to request client-side validation for the form; the
/// renderer checks for the same key to decide whether to emit the client-validation payload.
/// </summary>
/// <remarks>
/// Public for cross-assembly key visibility only. The type has no public members; it cannot
/// be constructed or used directly by application code.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ClientValidationMarker
{
    internal static readonly ClientValidationMarker Instance = new();

    private ClientValidationMarker() { }
}
