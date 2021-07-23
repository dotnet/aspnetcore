// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Forms
{
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
}
