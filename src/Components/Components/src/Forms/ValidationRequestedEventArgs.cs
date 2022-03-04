// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Provides information about the <see cref="EditContext.OnValidationRequested"/> event.
    /// </summary>
    public sealed class ValidationRequestedEventArgs
    {
        internal static readonly ValidationRequestedEventArgs Empty = new ValidationRequestedEventArgs();

        internal ValidationRequestedEventArgs()
        {
        }
    }
}
