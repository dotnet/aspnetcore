// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// Represents a collection of parameters supplied to an <see cref="IComponent"/>
    /// by its parent in the render tree.
    /// </summary>
    public readonly struct ParameterCollection
    {
        private readonly RenderTreeFrame[] _frames;
        private readonly int _ownerIndex;

        internal ParameterCollection(RenderTreeFrame[] frames, int ownerIndex)
        {
            _frames = frames;
            _ownerIndex = ownerIndex;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ParameterCollection"/>.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public ParameterEnumerator GetEnumerator()
            => new ParameterEnumerator(_frames, _ownerIndex);
    }
}
