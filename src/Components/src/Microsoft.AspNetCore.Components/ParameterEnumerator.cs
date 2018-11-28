// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.RenderTree;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// An enumerator that iterates through a <see cref="ParameterCollection"/>.
    /// </summary>
    public struct ParameterEnumerator
    {
        private RenderTreeFrameParameterEnumerator _directParamsEnumerator;
        private CascadingParameterEnumerator _cascadingParameterEnumerator;
        private bool _isEnumeratingDirectParams;

        internal ParameterEnumerator(RenderTreeFrame[] frames, int ownerIndex, IReadOnlyList<CascadingParameterState> cascadingParameters)
        {
            _directParamsEnumerator = new RenderTreeFrameParameterEnumerator(frames, ownerIndex);
            _cascadingParameterEnumerator = new CascadingParameterEnumerator(cascadingParameters);
            _isEnumeratingDirectParams = true;
        }

        /// <summary>
        /// Gets the current value of the enumerator.
        /// </summary>
        public Parameter Current => _isEnumeratingDirectParams
            ? _directParamsEnumerator.Current
            : _cascadingParameterEnumerator.Current;

        /// <summary>
        /// Instructs the enumerator to move to the next value in the sequence.
        /// </summary>
        /// <returns>A flag to indicate whether or not there is a next value.</returns>
        public bool MoveNext()
        {
            if (_isEnumeratingDirectParams)
            {
                if (_directParamsEnumerator.MoveNext())
                {
                    return true;
                }
                else
                {
                    _isEnumeratingDirectParams = false;
                }
            }

            return _cascadingParameterEnumerator.MoveNext();
        }

        struct RenderTreeFrameParameterEnumerator
        {
            private readonly RenderTreeFrame[] _frames;
            private readonly int _ownerIndex;
            private readonly int _ownerDescendantsEndIndexExcl;
            private int _currentIndex;
            private Parameter _current;

            internal RenderTreeFrameParameterEnumerator(RenderTreeFrame[] frames, int ownerIndex)
            {
                _frames = frames;
                _ownerIndex = ownerIndex;
                _ownerDescendantsEndIndexExcl = ownerIndex + _frames[ownerIndex].ElementSubtreeLength;
                _currentIndex = ownerIndex;
                _current = default;
            }

            public Parameter Current => _current;

            public bool MoveNext()
            {
                // Stop iteration if you get to the end of the owner's descendants...
                var nextIndex = _currentIndex + 1;
                if (nextIndex == _ownerDescendantsEndIndexExcl)
                {
                    return false;
                }

                // ... or if you get to its first non-attribute descendant (because attributes
                // are always before any other type of descendant)
                if (_frames[nextIndex].FrameType != RenderTreeFrameType.Attribute)
                {
                    return false;
                }

                _currentIndex = nextIndex;

                ref var frame = ref _frames[_currentIndex];
                _current = new Parameter(frame.AttributeName, frame.AttributeValue, false);

                return true;
            }
        }

        struct CascadingParameterEnumerator
        {
            private readonly IReadOnlyList<CascadingParameterState> _cascadingParameters;
            private int _currentIndex;
            private Parameter _current;

            public CascadingParameterEnumerator(IReadOnlyList<CascadingParameterState> cascadingParameters)
            {
                _cascadingParameters = cascadingParameters;
                _currentIndex = -1;
                _current = default;
            }

            public Parameter Current => _current;

            public bool MoveNext()
            {
                // Bail out early if there are no cascading parameters
                if (_cascadingParameters == null)
                {
                    return false;
                }

                var nextIndex = _currentIndex + 1;
                if (nextIndex < _cascadingParameters.Count)
                {
                    _currentIndex = nextIndex;

                    var state = _cascadingParameters[_currentIndex];
                    _current = new Parameter(state.LocalValueName, state.ValueSupplier.CurrentValue, true);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
