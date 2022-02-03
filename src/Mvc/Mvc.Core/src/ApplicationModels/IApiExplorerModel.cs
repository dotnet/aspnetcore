// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

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
