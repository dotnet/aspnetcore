// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// Represents a single parameter supplied to an <see cref="IComponent"/>
    /// by its parent in the render tree.
    /// </summary>
    public readonly struct Parameter
    {
        private readonly RenderTreeFrame[] _frames;
        private readonly int _frameIndex;

        internal Parameter(RenderTreeFrame[] frames, int currentIndex)
        {
            _frames = frames;
            _frameIndex = currentIndex;
        }

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name
            => _frames[_frameIndex].AttributeName;

        /// <summary>
        /// Gets the value of the parameter.
        /// </summary>
        public object Value
            => _frames[_frameIndex].AttributeValue;
    }
}
