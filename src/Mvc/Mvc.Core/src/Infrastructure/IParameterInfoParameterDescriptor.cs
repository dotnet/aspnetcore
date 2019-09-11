// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// A <see cref="ParameterDescriptor"/> for action parameters.
    /// </summary>
    public interface IParameterInfoParameterDescriptor
    {
        /// <summary>
        /// Gets the <see cref="System.Reflection.ParameterInfo"/>.
        /// </summary>
        ParameterInfo ParameterInfo { get; }
    }
}
