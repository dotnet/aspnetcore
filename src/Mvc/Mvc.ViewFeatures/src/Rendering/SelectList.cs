// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Represents a list that lets users select a single item.
/// This class is typically rendered as an HTML <c>&lt;select&gt;</c> element with the specified collection
/// of <see cref="SelectListItem"/> objects.
/// </summary>
public class SelectList : MultiSelectList
{
    /// <summary>
    /// Initialize a new instance of <see cref="SelectList"/>.
    /// </summary>
    /// <param name="items">The items.</param>
    public SelectList(IEnumerable items)
        : this(items, selectedValue: null)
    {
        ArgumentNullException.ThrowIfNull(items);
    }

    /// <summary>
    /// Initialize a new instance of <see cref="SelectList"/>.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="selectedValue">The selected value.</param>
    public SelectList(IEnumerable items, object selectedValue)
        : this(items, dataValueField: null, dataTextField: null, selectedValue: selectedValue)
    {
        ArgumentNullException.ThrowIfNull(items);
    }

    /// <summary>
    /// Initialize a new instance of <see cref="SelectList"/>.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="dataValueField">The data value field.</param>
    /// <param name="dataTextField">The data text field.</param>
    public SelectList(IEnumerable items, string dataValueField, string dataTextField)
        : this(items, dataValueField, dataTextField, selectedValue: null)
    {
        ArgumentNullException.ThrowIfNull(items);
    }

    /// <summary>
    /// Initialize a new instance of <see cref="SelectList"/>.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="dataValueField">The data value field.</param>
    /// <param name="dataTextField">The data text field.</param>
    /// <param name="selectedValue">The selected value.</param>
    public SelectList(
        IEnumerable items,
        string dataValueField,
        string dataTextField,
        object selectedValue)
        : base(items, dataValueField, dataTextField, ToEnumerable(selectedValue))
    {
        ArgumentNullException.ThrowIfNull(items);

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
        ArgumentNullException.ThrowIfNull(items);

        SelectedValue = selectedValue;
    }

    /// <summary>
    /// The selected value.
    /// </summary>
    public object SelectedValue { get; }

    private static IEnumerable ToEnumerable(object selectedValue)
    {
        return (selectedValue != null) ? new[] { selectedValue } : null;
    }
}
