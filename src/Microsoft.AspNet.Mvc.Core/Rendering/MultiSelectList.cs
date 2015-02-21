// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Mvc.Rendering.Expressions;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class MultiSelectList : IEnumerable<SelectListItem>
    {
        private IList<SelectListGroup> _groups;

        public MultiSelectList([NotNull] IEnumerable items)
            : this(items, selectedValues: null)
        {
        }

        public MultiSelectList([NotNull] IEnumerable items, IEnumerable selectedValues)
            : this(items, dataValueField: null, dataTextField: null, selectedValues: selectedValues)
        {
        }

        public MultiSelectList([NotNull] IEnumerable items, string dataValueField, string dataTextField)
            : this(items, dataValueField, dataTextField, selectedValues: null)
        {
        }

        public MultiSelectList(
            [NotNull] IEnumerable items,
            string dataValueField,
            string dataTextField,
            IEnumerable selectedValues)
            : this(items, dataValueField, dataTextField, selectedValues, dataGroupField: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MultiSelectList class by using the items to include in the list,
        /// the data value field, the data text field, the selected values, and the data group field.
        /// </summary>
        /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
        /// <param name="dataValueField">The data value field. Used to match the Value property of the corresponding
        /// <see cref="SelectListItem"/>.</param>
        /// <param name="dataTextField">The data text field. Used to match the Text property of the corresponding
        /// <see cref="SelectListItem"/>.</param>
        /// <param name="selectedValues">The selected values field. Used to match the Selected property of the
        /// corresponding <see cref="SelectListItem"/>.</param>
        /// <param name="dataGroupField">The data group field. Used to match the Group property of the corresponding
        /// <see cref="SelectListItem"/>.</param>
        public MultiSelectList(
            [NotNull] IEnumerable items,
            string dataValueField,
            string dataTextField,
            IEnumerable selectedValues,
            string dataGroupField)
        {
            Items = items;
            DataValueField = dataValueField;
            DataTextField = dataTextField;
            SelectedValues = selectedValues;
            DataGroupField = dataGroupField;

            if (DataGroupField != null)
            {
                _groups = new List<SelectListGroup>();
            }
        }

        /// <summary>
        /// Gets or sets the data group field.
        /// </summary>
        public string DataGroupField { get; private set; }

        public string DataTextField { get; private set; }

        public string DataValueField { get; private set; }

        public IEnumerable Items { get; private set; }

        public IEnumerable SelectedValues { get; private set; }

        public virtual IEnumerator<SelectListItem> GetEnumerator()
        {
            return GetListItems().GetEnumerator();
        }

        internal IList<SelectListItem> GetListItems()
        {
            return (!string.IsNullOrEmpty(DataValueField)) ?
                GetListItemsWithValueField() :
                GetListItemsWithoutValueField();
        }

        private IList<SelectListItem> GetListItemsWithValueField()
        {
            var selectedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (SelectedValues != null)
            {
                selectedValues.UnionWith(from object value in SelectedValues
                                         select Convert.ToString(value, CultureInfo.CurrentCulture));
            }

            var listItems = from object item in Items
                            let value = Eval(item, DataValueField)
                            select new SelectListItem
                            {
                                Group = GetGroup(item),
                                Value = value,
                                Text = Eval(item, DataTextField),
                                Selected = selectedValues.Contains(value)
                            };
            return listItems.ToList();
        }

        private IList<SelectListItem> GetListItemsWithoutValueField()
        {
            var selectedValues = new HashSet<object>();
            if (SelectedValues != null)
            {
                selectedValues.UnionWith(SelectedValues.Cast<object>());
            }

            var listItems = from object item in Items
                            select new SelectListItem
                            {
                                Group = GetGroup(item),
                                Text = Eval(item, DataTextField),
                                Selected = selectedValues.Contains(item)
                            };
            return listItems.ToList();
        }

        private static string Eval(object container, string expression)
        {
            var value = container;
            if (!string.IsNullOrEmpty(expression))
            {
                var viewDataInfo = ViewDataEvaluator.Eval(container, expression);
                value = viewDataInfo.Value;
            }

            return Convert.ToString(value, CultureInfo.CurrentCulture);
        }

        private SelectListGroup GetGroup(object container)
        {
            if (_groups == null)
            {
                return null;
            }

            var groupName = Eval(container, DataGroupField);
            if (string.IsNullOrEmpty(groupName))
            {
                return null;
            }

            // We use StringComparison.CurrentCulture because the group name is used to display as the value of
            // optgroup HTML tag's label attribute.
            var group = _groups.FirstOrDefault(g => string.Equals(g.Name, groupName, StringComparison.CurrentCulture));
            if (group == null)
            {
                group = new SelectListGroup() { Name = groupName };
                _groups.Add(group);
            }

            return group;
        }

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
