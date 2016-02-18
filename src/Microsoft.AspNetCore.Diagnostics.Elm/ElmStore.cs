// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Diagnostics.Elm
{
    public class ElmStore
    {
        private const int Capacity = 200;

        private LinkedList<ActivityContext> Activities { get; set; } = new LinkedList<ActivityContext>();

        /// <summary>
        /// Returns an IEnumerable of the contexts of the logs.
        /// </summary>
        /// <returns>An IEnumerable of <see cref="ActivityContext"/> objects where each context stores 
        /// information about a top level scope.</returns>
        public IEnumerable<ActivityContext> GetActivities()
        {
            for (var context = Activities.First; context != null; context = context.Next)
            {
                if (!context.Value.IsCollapsed && CollapseActivityContext(context.Value))
                {
                    Activities.Remove(context);
                }
            }
            return Activities;
        }

        /// <summary>
        /// Adds a new <see cref="ActivityContext"/> to the store.
        /// </summary>
        /// <param name="activity">The <see cref="ActivityContext"/> to be added to the store.</param>
        public void AddActivity(ActivityContext activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            lock (Activities)
            {
                Activities.AddLast(activity);
                while (Count() > Capacity)
                {
                    Activities.RemoveFirst();
                }
            }
        }

        /// <summary>
        /// Removes all activity contexts that have been stored.
        /// </summary>
        public void Clear()
        {
            Activities.Clear();
        }

        /// <summary>
        /// Returns the total number of logs in all activities in the store
        /// </summary>
        /// <returns>The total log count</returns>
        public int Count()
        {
            return Activities.Sum(a => Count(a.Root));
        }

        private int Count(ScopeNode node)
        {
            if (node == null)
            {
                return 0;
            }
            var sum = node.Messages.Count;
            foreach (var child in node.Children)
            {
                sum += Count(child);
            }
            return sum;
        }

        /// <summary>
        /// Removes any nodes on the context's scope tree that doesn't have any logs
        /// This may occur as a result of the filters turned on
        /// </summary>
        /// <param name="context">The context who's node should be condensed</param>
        /// <returns>true if the node has been condensed to null, false otherwise</returns>
        private bool CollapseActivityContext(ActivityContext context)
        {
            context.Root = CollapseHelper(context.Root);
            context.IsCollapsed = true;
            return context.Root == null;
        }

        private ScopeNode CollapseHelper(ScopeNode node)
        {
            if (node == null)
            {
                return node;
            }
            for (int i = 0; i < node.Children.Count; i++)
            {
                node.Children[i] = CollapseHelper(node.Children[i]);
            }
            node.Children.RemoveAll(c => c == null);
            if (node.Children.Count == 0 && node.Messages.Count == 0)
            {
                return null;
            }
            else
            {
                return node;
            }
        }
    }
}