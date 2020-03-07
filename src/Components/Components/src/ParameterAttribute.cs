// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Denotes the target member as a component parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ParameterAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value that determines whether the parameter will capture values that
        /// don't match any other parameter.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="CaptureUnmatchedValues"/> allows a component to accept arbitrary additional
        /// attributes, and pass them to another component, or some element of the underlying markup.
        /// </para>
        /// <para>
        /// <see cref="CaptureUnmatchedValues"/> can be used on at most one parameter per component.
        /// </para>
        /// <para>
        /// <see cref="CaptureUnmatchedValues"/> should only be applied to parameters of a type that
        /// can be used with <see cref="RenderTreeBuilder.AddMultipleAttributes(int, System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{string, System.Object}})"/>
        /// such as <see cref="Dictionary{String, Object}"/>.
        /// </para>
        /// </remarks>
        public bool CaptureUnmatchedValues { get; set; }
    }
}
