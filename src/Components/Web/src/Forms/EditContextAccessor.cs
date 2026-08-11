// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.Forms;

internal static class EditContextAccessor
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "MarkAsModified")]
    internal static extern void MarkAsModified(EditContext editContext, in FieldIdentifier fieldIdentifier);
}
