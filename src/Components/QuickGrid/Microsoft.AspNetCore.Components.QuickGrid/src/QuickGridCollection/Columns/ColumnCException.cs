// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns;
internal class ColumnCException : Exception
{
    public ColumnCException()
    {
    }

    public ColumnCException(string? message) : base(message)
    {
    }

    public ColumnCException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
