// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An entry in a <see cref="ModelStateDictionary"/>.
    /// </summary>
    public class ModelStateEntry
    {
        /// <summary>
        /// Gets the raw value from the request associated with this entry.
        /// </summary>
        public object RawValue { get; set; }

        /// <summary>
        /// Gets the set of values contained in <see cref="RawValue"/>, joined into a comma-separated string.
        /// </summary>
        public string AttemptedValue { get; set; }

        /// <summary>
        /// Gets the <see cref="ModelErrorCollection"/> for this entry.
        /// </summary>
        public ModelErrorCollection Errors { get; } = new ModelErrorCollection();

        /// <summary>
        /// Gets or sets the <see cref="ModelValidationState"/> for this entry.
        /// </summary>
        public ModelValidationState ValidationState { get; set; }
    }
}
