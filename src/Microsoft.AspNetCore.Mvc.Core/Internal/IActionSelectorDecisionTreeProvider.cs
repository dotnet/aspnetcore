// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Stores an <see cref="ActionSelectionDecisionTree"/> for the current value of
    /// <see cref="Infrastructure.IActionDescriptorCollectionProvider.ActionDescriptors"/>.
    /// </summary>
    public interface IActionSelectorDecisionTreeProvider
    {
        /// <summary>
        /// Gets the <see cref="Infrastructure.IActionDescriptorCollectionProvider"/>.
        /// </summary>
        IActionSelectionDecisionTree DecisionTree
        {
            get;
        }
    }
}