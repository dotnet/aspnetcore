// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Represents the optgroup HTML element and its attributes.
    /// In a select list, multiple groups with the same name are supported.
    /// They are compared with reference equality.
    /// </summary>
    public class SelectListGroup
    {
        /// <summary>
        /// Gets or sets a value that indicates whether this <see cref="SelectListGroup"/> is disabled.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Represents the value of the optgroup's label.
        /// </summary>
        public string Name { get; set; }
    }
}
