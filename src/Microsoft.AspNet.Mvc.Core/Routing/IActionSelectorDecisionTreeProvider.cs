// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// Stores an <see cref="ActionSelectionDecisionTree"/> for the current value of
    /// <see cref="Actions.IActionDescriptorsCollectionProvider.ActionDescriptors"/>.
    /// </summary>
    public interface IActionSelectorDecisionTreeProvider
    {
        /// <summary>
        /// Gets the <see cref="Actions.IActionDescriptorsCollectionProvider"/>.
        /// </summary>
        IActionSelectionDecisionTree DecisionTree
        {
            get;
        }
    }
}