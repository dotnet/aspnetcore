// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json.Serialization;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Wraps a JS interop argument, indicating that the value should not be serialized as JSON
    /// but instead should be passed as a reference.
    ///
    /// To avoid leaking memory, the reference must later be disposed by JS code or by .NET code.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to wrap.</typeparam>
    [JsonConverter(typeof(DotNetObjectReferenceJsonConverterFactory))]
    public sealed class DotNetObjectReference<TValue> : IDotNetObjectReference, IDisposable where TValue : class
    {
        private readonly DotNetObjectReferenceManager _referenceManager;
        private readonly TValue _value;
        private readonly long _objectId;

        /// <summary>
        /// Initializes a new instance of <see cref="DotNetObjectReference{TValue}" />.
        /// </summary>
        /// <param name="referenceManager"></param>
        /// <param name="value">The value to pass by reference.</param>
        internal DotNetObjectReference(DotNetObjectReferenceManager referenceManager, TValue value)
        {
            _referenceManager = referenceManager;
            _objectId = _referenceManager.TrackObject(this);
            _value = value;
        }

        /// <summary>
        /// Gets the object instance represented by this wrapper.
        /// </summary>
        public TValue Value
        {
            get
            {
                ThrowIfDisposed();
                return _value;
            }
        }

        internal long ObjectId
        {
            get
            {
                ThrowIfDisposed();
                return _objectId;
            }
        }

        object IDotNetObjectReference.Value => Value;

        internal bool Disposed { get; private set; }

        /// <summary>
        /// Stops tracking this object reference, allowing it to be garbage collected
        /// (if there are no other references to it). Once the instance is disposed, it
        /// can no longer be used in interop calls from JavaScript code.
        /// </summary>
        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
                _referenceManager.ReleaseDotNetObject(_objectId);
            }
        }

        private void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
