// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Supplies information about an change event that is being raised.
    /// </summary>
    public class ChangeEventArgs : EventArgs
    {
        private string?[]? _multipleValues;

        /// <summary>
        /// Gets or sets the new value.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the new array of values.
        /// </summary>
        public string?[]? MultipleValues
        {
            get => _multipleValues;
            set
            {
                _multipleValues = value;
                Value = value;
            }
        }
    }
}
