// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Authentication.OAuth.Claims
{
    /// <summary>
    /// A collection of ClaimActions used when mapping user data to Claims.
    /// </summary>
    public class ClaimActionCollection : IEnumerable<ClaimAction>
    {
        private IList<ClaimAction> Actions { get; } = new List<ClaimAction>();

        /// <summary>
        /// Remove all claim actions.
        /// </summary>
        public void Clear() => Actions.Clear();

        /// <summary>
        /// Remove all claim actions for the given ClaimType.
        /// </summary>
        /// <param name="claimType">The ClaimType of maps to remove.</param>
        public void Remove(string claimType)
        {
            var itemsToRemove = Actions.Where(map => string.Equals(claimType, map.ClaimType, StringComparison.OrdinalIgnoreCase)).ToList();
            itemsToRemove.ForEach(map => Actions.Remove(map));
        }

        /// <summary>
        /// Add a claim action to the collection.
        /// </summary>
        /// <param name="action">The claim action to add.</param>
        public void Add(ClaimAction action)
        {
            Actions.Add(action);
        }

        public IEnumerator<ClaimAction> GetEnumerator()
        {
            return Actions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Actions.GetEnumerator();
        }
    }
}