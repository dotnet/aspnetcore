// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Extensions for <see cref="JsonPatchDocument{T}"/>
/// </summary>
public static class JsonPatchExtensions
{
    /// <summary>
    /// Applies JSON patch operations on object and logs errors in <see cref="ModelStateDictionary"/>.
    /// </summary>
    /// <param name="patchDoc">The <see cref="JsonPatchDocument{T}"/>.</param>
    /// <param name="objectToApplyTo">The entity on which <see cref="JsonPatchDocument{T}"/> is applied.</param>
    /// <param name="modelState">The <see cref="ModelStateDictionary"/> to add errors.</param>
    public static void ApplyTo<T>(
        this JsonPatchDocument<T> patchDoc,
        T objectToApplyTo,
        ModelStateDictionary modelState) where T : class
    {
        ArgumentNullException.ThrowIfNull(patchDoc);
        ArgumentNullException.ThrowIfNull(objectToApplyTo);
        ArgumentNullException.ThrowIfNull(modelState);

        patchDoc.ApplyTo(objectToApplyTo, modelState, prefix: string.Empty);
    }

    /// <summary>
    /// Applies JSON patch operations on object and logs errors in <see cref="ModelStateDictionary"/>.
    /// </summary>
    /// <param name="patchDoc">The <see cref="JsonPatchDocument{T}"/>.</param>
    /// <param name="objectToApplyTo">The entity on which <see cref="JsonPatchDocument{T}"/> is applied.</param>
    /// <param name="modelState">The <see cref="ModelStateDictionary"/> to add errors.</param>
    /// <param name="prefix">The prefix to use when looking up values in <see cref="ModelStateDictionary"/>.</param>
    public static void ApplyTo<T>(
        this JsonPatchDocument<T> patchDoc,
        T objectToApplyTo,
        ModelStateDictionary modelState,
        string prefix) where T : class
    {
        ArgumentNullException.ThrowIfNull(patchDoc);
        ArgumentNullException.ThrowIfNull(objectToApplyTo);
        ArgumentNullException.ThrowIfNull(modelState);

        patchDoc.ApplyTo(objectToApplyTo, jsonPatchError =>
        {
            var affectedObjectName = jsonPatchError.AffectedObject.GetType().Name;
            var key = string.IsNullOrEmpty(prefix) ? affectedObjectName : prefix + "." + affectedObjectName;

            modelState.TryAddModelError(key, jsonPatchError.ErrorMessage);
        });
    }
}
