// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Wraps a JS interop argument, indicating that the value should not be serialized as JSON
    /// but instead should be passed as a reference.
    ///
    /// To avoid leaking memory, the reference must later be disposed by JS code or by .NET code.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to wrap.</typeparam>
    public sealed class DotNetObjectRef<TValue> : IDotNetObjectRef, IDisposable where TValue : class
    {
        private long? _trackingId;

        /// <summary>
        /// This API is for meant for JSON deserialization and should not be used by user code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DotNetObjectRef()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DotNetObjectRef{TValue}" />.
        /// </summary>
        /// <param name="value">The value to pass by reference.</param>
        internal DotNetObjectRef(TValue value)
        {
            Value = value;
            _trackingId = DotNetObjectRefManager.Current.TrackObject(this);
        }

        /// <summary>
        /// Gets the object instance represented by this wrapper.
        /// </summary>
        [JsonIgnore]
        public TValue Value { get; private set; }

        /// <summary>
        /// This API is for meant for JSON serialization and should not be used by user code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public long __dotNetObject
        {
            get => _trackingId.Value;
            set
            {
                if (_trackingId != null)
                {
                    throw new InvalidOperationException($"{nameof(DotNetObjectRef<TValue>)} cannot be reinitialized.");
                }

                _trackingId = value;
                Value = (TValue)DotNetObjectRefManager.Current.FindDotNetObject(value);
            }
        }

        object IDotNetObjectRef.Value => Value;

        /// <summary>
        /// Stops tracking this object reference, allowing it to be garbage collected
        /// (if there are no other references to it). Once the instance is disposed, it
        /// can no longer be used in interop calls from JavaScript code.
        /// </summary>
        public void Dispose()
        {
            DotNetObjectRefManager.Current.ReleaseDotNetObject(_trackingId.Value);
        }
    }
}
