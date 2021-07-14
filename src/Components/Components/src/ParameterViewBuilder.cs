// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Provides a mechanism to build a <see cref="ParameterView" />.
    /// </summary>
    public readonly struct ParameterViewBuilder
    {
        private readonly RenderTreeFrame[] _frames;

        /// <summary>
        /// Constructs an instance of <see cref="ParameterViewBuilder" />.
        /// </summary>
        /// <param name="count">The maximum number of parameters that can be held.</param>
        public ParameterViewBuilder(int count)
        {
            _frames = new RenderTreeFrame[count + 1];
            _frames[0] = RenderTreeFrame
                .Element(0, ParameterView.GeneratedParameterViewElementName)
                .WithElementSubtreeLength(1);
        }

        /// <summary>
        /// Adds a parameter.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        public void Add(string name, object? value)
        {
            var nextIndex = _frames[0].ElementSubtreeLengthField++;
            _frames[nextIndex] = RenderTreeFrame.Attribute(0, name, value);
        }

        /// <summary>
        /// Supplies a completed <see cref="ParameterView" />.
        /// </summary>
        /// <returns>The <see cref="ParameterView" />.</returns>
        public ParameterView ToParameterView()
            => new ParameterView(ParameterViewLifetime.Unbound, _frames, 0);
    }
}
