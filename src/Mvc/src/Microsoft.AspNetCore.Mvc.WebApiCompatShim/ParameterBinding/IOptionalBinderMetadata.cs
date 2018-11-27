// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// <para>
    /// An type that designates an optional parameter for the purposes
    /// of ASP.NET Web API action overloading. Optional parameters do not participate in overloading, and
    /// do not have to have a value for the action to be selected.
    /// </para>
    /// <para>
    /// This has no impact when used without ASP.NET Web API action overloading.
    /// </para>
    /// </summary>
    public interface IOptionalBinderMetadata
    {
        /// <summary>
        /// Gets a value indicating whether the parameter participates in ASP.NET Web API action overloading. If
        /// <c>true</c>, the parameter does not participate in overloading. Otherwise, it does.
        /// </summary>
        bool IsOptional { get; }
    }
}