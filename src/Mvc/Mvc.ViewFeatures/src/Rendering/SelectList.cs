// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Represents a list that lets users select a single item.
    /// This class is typically rendered as an HTML <c>&lt;select&gt;</c> element with the specified collection
    /// of <see cref="SelectListItem"/> objects.
    /// </summary>
    public class SelectList : MultiSelectList
    {
        public SelectList(IEnumerable items)
            : this(items, selectedValue: null)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
        }

        public SelectList(IEnumerable items, object selectedValue)
            : this(items, dataValueField: null, dataTextField: null, selectedValue: selectedValue)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
        }

        public SelectList(IEnumerable items, string dataValueField, string dataTextField)
            : this(items, dataValueField, dataTextField, selectedValue: null)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
        }

        public SelectList(
            IEnumerable items,
            string dataValueField,
            string dataTextField,
            object selectedValue)
            : base(items, dataValueField, dataTextField, ToEnumerable(selectedValue))
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            SelectedValue = selectedValue;
        }

        /// <summary>
        /// Initializes a new instance of the SelectList class by using the specified items for the list,
        /// the data value field, the data text field, a selected value, and the data group field.
        /// </summary>
        /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
        /// <param name="dataValueField">The data value field. Used to match the Value property of the corresponding
        /// <see cref="SelectListItem"/>.</param>
        /// <param name="dataTextField">The data text field. Used to match the Text property of the corresponding
        /// <see cref="SelectListItem"/>.</param>
        /// <param name="selectedValue">The selected values. Used to match the Selected property of the corresponding
        /// <see cref="SelectListItem"/>.</param>
        /// <param name="dataGroupField">The data group field. Used to match the Group property of the corresponding
        /// <see cref="SelectListItem"/>.</param>
        public SelectList(
            IEnumerable items,
            string dataValueField,
            string dataTextField,
            object selectedValue,
            string dataGroupField)
            : base(items, dataValueField, dataTextField, ToEnumerable(selectedValue), dataGroupField)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            SelectedValue = selectedValue;
        }

        public object SelectedValue { get; }

        private static IEnumerable ToEnumerable(object selectedValue)
        {
            return (selectedValue != null) ? new[] { selectedValue } : null;
        }
    }
}
