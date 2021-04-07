// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// An interface which is used to represent a something with a <see cref="BindingInfo"/>.
    /// </summary>
    public interface IBindingModel
    {
        /// <summary>
        /// The <see cref="BindingInfo"/>.
        /// </summary>
        BindingInfo? BindingInfo { get; set; }
    }
}
