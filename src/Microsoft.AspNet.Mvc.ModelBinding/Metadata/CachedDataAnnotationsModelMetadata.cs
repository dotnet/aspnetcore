// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    // Class does not override ComputeIsComplexType() because value calculated in ModelMetadata's base implementation
    // is correct.
    public class CachedDataAnnotationsModelMetadata : CachedModelMetadata<CachedDataAnnotationsMetadataAttributes>
    {
        private static readonly string HtmlName = DataType.Html.ToString();
        private bool _isEditFormatStringFromCache;

        public CachedDataAnnotationsModelMetadata(CachedDataAnnotationsModelMetadata prototype, 
                                                  Func<object> modelAccessor)
            : base(prototype, modelAccessor)
        {
        }

        public CachedDataAnnotationsModelMetadata(DataAnnotationsModelMetadataProvider provider, 
                                                  Type containerType, 
                                                  Type modelType, 
                                                  string propertyName, 
                                                  IEnumerable<Attribute> attributes)
            : base(provider, 
                   containerType, 
                   modelType, 
                   propertyName, 
                   new CachedDataAnnotationsMetadataAttributes(attributes))
        {
            BinderMetadata = attributes.OfType<IBinderMetadata>().FirstOrDefault();

            var modelNameProvider = attributes.OfType<IModelNameProvider>().FirstOrDefault();
            ModelName = modelNameProvider?.Name;

            var bindAttribute = attributes.OfType<BindAttribute>().FirstOrDefault();
            ReadSettingsFromBindAttribute(bindAttribute);
        }

        protected override bool ComputeConvertEmptyStringToNull()
        {
            return PrototypeCache.DisplayFormat != null
                       ? PrototypeCache.DisplayFormat.ConvertEmptyStringToNull
                       : base.ComputeConvertEmptyStringToNull();
        }

        protected override string ComputeNullDisplayText()
        {
            return PrototypeCache.DisplayFormat != null
                       ? PrototypeCache.DisplayFormat.NullDisplayText
                       : base.ComputeNullDisplayText();
        }

        /// <summary>
        /// Calculate <see cref="ModelMetadata.DataTypeName"/> based on presence of a <see cref="DataTypeAttribute"/>
        /// and its <see cref="DataTypeAttribute.GetDataTypeName()"/> method.
        /// </summary>
        /// <returns>
        /// Calculated <see cref="ModelMetadata.DataTypeName"/> value.
        /// <see cref="DataTypeAttribute.GetDataTypeName()"/> value if a <see cref="DataTypeAttribute"/> exists.
        /// <c>"Html"</c> if a <see cref="DisplayFormatAttribute"/> exists with its
        /// <see cref="DisplayFormatAttribute.HtmlEncode"/> value <c>false</c>. <c>null</c> otherwise.
        /// </returns>
        protected override string ComputeDataTypeName()
        {
            if (PrototypeCache.DataType != null)
            {
                return PrototypeCache.DataType.GetDataTypeName();
            }

            if (PrototypeCache.DisplayFormat != null && !PrototypeCache.DisplayFormat.HtmlEncode)
            {
                return HtmlName;
            }

            return base.ComputeDataTypeName();
        }

        protected override string ComputeDescription()
        {
            return PrototypeCache.Display != null
                       ? PrototypeCache.Display.GetDescription()
                       : base.ComputeDescription();
        }

        /// <summary>
        /// Calculate <see cref="ModelMetadata.DisplayFormatString"/> based on presence of a
        /// <see cref="DisplayFormatAttribute"/> and its <see cref="DisplayFormatAttribute.DataFormatString"/> value.
        /// </summary>
        /// <returns>
        /// Calculated <see cref="ModelMetadata.DisplayFormatString"/> value.
        /// <see cref="DisplayFormatAttribute.DataFormatString"/> if a <see cref="DisplayFormatAttribute"/> exists.
        /// <c>null</c> otherwise.
        /// </returns>
        protected override string ComputeDisplayFormatString()
        {
            return PrototypeCache.DisplayFormat != null
                ? PrototypeCache.DisplayFormat.DataFormatString
                : base.ComputeDisplayFormatString();
        }

        protected override string ComputeDisplayName()
        {
            // DisplayName may be provided by DisplayAttribute.
            // If that does not supply a name, then we fall back to the property name (in base.GetDisplayName()).
            if (PrototypeCache.Display != null)
            {
                // DisplayAttribute doesn't require you to set a name, so this could be null.
                var name = PrototypeCache.Display.GetName();
                if (name != null)
                {
                    return name;
                }
            }

            return base.ComputeDisplayName();
        }

        /// <summary>
        /// Calculate <see cref="ModelMetadata.EditFormatString"/> based on presence of a
        /// <see cref="DisplayFormatAttribute"/> and its <see cref="DisplayFormatAttribute.ApplyFormatInEditMode"/> and
        /// <see cref="DisplayFormatAttribute.DataFormatString"/> values.
        /// </summary>
        /// <returns>
        /// Calculated <see cref="ModelMetadata.DisplayFormatString"/> value.
        /// <see cref="DisplayFormatAttribute.DataFormatString"/> if a <see cref="DisplayFormatAttribute"/> exists and
        /// its <see cref="DisplayFormatAttribute.ApplyFormatInEditMode"/> is <c>true</c>; <c>null</c> otherwise.
        /// </returns>
        /// <remarks>
        /// Subclasses overriding this method should also override <see cref="ComputeHasNonDefaultEditFormat"/> to
        /// ensure the two calculations remain consistent.
        /// </remarks>
        protected override string ComputeEditFormatString()
        {
            if (PrototypeCache.DisplayFormat != null && PrototypeCache.DisplayFormat.ApplyFormatInEditMode)
            {
                _isEditFormatStringFromCache = true;
                return PrototypeCache.DisplayFormat.DataFormatString;
            }

            return base.ComputeEditFormatString();
        }

        /// <summary>
        /// Calculate <see cref="ModelMetadata.HasNonDefaultEditFormat"/> based on
        /// <see cref="ModelMetadata.EditFormatString"/> and presence of <see cref="DataTypeAttribute"/> and
        /// <see cref="DisplayFormatAttribute"/>.
        /// </summary>
        /// <returns>
        /// Calculated <see cref="ModelMetadata.HasNonDefaultEditFormat"/> value. <c>true</c> if
        /// <see cref="ModelMetadata.EditFormatString"/> is non-<c>null</c>, non-empty, and came from the cache (was
        /// not set directly).  In addition the applied <see cref="DisplayFormatAttribute"/> must not have come from an
        /// applied <see cref="DataTypeAttribute"/>. <c>false</c> otherwise.
        /// </returns>
        protected override bool ComputeHasNonDefaultEditFormat()
        {
            // Following calculation ignores possibility something (an IModelMetadataProvider) set EditFormatString
            // directly.
            if (!string.IsNullOrEmpty(EditFormatString) && _isEditFormatStringFromCache)
            {
                // Have a non-empty EditFormatString based on [DisplayFormat] from our cache.
                if (PrototypeCache.DataType == null)
                {
                    // Attributes include no [DataType]; [DisplayFormat] was applied directly.
                    return true;
                }

                if (PrototypeCache.DataType.DisplayFormat != PrototypeCache.DisplayFormat)
                {
                    // Attributes include separate [DataType] and [DisplayFormat]; [DisplayFormat] provided override.
                    return true;
                }

                if (PrototypeCache.DataType.GetType() != typeof(DataTypeAttribute))
                {
                    // Attributes include [DisplayFormat] copied from [DataType] and [DataType] was of a subclass.
                    // Assume the [DataType] constructor used the protected DisplayFormat setter to override its
                    // default.  That is derived [DataType] provided override.
                    return true;
                }
            }

            return base.ComputeHasNonDefaultEditFormat();
        }

        /// <summary>
        /// Calculate <see cref="ModelMetadata.HideSurroundingHtml"/> based on presence of an
        /// <see cref="HiddenInputAttribute"/> and its <see cref="HiddenInputAttribute.DisplayValue"/> value.
        /// </summary>
        /// <returns>Calculated <see cref="ModelMetadata.HideSurroundingHtml"/> value. <c>true</c> if an
        /// <see cref="HiddenInputAttribute"/> exists and its <see cref="HiddenInputAttribute.DisplayValue"/> value is
        /// <c>false</c>; <c>false</c> otherwise.</returns>
        protected override bool ComputeHideSurroundingHtml()
        {
            if (PrototypeCache.HiddenInput != null)
            {
                return !PrototypeCache.HiddenInput.DisplayValue;
            }

            return base.ComputeHideSurroundingHtml();
        }

        protected override bool ComputeIsReadOnly()
        {
            if (PrototypeCache.Editable != null)
            {
                return !PrototypeCache.Editable.AllowEdit;
            }

            return base.ComputeIsReadOnly();
        }

        protected override bool ComputeIsRequired()
        {
            return (PrototypeCache.Required != null) || base.ComputeIsRequired();
        }

        protected override string ComputeSimpleDisplayText()
        {
            if (Model != null &&
                PrototypeCache.DisplayColumn != null &&
                !string.IsNullOrEmpty(PrototypeCache.DisplayColumn.DisplayColumn))
            {
                var displayColumnProperty = ModelType.GetTypeInfo().GetDeclaredProperty(
                                                    PrototypeCache.DisplayColumn.DisplayColumn);
                ValidateDisplayColumnAttribute(PrototypeCache.DisplayColumn, displayColumnProperty, ModelType);

                var simpleDisplayTextValue = displayColumnProperty.GetValue(Model, null);
                if (simpleDisplayTextValue != null)
                {
                    return simpleDisplayTextValue.ToString();
                }
            }

            return base.ComputeSimpleDisplayText();
        }

        protected override bool ComputeShowForDisplay()
        {
            return PrototypeCache.ScaffoldColumn != null
                       ? PrototypeCache.ScaffoldColumn.Scaffold
                       : base.ComputeShowForDisplay();
        }

        protected override bool ComputeShowForEdit()
        {
            return PrototypeCache.ScaffoldColumn != null
                       ? PrototypeCache.ScaffoldColumn.Scaffold
                       : base.ComputeShowForEdit();
        }

        private static void ValidateDisplayColumnAttribute(DisplayColumnAttribute displayColumnAttribute,
            PropertyInfo displayColumnProperty, Type modelType)
        {
            if (displayColumnProperty == null)
            {
                throw new InvalidOperationException(
                        Resources.FormatDataAnnotationsModelMetadataProvider_UnknownProperty(
                        modelType.FullName, displayColumnAttribute.DisplayColumn));
            }

            if (displayColumnProperty.GetGetMethod() == null)
            {
                throw new InvalidOperationException(
                        Resources.FormatDataAnnotationsModelMetadataProvider_UnreadableProperty(
                        modelType.FullName, displayColumnAttribute.DisplayColumn));
            }
        }

        private void ReadSettingsFromBindAttribute(BindAttribute bindAttribute)
        {
            if (bindAttribute == null)
            {
                return;
            }

            ExcludedProperties = SplitString(bindAttribute.Exclude).ToList();
            IncludedProperties = SplitString(bindAttribute.Include).ToList();
        }

        private static IEnumerable<string> SplitString(string original)
        {
            if (string.IsNullOrEmpty(original))
            {
                return new string[0];
            }

            var split = original.Split(',')
                                .Select(piece => piece.Trim())
                                .Where(trimmed => !string.IsNullOrEmpty(trimmed));
            return split;
        }
    }
}
