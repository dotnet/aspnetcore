// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An action result which formats the given <typeparamref name="T"/> as JSON.
    /// </summary>
    public class JsonResult<T> : JsonResult
    {
        /// <summary>
        /// Creates a new <see cref="JsonResult"/> with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON.</param>
        public JsonResult(T value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new <see cref="JsonResult"/> with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON.</param>
        /// <param name="serializerSettings">
        /// The serializer settings to be used by the formatter.
        /// <para>
        /// When using <c>System.Text.Json</c>, this should be an instance of <see cref="JsonSerializerOptions" />.
        /// </para>
        /// <para>
        /// When using <c>Newtonsoft.Json</c>, this should be an instance of <c>JsonSerializerSettings</c>.
        /// </para>
        /// </param>
        public JsonResult(T value, object serializerSettings)
            : base(value, serializerSettings)
        {
        }
    }
}
