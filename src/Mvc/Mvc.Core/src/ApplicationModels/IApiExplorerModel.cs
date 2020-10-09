// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// An interface that allows access to an ApiExplorerModel.
    /// </summary>
    public interface IApiExplorerModel
    {
        /// <summary>
        /// The ApiExporerModel.
        /// </summary>
        ApiExplorerModel ApiExplorer { get; set; }
    }
}
