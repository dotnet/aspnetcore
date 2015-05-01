// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An type that designates an optional parameter for the purposes
    /// of WebAPI action overloading. Optional parameters do not participate in overloading, and 
    /// do not have to have a value for the action to be selected.
    /// 
    /// This has no impact when used without WebAPI action overloading.
    /// </summary>
    public interface IOptionalBinderMetadata
    {
        bool IsOptional { get; }
    }
}