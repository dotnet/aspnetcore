// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Provides a <see cref="Type"/> which implements <see cref="IModelBinder"/>.
    /// </summary>
    public interface IBinderTypeProviderMetadata : IBindingSourceMetadata
    {
        /// <summary>
        /// A <see cref="Type"/> which implements either <see cref="IModelBinder"/>.
        /// </summary>
        Type BinderType { get; }
    }
}
