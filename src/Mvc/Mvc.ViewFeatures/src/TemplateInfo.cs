// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Represents a template which keeps track of visited objects.
/// </summary>
public class TemplateInfo
{
    // Keep a collection of visited objects to prevent infinite recursion.
    private readonly HashSet<object> _visitedObjects;

    private object _formattedModelValue;
    private string _htmlFieldPrefix;

    /// <summary>
    /// Initializes a new instance of <see cref="TemplateInfo"/>.
    /// </summary>
    public TemplateInfo()
    {
        _htmlFieldPrefix = string.Empty;
        _formattedModelValue = string.Empty;
        _visitedObjects = new HashSet<object>();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TemplateInfo"/>.
    /// </summary>
    /// <param name="original">The original value to copy.</param>
    public TemplateInfo(TemplateInfo original)
    {
        FormattedModelValue = original.FormattedModelValue;
        HtmlFieldPrefix = original.HtmlFieldPrefix;
        _visitedObjects = new HashSet<object>(original._visitedObjects);
    }

    /// <summary>
    /// Gets or sets the formatted model value.
    /// </summary>
    /// <value>The formatted model value.</value>
    /// <remarks>
    /// Will never return <c>null</c> to avoid problems when using HTML helpers within a template.  Otherwise the
    /// helpers could find elements in the `ViewDataDictionary`, not the intended Model properties.
    /// </remarks>
    public object FormattedModelValue
    {
        get { return _formattedModelValue; }
        set { _formattedModelValue = value ?? string.Empty; }
    }

    /// <summary>
    /// Gets or sets the HTML field prefix.
    /// </summary>
    /// <value>The HTML field prefix.</value>
    /// <remarks>
    /// Will never return <c>null</c> for consistency with <see cref="FormattedModelValue"/>.
    /// </remarks>
    public string HtmlFieldPrefix
    {
        get { return _htmlFieldPrefix; }
        set { _htmlFieldPrefix = value ?? string.Empty; }
    }

    /// <summary>
    /// Gets how many objects have been visited.
    /// </summary>
    public int TemplateDepth
    {
        get { return _visitedObjects.Count; }
    }

    /// <summary>
    /// Mark a value as visited.
    /// </summary>
    /// <param name="value">The object to visit.</param>
    /// <returns>If this object is newly visited.</returns>
    public bool AddVisited(object value)
    {
        return _visitedObjects.Add(value);
    }

    /// <summary>
    /// Returns the full HTML element name for the specified <paramref name="partialFieldName"/>.
    /// </summary>
    /// <param name="partialFieldName">Expression name, relative to the current model.</param>
    /// <returns>Fully-qualified expression name for <paramref name="partialFieldName"/>.</returns>
    public string GetFullHtmlFieldName(string partialFieldName)
    {
        if (string.IsNullOrEmpty(partialFieldName))
        {
            return HtmlFieldPrefix;
        }

        if (string.IsNullOrEmpty(HtmlFieldPrefix))
        {
            return partialFieldName;
        }

        if (partialFieldName.StartsWith('['))
        {
            // The partialFieldName might represent an indexer access, in which case combining
            // with a 'dot' would be invalid.
            return HtmlFieldPrefix + partialFieldName;
        }

        return HtmlFieldPrefix + "." + partialFieldName;
    }

    /// <summary>
    /// Checks if a model has been visited already.
    /// </summary>
    /// <param name="modelExplorer">The <see cref="ModelExplorer"/>.</param>
    /// <returns>Whether the model has been visited.</returns>
    public bool Visited(ModelExplorer modelExplorer)
    {
        return _visitedObjects.Contains(modelExplorer.Model ?? modelExplorer.Metadata.ModelType);
    }
}
