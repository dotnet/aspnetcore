// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal static class ValidationHelpers
{
    public static string GetModelErrorMessageOrDefault(ModelError modelError)
    {
        Debug.Assert(modelError != null);

        if (!string.IsNullOrEmpty(modelError.ErrorMessage))
        {
            return modelError.ErrorMessage;
        }

        // Default in the ValidationSummary case is no error message.
        return string.Empty;
    }

    public static string GetModelErrorMessageOrDefault(
        ModelError modelError,
        ModelStateEntry containingEntry,
        ModelExplorer modelExplorer)
    {
        Debug.Assert(modelError != null);
        Debug.Assert(containingEntry != null);
        Debug.Assert(modelExplorer != null);

        if (!string.IsNullOrEmpty(modelError.ErrorMessage))
        {
            return modelError.ErrorMessage;
        }

        // Default in the ValidationMessage case is a fallback error message.
        var attemptedValue = containingEntry.AttemptedValue ?? "null";
        return modelExplorer.Metadata.ModelBindingMessageProvider.ValueIsInvalidAccessor(attemptedValue);
    }

    // Returns non-null list of model states, which caller will render in order provided.
    public static IList<ModelStateEntry> GetModelStateList(
        ViewDataDictionary viewData,
        bool excludePropertyErrors)
    {
        if (excludePropertyErrors)
        {
            viewData.ModelState.TryGetValue(viewData.TemplateInfo.HtmlFieldPrefix, out var ms);

            if (ms != null)
            {
                return new[] { ms };
            }
        }
        else if (viewData.ModelState.Count > 0)
        {
            var metadata = viewData.ModelMetadata;
            var modelStateDictionary = viewData.ModelState;
            var entries = new List<ModelStateEntry>();
            Visit(modelStateDictionary.Root, metadata, entries);

            if (entries.Count < modelStateDictionary.Count)
            {
                // Account for entries in the ModelStateDictionary that do not have corresponding ModelMetadata values.
                foreach (var entry in modelStateDictionary)
                {
                    if (!entries.Contains(entry.Value))
                    {
                        entries.Add(entry.Value);
                    }
                }
            }

            return entries;
        }

        return Array.Empty<ModelStateEntry>();
    }

    private static void Visit(
        ModelStateEntry modelStateEntry,
        ModelMetadata metadata,
        List<ModelStateEntry> orderedModelStateEntries)
    {
        if (metadata.ElementMetadata != null && modelStateEntry.Children != null)
        {
            foreach (var indexEntry in modelStateEntry.Children)
            {
                Visit(indexEntry, metadata.ElementMetadata, orderedModelStateEntries);
            }
        }
        else
        {
            for (var i = 0; i < metadata.Properties.Count; i++)
            {
                var propertyMetadata = metadata.Properties[i];
                var propertyModelStateEntry = modelStateEntry.GetModelStateForProperty(propertyMetadata.PropertyName);
                if (propertyModelStateEntry != null)
                {
                    Visit(propertyModelStateEntry, propertyMetadata, orderedModelStateEntries);
                }
            }
        }

        if (!modelStateEntry.IsContainerNode)
        {
            orderedModelStateEntries.Add(modelStateEntry);
        }
    }
}
