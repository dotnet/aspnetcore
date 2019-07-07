// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Methods used internally by @bind syntax. Not intended to be used directly.
    /// </summary>
    public static class BindMethods
    {
        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static T GetValue<T>(T value) => value;

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static string GetValue(DateTime value, string format) =>
            value == default ? null
            : (format == null ? value.ToString() : value.ToString(format));

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static string GetValue(DateTime? value, string format) =>
            value == default ? null
            : (format == null ? value.ToString() : value.Value.ToString(format));

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static string GetValue(DateTimeOffset value, string format) =>
            value == default ? null
            : (format == null ? value.ToString() : value.ToString(format));

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static string GetValue(DateTimeOffset? value, string format) =>
            value == default ? null
            : (format == null ? value.ToString() : value.Value.ToString(format));
    }
}
