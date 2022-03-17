// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Supplies information about an error event that is being raised.
/// </summary>
public class ErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets a a human-readable error message describing the problem.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets the name of the script file in which the error occurred.
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    /// Gets the line number of the script file on which the error occurred.
    /// </summary>
    public int Lineno { get; set; }

    /// <summary>
    /// Gets the column number of the script file on which the error occurred.
    /// </summary>
    public int Colno { get; set; }

    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    public string? Type { get; set; }
}
