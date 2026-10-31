// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

internal abstract class SaveTempDataPropertyFilterBase : ISaveTempDataCallback
{
    protected readonly ITempDataDictionaryFactory _tempDataFactory;

    public SaveTempDataPropertyFilterBase(ITempDataDictionaryFactory tempDataFactory)
    {
        _tempDataFactory = tempDataFactory;
    }

    /// <summary>
    /// Describes the temp data properties which exist on <see cref="Subject"/>
    /// </summary>
    public IReadOnlyList<LifecycleProperty> Properties { get; set; }

    /// <summary>
    /// The <see cref="object"/> which has the temp data properties.
    /// </summary>
    public object Subject { get; set; }

    /// <summary>
    /// Tracks the values which originally existed in temp data.
    /// </summary>
    public IDictionary<PropertyInfo, object> OriginalValues { get; } = new Dictionary<PropertyInfo, object>();

    /// <summary>
    /// Puts the modified values of <see cref="Subject"/> into <paramref name="tempData"/>.
    /// </summary>
    /// <param name="tempData">The <see cref="ITempDataDictionary"/> to be updated.</param>
    public void OnTempDataSaving(ITempDataDictionary tempData)
    {
        if (Subject == null)
        {
            return;
        }

        for (var i = 0; i < Properties.Count; i++)
        {
            var property = Properties[i];
            OriginalValues.TryGetValue(property.PropertyInfo, out var originalValue);

            var newValue = property.GetValue(Subject);
            if (newValue != null && !newValue.Equals(originalValue))
            {
                tempData[property.Key] = newValue;
            }
        }
    }

    /// <summary>
    /// Sets the values of the properties of <see cref="Subject"/> from <paramref name="tempData"/>.
    /// </summary>
    /// <param name="tempData">The <see cref="ITempDataDictionary"/>.</param>
    protected void SetPropertyValues(ITempDataDictionary tempData)
    {
        if (Properties == null)
        {
            return;
        }

        Debug.Assert(Subject != null, "Subject must be set before this method is invoked.");

        for (var i = 0; i < Properties.Count; i++)
        {
            var property = Properties[i];
            var value = tempData[property.Key];

            OriginalValues[property.PropertyInfo] = value;
            property.SetValue(Subject, value);
        }
    }

    public static IReadOnlyList<LifecycleProperty> GetTempDataProperties(
        TempDataSerializer tempDataSerializer,
        Type type)
    {
        List<LifecycleProperty> results = null;
        var errorMessages = new List<string>();

        var propertyHelpers = PropertyHelper.GetVisibleProperties(type: type);
        for (var i = 0; i < propertyHelpers.Length; i++)
        {
            var propertyHelper = propertyHelpers[i];
            var property = propertyHelper.Property;
            var tempDataAttribute = property.GetCustomAttribute<TempDataAttribute>();
            if (tempDataAttribute != null && ValidateProperty(tempDataSerializer, errorMessages, propertyHelper.Property))
            {
                if (results == null)
                {
                    results = new List<LifecycleProperty>();
                }

                var key = tempDataAttribute.Key;
                if (string.IsNullOrEmpty(key))
                {
                    key = property.Name;
                }

                results.Add(new LifecycleProperty(property, key));
            }
        }

        if (errorMessages.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errorMessages));
        }

        return results;
    }

    private static bool ValidateProperty(TempDataSerializer tempDataSerializer, List<string> errorMessages, PropertyInfo property)
    {
        if (!(property.SetMethod != null &&
            property.SetMethod.IsPublic &&
            property.GetMethod != null &&
            property.GetMethod.IsPublic))
        {
            errorMessages.Add(
                Resources.FormatTempDataProperties_PublicGetterSetter(property.DeclaringType.FullName, property.Name, nameof(TempDataAttribute)));

            return false;
        }

        if (!tempDataSerializer.CanSerializeType(property.PropertyType))
        {
            var errorMessage = Resources.FormatTempDataProperties_InvalidType(
                tempDataSerializer.GetType().FullName,
                TypeNameHelper.GetTypeDisplayName(property.DeclaringType),
                property.Name,
                TypeNameHelper.GetTypeDisplayName(property.PropertyType));

            errorMessages.Add(errorMessage);

            return false;
        }

        return true;
    }
}
