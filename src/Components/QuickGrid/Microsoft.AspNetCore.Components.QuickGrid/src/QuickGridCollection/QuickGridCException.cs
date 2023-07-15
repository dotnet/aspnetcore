// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection;
internal sealed class QuickGridCException : Exception
{
    public QuickGridCException()
    {
    }

    public QuickGridCException(string? message) : base(message)
    {
    }

    public QuickGridCException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
