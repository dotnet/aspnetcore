// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class SelectList : MultiSelectList
    {
        public SelectList([NotNull] IEnumerable items)
            : this(items, selectedValue: null)
        {
        }

        public SelectList([NotNull] IEnumerable items, object selectedValue)
            : this(items, dataValueField: null, dataTextField: null, selectedValue: selectedValue)
        {
        }

        public SelectList([NotNull] IEnumerable items, string dataValueField, string dataTextField)
            : this(items, dataValueField, dataTextField, selectedValue: null)
        {
        }

        public SelectList(
            [NotNull] IEnumerable items,
            string dataValueField,
            string dataTextField,
            object selectedValue)
            : base(items, dataValueField, dataTextField, ToEnumerable(selectedValue))
        {
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
            [NotNull] IEnumerable items,
            string dataValueField,
            string dataTextField,
            object selectedValue,
            string dataGroupField)
            : base(items, dataValueField, dataTextField, ToEnumerable(selectedValue), dataGroupField)
        {
            SelectedValue = selectedValue;
        }

        public object SelectedValue { get; private set; }

        private static IEnumerable ToEnumerable(object selectedValue)
        {
            return (selectedValue != null) ? new[] { selectedValue } : null;
        }
    }
}
