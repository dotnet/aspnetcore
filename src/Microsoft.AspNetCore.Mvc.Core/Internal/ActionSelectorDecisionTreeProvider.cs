// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <inheritdoc />
    public class ActionSelectorDecisionTreeProvider : IActionSelectorDecisionTreeProvider
    {
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private ActionSelectionDecisionTree _decisionTree;

        /// <summary>
        /// Creates a new <see cref="ActionSelectorDecisionTreeProvider"/>.
        /// </summary>
        /// <param name="actionDescriptorCollectionProvider">
        /// The <see cref="IActionDescriptorCollectionProvider"/>.
        /// </param>
        public ActionSelectorDecisionTreeProvider(
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        /// <inheritdoc />
        public IActionSelectionDecisionTree DecisionTree
        {
            get
            {
                var descriptors = _actionDescriptorCollectionProvider.ActionDescriptors;
                if (descriptors == null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatPropertyOfTypeCannotBeNull(
                            "ActionDescriptors",
                            _actionDescriptorCollectionProvider.GetType()));
                }

                if (_decisionTree == null || descriptors.Version != _decisionTree.Version)
                {
                    _decisionTree = new ActionSelectionDecisionTree(descriptors);
                }

                return _decisionTree;
            }
        }
    }
}