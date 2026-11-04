// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Represents the type of HTML input to be rendered by a <see cref="InputDate{TValue}"/> component.
/// </summary>
public enum InputDateType
{
    /// <summary>
    /// Lets the user enter a date.
    /// </summary>
    Date,

    /// <summary>
    /// Lets the user enter both a date and a time.
    /// </summary>
    DateTimeLocal,

    /// <summary>
    /// Lets the user enter a month and a year.
    /// </summary>
    Month,

    /// <summary>
    /// Lets the user enter a time.
    /// </summary>
    Time,
}
