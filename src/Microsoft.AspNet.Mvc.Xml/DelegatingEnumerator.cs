// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Xml
{
    /// <summary>
    /// Delegates enumeration of elements to the original enumerator and wraps the items
    /// with the supplied <see cref="IWrapperProvider"/>.
    /// </summary>
    /// <typeparam name="TWrapped">The type to which the individual elements need to be wrapped to.</typeparam>
    /// <typeparam name="TDeclared">The original type of the element being wrapped.</typeparam>
    public class DelegatingEnumerator<TWrapped, TDeclared> : IEnumerator<TWrapped>
    {
        private readonly IEnumerator<TDeclared> _inner;
        private readonly IWrapperProvider _wrapperProvider;

        /// <summary>
        /// Initializes a <see cref="DelegatingEnumerable{TWrapped, TDeclared}"/> which enumerates 
        /// over the elements of the original enumerator and wraps them using the supplied
        /// <see cref="IWrapperProvider"/>.
        /// </summary>
        /// <param name="inner">The original enumerator.</param>
        /// <param name="wrapperProvider">The wrapper provider to wrap individual elements.</param>
        public DelegatingEnumerator([NotNull] IEnumerator<TDeclared> inner, IWrapperProvider wrapperProvider)
        {
            _inner = inner;
            _wrapperProvider = wrapperProvider;
        }

        /// <inheritdoc />
        public TWrapped Current
        {
            get
            {
                object obj = _inner.Current;
                if (_wrapperProvider == null)
                {
                    // if there is no wrapper, then this cast should not fail
                    return (TWrapped)obj;
                }

                return (TWrapped)_wrapperProvider.Wrap(obj);
            }
        }

        /// <inheritdoc />
        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _inner.Dispose();
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            return _inner.MoveNext();
        }

        /// <inheritdoc />
        public void Reset()
        {
            _inner.Reset();
        }
    }
}