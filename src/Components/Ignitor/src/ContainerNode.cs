// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

#nullable enable
namespace Ignitor
{
    public abstract class ContainerNode : Node
    {
        private readonly List<Node> _children;

        protected ContainerNode()
        {
            _children = new List<Node>();
        }

        public IReadOnlyList<Node> Children => _children;

        public void InsertLogicalChild(Node child, int childIndex)
        {
            if (child is LogicalContainerNode comment && comment.Children.Count > 0)
            {
                // There's nothing to stop us implementing support for this scenario, and it's not difficult
                // (after inserting 'child' itself, also iterate through its logical children and physically
                // put them as following-siblings in the DOM). However there's no scenario that requires it
                // presently, so if we did implement it there'd be no good way to have tests for it.
                throw new Exception("Not implemented: inserting non-empty logical container");
            }

            if (child.Parent != null)
            {
                // Likewise, we could easily support this scenario too (in this 'if' block, just splice
                // out 'child' from the logical children array of its previous logical parent by using
                // Array.prototype.indexOf to determine its previous sibling index).
                // But again, since there's not currently any scenario that would use it, we would not
                // have any test coverage for such an implementation.
                throw new NotSupportedException("Not implemented: moving existing logical children");
            }

            if (childIndex < Children.Count)
            {
                // Insert
                _children.Insert(childIndex, child);
            }
            else
            {
                // Append
                _children.Add(child);
            }

            child.Parent = this;
        }

        public ContainerNode CreateAndInsertContainer(int childIndex)
        {
            var containerElement = new LogicalContainerNode();
            InsertLogicalChild(containerElement, childIndex);
            return containerElement;
        }

        public ComponentNode CreateAndInsertComponent(int componentId, int childIndex)
        {
            var componentElement = new ComponentNode(componentId);
            InsertLogicalChild(componentElement, childIndex);
            return componentElement;
        }

        public void RemoveLogicalChild(int childIndex)
        {
            var childToRemove = Children[childIndex];
            _children.RemoveAt(childIndex);

            // If it's a logical container, also remove its descendants
            if (childToRemove is LogicalContainerNode container)
            {
                while (container.Children.Count > 0)
                {
                    container.RemoveLogicalChild(0);
                }
            }
        }
    }
}
#nullable restore
