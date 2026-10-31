// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// Display metadata details for a <see cref="ModelMetadata"/>.
/// </summary>
public class DisplayMetadata
{
    private Func<string?> _displayFormatStringProvider = () => null;
    private Func<string?> _editFormatStringProvider = () => null;
    private Func<string?> _nullDisplayTextProvider = () => null;

    /// <summary>
    /// Gets a set of additional values. See <see cref="ModelMetadata.AdditionalValues"/>
    /// </summary>
    public IDictionary<object, object> AdditionalValues { get; } = new Dictionary<object, object>();

    /// <summary>
    /// Gets or sets a value indicating whether or not to convert an empty string value or one containing only
    /// whitespace characters to <see langword="null"/> when representing a model as text. See
    /// <see cref="ModelMetadata.ConvertEmptyStringToNull"/>
    /// </summary>
    public bool ConvertEmptyStringToNull { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the data type.
    /// See <see cref="ModelMetadata.DataTypeName"/>
    /// </summary>
    public string? DataTypeName { get; set; }

    /// <summary>
    /// Gets or sets a delegate which is used to get a value for the
    /// model description. See <see cref="ModelMetadata.Description"/>.
    /// </summary>
    public Func<string?>? Description { get; set; }

    /// <summary>
    /// Gets or sets a display format string for the model.
    /// See <see cref="ModelMetadata.DisplayFormatString"/>
    /// </summary>
    /// <remarks>
    /// Setting <see cref="DisplayFormatString"/> also changes <see cref="DisplayFormatStringProvider"/>.
    /// </remarks>
    public string? DisplayFormatString
    {
        get
        {
            return DisplayFormatStringProvider();
        }
        set
        {
            DisplayFormatStringProvider = () => value;
        }
    }

    /// <summary>
    /// Gets or sets a delegate which is used to get the display format string for the model. See
    /// <see cref="ModelMetadata.DisplayFormatString"/>.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="DisplayFormatStringProvider"/> also changes <see cref="DisplayFormatString"/>.
    /// </remarks>
    public Func<string?> DisplayFormatStringProvider
    {
        get
        {
            return _displayFormatStringProvider;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _displayFormatStringProvider = value;
        }
    }

    /// <summary>
    /// Gets or sets a delegate which is used to get a value for the
    /// display name of the model. See <see cref="ModelMetadata.DisplayName"/>.
    /// </summary>
    public Func<string?>? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets an edit format string for the model.
    /// See <see cref="ModelMetadata.EditFormatString"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setting <see cref="EditFormatString"/> also changes <see cref="EditFormatStringProvider"/>.
    /// </para>
    /// <para>
    /// <see cref="IDisplayMetadataProvider"/> instances that set this property to a non-<see langword="null"/>,
    /// non-empty, non-default value should also set <see cref="HasNonDefaultEditFormat"/> to
    /// <see langword="true"/>.
    /// </para>
    /// </remarks>
    public string? EditFormatString
    {
        get
        {
            return EditFormatStringProvider();
        }
        set
        {
            EditFormatStringProvider = () => value;
        }
    }

    /// <summary>
    /// Gets or sets a delegate which is used to get the edit format string for the model. See
    /// <see cref="ModelMetadata.EditFormatString"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setting <see cref="EditFormatStringProvider"/> also changes <see cref="EditFormatString"/>.
    /// </para>
    /// <para>
    /// <see cref="IDisplayMetadataProvider"/> instances that set this property to a non-default value should
    /// also set <see cref="HasNonDefaultEditFormat"/> to <see langword="true"/>.
    /// </para>
    /// </remarks>
    public Func<string?> EditFormatStringProvider
    {
        get
        {
            return _editFormatStringProvider;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _editFormatStringProvider = value;
        }
    }

    /// <summary>
    /// Gets the ordered and grouped display names and values of all <see cref="System.Enum"/> values in
    /// <see cref="ModelMetadata.UnderlyingOrModelType"/>. See
    /// <see cref="ModelMetadata.EnumGroupedDisplayNamesAndValues"/>.
    /// </summary>
    public IEnumerable<KeyValuePair<EnumGroupAndName, string>>? EnumGroupedDisplayNamesAndValues { get; set; }

    /// <summary>
    /// Gets the names and values of all <see cref="System.Enum"/> values in
    /// <see cref="ModelMetadata.UnderlyingOrModelType"/>. See <see cref="ModelMetadata.EnumNamesAndValues"/>.
    /// </summary>
    // This could be implemented in DefaultModelMetadata. But value should be cached.
    public IReadOnlyDictionary<string, string>? EnumNamesAndValues { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the model has a non-default edit format.
    /// See <see cref="ModelMetadata.HasNonDefaultEditFormat"/>
    /// </summary>
    public bool HasNonDefaultEditFormat { get; set; }

    /// <summary>
    /// Gets or sets a value indicating if the surrounding HTML should be hidden.
    /// See <see cref="ModelMetadata.HideSurroundingHtml"/>
    /// </summary>
    public bool HideSurroundingHtml { get; set; }

    /// <summary>
    /// Gets or sets a value indicating if the model value should be HTML encoded.
    /// See <see cref="ModelMetadata.HtmlEncode"/>
    /// </summary>
    public bool HtmlEncode { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether <see cref="ModelMetadata.UnderlyingOrModelType"/> is for an
    /// <see cref="System.Enum"/>. See <see cref="ModelMetadata.IsEnum"/>.
    /// </summary>
    // This could be implemented in DefaultModelMetadata. But value is needed in the details provider.
    public bool IsEnum { get; set; }

    /// <summary>
    /// Gets a value indicating whether <see cref="ModelMetadata.UnderlyingOrModelType"/> is for an
    /// <see cref="System.Enum"/> with an associated <see cref="System.FlagsAttribute"/>. See
    /// <see cref="ModelMetadata.IsFlagsEnum"/>.
    /// </summary>
    // This could be implemented in DefaultModelMetadata. But value is needed in the details provider.
    public bool IsFlagsEnum { get; set; }

    /// <summary>
    /// Gets or sets the text to display when the model value is <see langword="null"/>.
    /// See <see cref="ModelMetadata.NullDisplayText"/>
    /// </summary>
    /// <remarks>
    /// Setting <see cref="NullDisplayText"/> also changes <see cref="NullDisplayTextProvider"/>.
    /// </remarks>
    public string? NullDisplayText
    {
        get
        {
            return NullDisplayTextProvider();
        }
        set
        {
            NullDisplayTextProvider = () => value;
        }
    }

    /// <summary>
    /// Gets or sets a delegate which is used to get the text to display when the model is <see langword="null"/>.
    /// See <see cref="ModelMetadata.NullDisplayText"/>.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="NullDisplayTextProvider"/> also changes <see cref="NullDisplayText"/>.
    /// </remarks>
    public Func<string?> NullDisplayTextProvider
    {
        get
        {
            return _nullDisplayTextProvider;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _nullDisplayTextProvider = value;
        }
    }

    /// <summary>
    /// Gets or sets the order.
    /// See <see cref="ModelMetadata.Order"/>
    /// </summary>
    public int Order { get; set; } = 10000;

    /// <summary>
    /// Gets or sets a delegate which is used to get a value for the
    /// model's placeholder text. See <see cref="ModelMetadata.Placeholder"/>.
    /// </summary>
    public Func<string?>? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not to include in the model value in display.
    /// See <see cref="ModelMetadata.ShowForDisplay"/>
    /// </summary>
    public bool ShowForDisplay { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to include in the model value in an editor.
    /// See <see cref="ModelMetadata.ShowForEdit"/>
    /// </summary>
    public bool ShowForEdit { get; set; } = true;

    /// <summary>
    /// Gets or sets a the property name of a model property to use for display.
    /// See <see cref="ModelMetadata.SimpleDisplayProperty"/>
    /// </summary>
    public string? SimpleDisplayProperty { get; set; }

    /// <summary>
    /// Gets or sets a hint for location of a display or editor template.
    /// See <see cref="ModelMetadata.TemplateHint"/>
    /// </summary>
    public string? TemplateHint { get; set; }
}
