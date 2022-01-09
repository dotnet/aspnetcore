// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

// Purpose of this interface, instead of just using ErrorBoundaryBase directly:
//
// [1] It keeps clear what is fundamental to an error boundary from the Renderer's perspective.
//     Anything more specific than this is just a useful pattern inside ErrorBoundaryBase.
// [2] It improves linkability. If an application isn't using error boundaries, then all of
//     ErrorBoundaryBase and its dependencies can be linked out, leaving only this interface.
//
// If we wanted, we could make this public, but it could lead to common antipatterns such as
// routinely marking all components as error boundaries (e.g., in a common base class) in an
// attempt to create "On Error Resume Next"-type behaviors.

internal interface IErrorBoundary
{
    void HandleException(Exception error);
}
