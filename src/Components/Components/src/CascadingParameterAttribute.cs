// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Denotes the target member as a cascading component parameter. Its value will be
    /// supplied by the closest ancestor <see cref="CascadingValue{T}"/> component that
    /// supplies values with a compatible type and name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class CascadingParameterAttribute : Attribute
    {
        /// <summary>
        /// If specified, the parameter value will be supplied by the closest
        /// ancestor <see cref="CascadingValue{T}"/> that supplies a value with
        /// this name.
        ///
        /// If null, the parameter value will be supplied by the closest ancestor
        /// <see cref="CascadingValue{T}"/>  that supplies a value with a compatible
        /// type.
        /// </summary>
        public string Name { get; set; }
    }
}
