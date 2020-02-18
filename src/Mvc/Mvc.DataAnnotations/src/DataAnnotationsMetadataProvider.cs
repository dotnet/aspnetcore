// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    /// <summary>
    /// An implementation of <see cref="IBindingMetadataProvider"/> and <see cref="IDisplayMetadataProvider"/> for
    /// the System.ComponentModel.DataAnnotations attribute classes.
    /// </summary>
    internal class DataAnnotationsMetadataProvider :
        IBindingMetadataProvider,
        IDisplayMetadataProvider,
        IValidationMetadataProvider
    {
        // The [Nullable] attribute is synthesized by the compiler. It's best to just compare the type name.
        private const string NullableAttributeFullTypeName = "System.Runtime.CompilerServices.NullableAttribute";
        private const string NullableFlagsFieldName = "NullableFlags";

        private const string NullableContextAttributeFullName = "System.Runtime.CompilerServices.NullableContextAttribute";
        private const string NullableContextFlagsFieldName = "Flag";

        private readonly IStringLocalizerFactory _stringLocalizerFactory;
        private readonly MvcOptions _options;
        private readonly MvcDataAnnotationsLocalizationOptions _localizationOptions;

        public DataAnnotationsMetadataProvider(
            MvcOptions options,
            IOptions<MvcDataAnnotationsLocalizationOptions> localizationOptions,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (localizationOptions == null)
            {
                throw new ArgumentNullException(nameof(localizationOptions));
            }

            _options = options;
            _localizationOptions = localizationOptions.Value;
            _stringLocalizerFactory = stringLocalizerFactory;
        }

        /// <inheritdoc />
        public void CreateBindingMetadata(BindingMetadataProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var editableAttribute = context.Attributes.OfType<EditableAttribute>().FirstOrDefault();
            if (editableAttribute != null)
            {
                context.BindingMetadata.IsReadOnly = !editableAttribute.AllowEdit;
            }
        }

        /// <inheritdoc />
        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var attributes = context.Attributes;
            var dataTypeAttribute = attributes.OfType<DataTypeAttribute>().FirstOrDefault();
            var displayAttribute = attributes.OfType<DisplayAttribute>().FirstOrDefault();
            var displayColumnAttribute = attributes.OfType<DisplayColumnAttribute>().FirstOrDefault();
            var displayFormatAttribute = attributes.OfType<DisplayFormatAttribute>().FirstOrDefault();
            var displayNameAttribute = attributes.OfType<DisplayNameAttribute>().FirstOrDefault();
            var hiddenInputAttribute = attributes.OfType<HiddenInputAttribute>().FirstOrDefault();
            var scaffoldColumnAttribute = attributes.OfType<ScaffoldColumnAttribute>().FirstOrDefault();
            var uiHintAttribute = attributes.OfType<UIHintAttribute>().FirstOrDefault();

            // Special case the [DisplayFormat] attribute hanging off an applied [DataType] attribute. This property is
            // non-null for DataType.Currency, DataType.Date, DataType.Time, and potentially custom [DataType]
            // subclasses. The DataType.Currency, DataType.Date, and DataType.Time [DisplayFormat] attributes have a
            // non-null DataFormatString and the DataType.Date and DataType.Time [DisplayFormat] attributes have
            // ApplyFormatInEditMode==true.
            if (displayFormatAttribute == null && dataTypeAttribute != null)
            {
                displayFormatAttribute = dataTypeAttribute.DisplayFormat;
            }

            var displayMetadata = context.DisplayMetadata;

            // ConvertEmptyStringToNull
            if (displayFormatAttribute != null)
            {
                displayMetadata.ConvertEmptyStringToNull = displayFormatAttribute.ConvertEmptyStringToNull;
            }

            // DataTypeName
            if (dataTypeAttribute != null)
            {
                displayMetadata.DataTypeName = dataTypeAttribute.GetDataTypeName();
            }
            else if (displayFormatAttribute != null && !displayFormatAttribute.HtmlEncode)
            {
                displayMetadata.DataTypeName = nameof(DataType.Html);
            }

            var containerType = context.Key.ContainerType ?? context.Key.ModelType;
            IStringLocalizer localizer = null;
            if (_stringLocalizerFactory != null && _localizationOptions.DataAnnotationLocalizerProvider != null)
            {
                localizer = _localizationOptions.DataAnnotationLocalizerProvider(containerType, _stringLocalizerFactory);
            }

            // Description
            if (displayAttribute != null)
            {
                if (localizer != null &&
                    !string.IsNullOrEmpty(displayAttribute.Description) &&
                    displayAttribute.ResourceType == null)
                {
                    displayMetadata.Description = () => localizer[displayAttribute.Description];
                }
                else
                {
                    displayMetadata.Description = () => displayAttribute.GetDescription();
                }
            }

            // DisplayFormatString
            if (displayFormatAttribute != null)
            {
                displayMetadata.DisplayFormatString = displayFormatAttribute.DataFormatString;
            }

            // DisplayName
            // DisplayAttribute has precedence over DisplayNameAttribute.
            if (displayAttribute?.GetName() != null)
            {
                if (localizer != null &&
                    !string.IsNullOrEmpty(displayAttribute.Name) &&
                    displayAttribute.ResourceType == null)
                {
                    displayMetadata.DisplayName = () => localizer[displayAttribute.Name];
                }
                else
                {
                    displayMetadata.DisplayName = () => displayAttribute.GetName();
                }
            }
            else if (displayNameAttribute != null)
            {
                if (localizer != null &&
                    !string.IsNullOrEmpty(displayNameAttribute.DisplayName))
                {
                    displayMetadata.DisplayName = () => localizer[displayNameAttribute.DisplayName];
                }
                else
                {
                    displayMetadata.DisplayName = () => displayNameAttribute.DisplayName;
                }
            }

            // EditFormatString
            if (displayFormatAttribute != null && displayFormatAttribute.ApplyFormatInEditMode)
            {
                displayMetadata.EditFormatString = displayFormatAttribute.DataFormatString;
            }

            // IsEnum et cetera
            var underlyingType = Nullable.GetUnderlyingType(context.Key.ModelType) ?? context.Key.ModelType;
            var underlyingTypeInfo = underlyingType.GetTypeInfo();

            if (underlyingTypeInfo.IsEnum)
            {
                // IsEnum
                displayMetadata.IsEnum = true;

                // IsFlagsEnum
                displayMetadata.IsFlagsEnum = underlyingTypeInfo.IsDefined(typeof(FlagsAttribute), inherit: false);

                // EnumDisplayNamesAndValues and EnumNamesAndValues
                //
                // Order EnumDisplayNamesAndValues by DisplayAttribute.Order, then by the order of Enum.GetNames().
                // That method orders by absolute value, then its behavior is undefined (but hopefully stable).
                // Add to EnumNamesAndValues in same order but Dictionary does not guarantee order will be preserved.

                var groupedDisplayNamesAndValues = new List<KeyValuePair<EnumGroupAndName, string>>();
                var namesAndValues = new Dictionary<string, string>();

                IStringLocalizer enumLocalizer = null;
                if (_stringLocalizerFactory != null && _localizationOptions.DataAnnotationLocalizerProvider != null)
                {
                    enumLocalizer = _localizationOptions.DataAnnotationLocalizerProvider(underlyingType, _stringLocalizerFactory);
                }

                var enumFields = Enum.GetNames(underlyingType)
                    .Select(name => underlyingType.GetField(name))
                    .OrderBy(field => field.GetCustomAttribute<DisplayAttribute>(inherit: false)?.GetOrder() ?? 1000);

                foreach (var field in enumFields)
                {
                    var groupName = GetDisplayGroup(field);
                    var value = ((Enum)field.GetValue(obj: null)).ToString("d");

                    groupedDisplayNamesAndValues.Add(new KeyValuePair<EnumGroupAndName, string>(
                        new EnumGroupAndName(
                            groupName,
                            () => GetDisplayName(field, enumLocalizer)),
                        value));
                    namesAndValues.Add(field.Name, value);
                }

                displayMetadata.EnumGroupedDisplayNamesAndValues = groupedDisplayNamesAndValues;
                displayMetadata.EnumNamesAndValues = namesAndValues;
            }

            // HasNonDefaultEditFormat
            if (!string.IsNullOrEmpty(displayFormatAttribute?.DataFormatString) &&
                displayFormatAttribute?.ApplyFormatInEditMode == true)
            {
                // Have a non-empty EditFormatString based on [DisplayFormat] from our cache.
                if (dataTypeAttribute == null)
                {
                    // Attributes include no [DataType]; [DisplayFormat] was applied directly.
                    displayMetadata.HasNonDefaultEditFormat = true;
                }
                else if (dataTypeAttribute.DisplayFormat != displayFormatAttribute)
                {
                    // Attributes include separate [DataType] and [DisplayFormat]; [DisplayFormat] provided override.
                    displayMetadata.HasNonDefaultEditFormat = true;
                }
                else if (dataTypeAttribute.GetType() != typeof(DataTypeAttribute))
                {
                    // Attributes include [DisplayFormat] copied from [DataType] and [DataType] was of a subclass.
                    // Assume the [DataType] constructor used the protected DisplayFormat setter to override its
                    // default.  That is derived [DataType] provided override.
                    displayMetadata.HasNonDefaultEditFormat = true;
                }
            }

            // HideSurroundingHtml
            if (hiddenInputAttribute != null)
            {
                displayMetadata.HideSurroundingHtml = !hiddenInputAttribute.DisplayValue;
            }

            // HtmlEncode
            if (displayFormatAttribute != null)
            {
                displayMetadata.HtmlEncode = displayFormatAttribute.HtmlEncode;
            }

            // NullDisplayText
            if (displayFormatAttribute != null)
            {
                displayMetadata.NullDisplayText = displayFormatAttribute.NullDisplayText;
            }

            // Order
            if (displayAttribute?.GetOrder() != null)
            {
                displayMetadata.Order = displayAttribute.GetOrder().Value;
            }

            // Placeholder
            if (displayAttribute != null)
            {
                if (localizer != null &&
                    !string.IsNullOrEmpty(displayAttribute.Prompt) &&
                    displayAttribute.ResourceType == null)
                {
                    displayMetadata.Placeholder = () => localizer[displayAttribute.Prompt];
                }
                else
                {
                    displayMetadata.Placeholder = () => displayAttribute.GetPrompt();
                }
            }

            // ShowForDisplay
            if (scaffoldColumnAttribute != null)
            {
                displayMetadata.ShowForDisplay = scaffoldColumnAttribute.Scaffold;
            }

            // ShowForEdit
            if (scaffoldColumnAttribute != null)
            {
                displayMetadata.ShowForEdit = scaffoldColumnAttribute.Scaffold;
            }

            // SimpleDisplayProperty
            if (displayColumnAttribute != null)
            {
                displayMetadata.SimpleDisplayProperty = displayColumnAttribute.DisplayColumn;
            }

            // TemplateHint
            if (uiHintAttribute != null)
            {
                displayMetadata.TemplateHint = uiHintAttribute.UIHint;
            }
            else if (hiddenInputAttribute != null)
            {
                displayMetadata.TemplateHint = "HiddenInput";
            }
        }

        /// <inheritdoc />
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Read interface .Count once rather than per iteration
            var contextAttributes = context.Attributes;
            var contextAttributesCount = contextAttributes.Count;
            var attributes = new List<object>(contextAttributesCount);

            for (var i = 0; i < contextAttributesCount; i++)
            {
                var attribute = contextAttributes[i];
                if (attribute is ValidationProviderAttribute validationProviderAttribute)
                {
                    attributes.AddRange(validationProviderAttribute.GetValidationAttributes());
                }
                else
                {
                    attributes.Add(attribute);
                }
            }

            // RequiredAttribute marks a property as required by validation - this means that it
            // must have a non-null value on the model during validation.
            var requiredAttribute = attributes.OfType<RequiredAttribute>().FirstOrDefault();

            // For non-nullable reference types, treat them as-if they had an implicit [Required].
            // This allows the developer to specify [Required] to customize the error message, so
            // if they already have [Required] then there's no need for us to do this check.
            if (!_options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes &&
                requiredAttribute == null &&
                !context.Key.ModelType.IsValueType &&
                context.Key.MetadataKind != ModelMetadataKind.Type)
            {
                var addInferredRequiredAttribute = false;
                if (context.Key.MetadataKind == ModelMetadataKind.Type)
                {
                    // Do nothing.
                }
                else if (context.Key.MetadataKind == ModelMetadataKind.Property)
                {
                    var property = context.Key.PropertyInfo;
                    if (property is null)
                    {
                        // PropertyInfo was unavailable on ModelIdentity prior to 3.1.
                        // Making a cogent argument about the nullability of the property requires inspecting the declared type,
                        // since looking at the runtime type may result in false positives: https://github.com/dotnet/aspnetcore/issues/14812
                        // The only way we could arrive here is if the ModelMetadata was constructed using the non-default provider.
                        // We'll cursorily examine the attributes on the property, but not the ContainerType to make a decision about it's nullability.

                        if (HasNullableAttribute(context.PropertyAttributes, out var propertyHasNullableAttribute))
                        {
                            addInferredRequiredAttribute = propertyHasNullableAttribute;
                        }
                    }
                    else
                    {
                        addInferredRequiredAttribute = IsNullableReferenceType(
                            property.DeclaringType,
                            member: null,
                            context.PropertyAttributes);
                    }
                }
                else if (context.Key.MetadataKind == ModelMetadataKind.Parameter)
                {
                    addInferredRequiredAttribute = IsNullableReferenceType(
                        context.Key.ParameterInfo?.Member.ReflectedType,
                        context.Key.ParameterInfo.Member,
                        context.ParameterAttributes);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported ModelMetadataKind: " + context.Key.MetadataKind);
                }

                if (addInferredRequiredAttribute)
                {
                    // Since this behavior specifically relates to non-null-ness, we will use the non-default
                    // option to tolerate empty/whitespace strings. empty/whitespace INPUT will still result in
                    // a validation error by default because we convert empty/whitespace strings to null
                    // unless you say otherwise.
                    requiredAttribute = new RequiredAttribute()
                    {
                        AllowEmptyStrings = true,
                    };
                    attributes.Add(requiredAttribute);
                }
            }

            if (requiredAttribute != null)
            {
                context.ValidationMetadata.IsRequired = true;
            }

            foreach (var attribute in attributes.OfType<ValidationAttribute>())
            {
                // If another provider has already added this attribute, do not repeat it.
                // This will prevent attributes like RemoteAttribute (which implement ValidationAttribute and
                // IClientModelValidator) to be added to the ValidationMetadata twice.
                // This is to ensure we do not end up with duplication validation rules on the client side.
                if (!context.ValidationMetadata.ValidatorMetadata.Contains(attribute))
                {
                    context.ValidationMetadata.ValidatorMetadata.Add(attribute);
                }
            }
        }

        private static string GetDisplayName(FieldInfo field, IStringLocalizer stringLocalizer)
        {
            var display = field.GetCustomAttribute<DisplayAttribute>(inherit: false);
            if (display != null)
            {
                // Note [Display(Name = "")] is allowed but we will not attempt to localize the empty name.
                var name = display.GetName();
                if (stringLocalizer != null && !string.IsNullOrEmpty(name) && display.ResourceType == null)
                {
                    name = stringLocalizer[name];
                }

                return name ?? field.Name;
            }

            return field.Name;
        }

        // Return non-empty group specified in a [Display] attribute for a field, if any; string.Empty otherwise.
        private static string GetDisplayGroup(FieldInfo field)
        {
            var display = field.GetCustomAttribute<DisplayAttribute>(inherit: false);
            if (display != null)
            {
                // Note [Display(Group = "")] is allowed.
                var group = display.GetGroupName();
                if (group != null)
                {
                    return group;
                }
            }

            return string.Empty;
        }

        internal static bool IsNullableReferenceType(Type containingType, MemberInfo member, IEnumerable<object> attributes)
        {
            if (HasNullableAttribute(attributes, out var result))
            {
                return result;
            }

            return IsNullableBasedOnContext(containingType, member);
        }

        // Internal for testing
        internal static bool HasNullableAttribute(IEnumerable<object> attributes, out bool isNullable)
        {
            // [Nullable] is compiler synthesized, comparing by name.
            var nullableAttribute = attributes
                .FirstOrDefault(a => string.Equals(a.GetType().FullName, NullableAttributeFullTypeName, StringComparison.Ordinal));
            if (nullableAttribute == null)
            {
                isNullable = false;
                return false; // [Nullable] not found
            }

            // We don't handle cases where generics and NNRT are used. This runs into a
            // fundamental limitation of ModelMetadata - we use a single Type and Property/Parameter
            // to look up the metadata. However when generics are involved and NNRT is in use
            // the distance between the [Nullable] and member we're looking at is potentially
            // unbounded.
            //
            // See: https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-reference-types.md#annotations
            if (nullableAttribute.GetType().GetField(NullableFlagsFieldName) is FieldInfo field &&
                field.GetValue(nullableAttribute) is byte[] flags &&
                flags.Length > 0 &&
                flags[0] == 1) // First element is the property/parameter type.
            {
                isNullable = true;
                return true; // [Nullable] found and type is an NNRT
            }

            isNullable = false;
            return true; // [Nullable] found but type is not an NNRT
        }

        internal static bool IsNullableBasedOnContext(Type containingType, MemberInfo member)
        {
            // For generic types, inspecting the nullability requirement additionally requires
            // inspecting the nullability constraint on generic type parameters. This is fairly non-triviial
            // so we'll just avoid calculating it. Users should still be able to apply an explicit [Required]
            // attribute on these members.
            if (containingType.IsGenericType)
            {
                return false;
            }

            // The [Nullable] and [NullableContext] attributes are not inherited.
            //
            // The [NullableContext] attribute can appear on a method or on the module.
            var attributes = member?.GetCustomAttributes(inherit: false) ?? Array.Empty<object>();
            var isNullable = AttributesHasNullableContext(attributes);
            if (isNullable != null)
            {
                return isNullable.Value;
            }

            // Check on the containing type
            var type = containingType;
            do
            {
                attributes = type.GetCustomAttributes(inherit: false);
                isNullable = AttributesHasNullableContext(attributes);
                if (isNullable != null)
                {
                    return isNullable.Value;
                }

                type = type.DeclaringType;
            }
            while (type != null);

            // If we don't find the attribute on the declaring type then repeat at the module level
            attributes = containingType.Module.GetCustomAttributes(inherit: false);
            isNullable = AttributesHasNullableContext(attributes);
            return isNullable ?? false;

            bool? AttributesHasNullableContext(object[] attributes)
            {
                var nullableContextAttribute = attributes
                    .FirstOrDefault(a => string.Equals(a.GetType().FullName, NullableContextAttributeFullName, StringComparison.Ordinal));
                if (nullableContextAttribute != null)
                {
                    if (nullableContextAttribute.GetType().GetField(NullableContextFlagsFieldName) is FieldInfo field &&
                        field.GetValue(nullableContextAttribute) is byte @byte)
                    {
                        return @byte == 1; // [NullableContext] found
                    }
                }

                return null;
            }
        }
    }
}
