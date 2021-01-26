// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Represents an item in a <see cref="SelectList"/> or <see cref="MultiSelectList"/>.
    /// This class is typically rendered as an HTML <c>&lt;option&gt;</c> element with the specified
    /// attribute values.
    /// </summary>
    public class SelectListItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SelectListItem"/>.
        /// </summary>
        public SelectListItem() { }

        /// <summary>
        /// Initializes a new instance of <see cref="SelectListItem"/>.
        /// </summary>
        /// <param name="text">The display text of this <see cref="SelectListItem"/>.</param>
        /// <param name="value">The value of this <see cref="SelectListItem"/>.</param>
        public SelectListItem(string text, string value)
            : this()
        {
            Text = text;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SelectListItem"/>.
        /// </summary>
        /// <param name="text">The display text of this <see cref="SelectListItem"/>.</param>
        /// <param name="value">The value of this <see cref="SelectListItem"/>.</param>
        /// <param name="selected">Value that indicates whether this <see cref="SelectListItem"/> is selected.</param>
        public SelectListItem(string text, string value, bool selected)
            : this(text, value)
        {
            Selected = selected;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SelectListItem"/>.
        /// </summary>
        /// <param name="text">The display text of this <see cref="SelectListItem"/>.</param>
        /// <param name="value">The value of this <see cref="SelectListItem"/>.</param>
        /// <param name="selected">Value that indicates whether this <see cref="SelectListItem"/> is selected.</param>
        /// <param name="disabled">Value that indicates whether this <see cref="SelectListItem"/> is disabled.</param>
        public SelectListItem(string text, string value, bool selected, bool disabled)
            : this(text, value, selected)
        {
            Disabled = disabled;
        }

        /// <summary>
        /// Gets or sets a value that indicates whether this <see cref="SelectListItem"/> is disabled.
        /// This property is typically rendered as a <c>disabled="disabled"</c> attribute in the HTML
        /// <c>&lt;option&gt;</c> element.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Represents the optgroup HTML element this item is wrapped into.
        /// In a select list, multiple groups with the same name are supported.
        /// They are compared with reference equality.
        /// </summary>
        public SelectListGroup Group { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether this <see cref="SelectListItem"/> is selected.
        /// This property is typically rendered as a <c>selected="selected"</c> attribute in the HTML
        /// <c>&lt;option&gt;</c> element.
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates the display text of this <see cref="SelectListItem"/>.
        /// This property is typically rendered as the inner HTML in the HTML <c>&lt;option&gt;</c> element.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates the value of this <see cref="SelectListItem"/>.
        /// This property is typically rendered as a <c>value="..."</c> attribute in the HTML
        /// <c>&lt;option&gt;</c> element.
        /// </summary>
        public string Value { get; set; }
    }
}
