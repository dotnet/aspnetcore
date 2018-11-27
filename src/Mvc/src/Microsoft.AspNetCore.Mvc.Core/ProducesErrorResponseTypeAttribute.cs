// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Specifies the type returned by default by controllers annotated with <see cref="ApiControllerAttribute"/>.
    /// <para>
    /// <see cref="Type"/> specifies the error model type associated with a <see cref="ProducesResponseTypeAttribute"/>
    /// for a client error (HTTP Status Code 4xx) when no value is provided. When no value is specified, MVC assumes the
    /// client error type to be <see cref="ProblemDetails"/>, if mapping client errors (<see cref="ApiBehaviorOptions.ClientErrorMapping"/>)
    /// is used.
    /// </para>
    /// <para>
    /// Use this <see cref="Attribute"/> to configure the default error type if your application uses a custom error type to respond.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ProducesErrorResponseTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ProducesErrorResponseTypeAttribute"/>.
        /// </summary>
        /// <param name="type">The error type. Use <see cref="void"/> to indicate the absence of a default error type.</param>
        public ProducesErrorResponseTypeAttribute(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// Gets the default error type.
        /// </summary>
        public Type Type { get; }
    }
}
