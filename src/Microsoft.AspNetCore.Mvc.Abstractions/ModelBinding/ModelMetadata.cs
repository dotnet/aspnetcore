// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A metadata representation of a model type, property or parameter.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public abstract class ModelMetadata : IEquatable<ModelMetadata>, IModelMetadataProvider
    {
        /// <summary>
        /// The default value of <see cref="ModelMetadata.Order"/>.
        /// </summary>
        public static readonly int DefaultOrder = 10000;

        private int? _hashCode;

        /// <summary>
        /// Creates a new <see cref="ModelMetadata"/>.
        /// </summary>
        /// <param name="identity">The <see cref="ModelMetadataIdentity"/>.</param>
        protected ModelMetadata(ModelMetadataIdentity identity)
        {
            Identity = identity;

            InitializeTypeInformation();
        }

        /// <summary>
        /// Gets the container type of this metadata if it represents a property, otherwise <c>null</c>.
        /// </summary>
        public Type ContainerType => Identity.ContainerType;

        /// <summary>
        /// Gets the metadata of the container type that the current instance is part of.
        /// </summary>
        public virtual ModelMetadata ContainerMetadata
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets a value indicating the kind of metadata element represented by the current instance.
        /// </summary>
        public ModelMetadataKind MetadataKind => Identity.MetadataKind;

        /// <summary>
        /// Gets the model type represented by the current instance.
        /// </summary>
        public Type ModelType => Identity.ModelType;

        /// <summary>
        /// Gets the property name represented by the current instance.
        /// </summary>
        public string PropertyName => Identity.Name;

        /// <summary>
        /// Gets the key for the current instance.
        /// </summary>
        protected ModelMetadataIdentity Identity { get; }

        /// <summary>
        /// Gets a collection of additional information about the model.
        /// </summary>
        public abstract IReadOnlyDictionary<object, object> AdditionalValues { get; }

        /// <summary>
        /// Gets the collection of <see cref="ModelMetadata"/> instances for the model's properties.
        /// </summary>
        public abstract ModelPropertyCollection Properties { get; }

        /// <summary>
        /// Gets the name of a model if specified explicitly using <see cref="IModelNameProvider"/>.
        /// </summary>
        public abstract string BinderModelName { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> of an <see cref="IModelBinder"/> of a model if specified explicitly using
        /// <see cref="IBinderTypeProviderMetadata"/>.
        /// </summary>
        public abstract Type BinderType { get; }

        /// <summary>
        /// Gets a binder metadata for this model.
        /// </summary>
        public abstract BindingSource BindingSource { get; }

        /// <summary>
        /// Gets a value indicating whether or not to convert an empty string value or one containing only whitespace
        /// characters to <c>null</c> when representing a model as text.
        /// </summary>
        public abstract bool ConvertEmptyStringToNull { get; }

        /// <summary>
        /// Gets the name of the model's datatype.  Overrides <see cref="ModelType"/> in some
        /// display scenarios.
        /// </summary>
        /// <value><c>null</c> unless set manually or through additional metadata e.g. attributes.</value>
        public abstract string DataTypeName { get; }

        /// <summary>
        /// Gets the description of the model.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the format string (see https://msdn.microsoft.com/en-us/library/txafckwd.aspx) used to display the
        /// model.
        /// </summary>
        public abstract string DisplayFormatString { get; }

        /// <summary>
        /// Gets the display name of the model.
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Gets the format string (see https://msdn.microsoft.com/en-us/library/txafckwd.aspx) used to edit the model.
        /// </summary>
        public abstract string EditFormatString { get; }

        /// <summary>
        /// Gets the <see cref="ModelMetadata"/> for elements of <see cref="ModelType"/> if that <see cref="Type"/>
        /// implements <see cref="IEnumerable"/>.
        /// </summary>
        /// <value>
        /// <see cref="ModelMetadata"/> for <c>T</c> if <see cref="ModelType"/> implements
        /// <see cref="IEnumerable{T}"/>. <see cref="ModelMetadata"/> for <c>object</c> if <see cref="ModelType"/>
        /// implements <see cref="IEnumerable"/> but not <see cref="IEnumerable{T}"/>. <c>null</c> otherwise i.e. when
        /// <see cref="IsEnumerableType"/> is <c>false</c>.
        /// </value>
        public abstract ModelMetadata ElementMetadata { get; }

        /// <summary>
        /// Gets the ordered and grouped display names and values of all <see cref="Enum"/> values in
        /// <see cref="UnderlyingOrModelType"/>.
        /// </summary>
        /// <value>
        /// An <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{EnumGroupAndName, String}"/> of mappings between
        /// <see cref="Enum"/> field groups, names and values. <c>null</c> if <see cref="IsEnum"/> is <c>false</c>.
        /// </value>
        public abstract IEnumerable<KeyValuePair<EnumGroupAndName, string>> EnumGroupedDisplayNamesAndValues { get; }

        /// <summary>
        /// Gets the names and values of all <see cref="Enum"/> values in <see cref="UnderlyingOrModelType"/>.
        /// </summary>
        /// <value>
        /// An <see cref="IReadOnlyDictionary{String, String}"/> of mappings between <see cref="Enum"/> field names
        /// and values. <c>null</c> if <see cref="IsEnum"/> is <c>false</c>.
        /// </value>
        public abstract IReadOnlyDictionary<string, string> EnumNamesAndValues { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="EditFormatString"/> has a non-<c>null</c>, non-empty
        /// value different from the default for the datatype.
        /// </summary>
        public abstract bool HasNonDefaultEditFormat { get; }

        /// <summary>
        /// Gets a value indicating whether the value should be HTML-encoded.
        /// </summary>
        /// <value>If <c>true</c>, value should be HTML-encoded. Default is <c>true</c>.</value>
        public abstract bool HtmlEncode { get; }

        /// <summary>
        /// Gets a value indicating whether the "HiddenInput" display template should return
        /// <c>string.Empty</c> (not the expression value) and whether the "HiddenInput" editor template should not
        /// also return the expression value (together with the hidden &lt;input&gt; element).
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, also causes the default <see cref="object"/> display and editor templates to return HTML
        /// lacking the usual per-property &lt;div&gt; wrapper around the associated property. Thus the default
        /// <see cref="object"/> display template effectively skips the property and the default <see cref="object"/>
        /// editor template returns only the hidden &lt;input&gt; element for the property.
        /// </remarks>
        public abstract bool HideSurroundingHtml { get; }

        /// <summary>
        /// Gets a value indicating whether or not the model value can be bound by model binding. This is only
        /// applicable when the current instance represents a property.
        /// </summary>
        /// <remarks>
        /// If <c>true</c> then the model value is considered supported by model binding and can be set
        /// based on provided input in the request.
        /// </remarks>
        public abstract bool IsBindingAllowed { get; }

        /// <summary>
        /// Gets a value indicating whether or not the model value is required by model binding. This is only
        /// applicable when the current instance represents a property.
        /// </summary>
        /// <remarks>
        /// If <c>true</c> then the model value is considered required by model binding and must have a value
        /// supplied in the request to be considered valid.
        /// </remarks>
        public abstract bool IsBindingRequired { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="UnderlyingOrModelType"/> is for an <see cref="Enum"/>.
        /// </summary>
        /// <value>
        /// <c>true</c> if <c>type.IsEnum</c> (<c>type.GetTypeInfo().IsEnum</c> for DNX Core 5.0) is <c>true</c> for
        /// <see cref="UnderlyingOrModelType"/>; <c>false</c> otherwise.
        /// </value>
        public abstract bool IsEnum { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="UnderlyingOrModelType"/> is for an <see cref="Enum"/> with an
        /// associated <see cref="FlagsAttribute"/>.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="IsEnum"/> is <c>true</c> and <see cref="UnderlyingOrModelType"/> has an
        /// associated <see cref="FlagsAttribute"/>; <c>false</c> otherwise.
        /// </value>
        public abstract bool IsFlagsEnum { get; }

        /// <summary>
        /// Gets a value indicating whether or not the model value is read-only. This is only applicable when
        /// the current instance represents a property.
        /// </summary>
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// Gets a value indicating whether or not the model value is required. This is only applicable when
        /// the current instance represents a property.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <c>true</c> then the model value is considered required by validators.
        /// </para>
        /// <para>
        /// By default an implicit <c>System.ComponentModel.DataAnnotations.RequiredAttribute</c> will be added
        /// if not present when <c>true.</c>.
        /// </para>
        /// </remarks>
        public abstract bool IsRequired { get; }

        /// <summary>
        /// Gets the <see cref="Metadata.ModelBindingMessageProvider"/> instance.
        /// </summary>
        public abstract ModelBindingMessageProvider ModelBindingMessageProvider { get; }

        /// <summary>
        /// Gets a value indicating where the current metadata should be ordered relative to other properties
        /// in its containing type.
        /// </summary>
        /// <remarks>
        /// <para>For example this property is used to order items in <see cref="Properties"/>.</para>
        /// <para>The default order is <c>10000</c>.</para>
        /// </remarks>
        /// <value>The order value of the current metadata.</value>
        public abstract int Order { get; }

        /// <summary>
        /// Gets the text to display as a placeholder value for an editor.
        /// </summary>
        public abstract string Placeholder { get; }

        /// <summary>
        /// Gets the text to display when the model is <c>null</c>.
        /// </summary>
        public abstract string NullDisplayText { get; }

        /// <summary>
        /// Gets the <see cref="IPropertyFilterProvider"/>, which can determine which properties
        /// should be model bound.
        /// </summary>
        public abstract IPropertyFilterProvider PropertyFilterProvider { get; }

        /// <summary>
        /// Gets a value that indicates whether the property should be displayed in read-only views.
        /// </summary>
        public abstract bool ShowForDisplay { get; }

        /// <summary>
        /// Gets a value that indicates whether the property should be displayed in editable views.
        /// </summary>
        public abstract bool ShowForEdit { get; }

        /// <summary>
        /// Gets  a value which is the name of the property used to display the model.
        /// </summary>
        public abstract string SimpleDisplayProperty { get; }

        /// <summary>
        /// Gets a string used by the templating system to discover display-templates and editor-templates.
        /// </summary>
        public abstract string TemplateHint { get; }

        /// <summary>
        /// Gets an <see cref="IPropertyValidationFilter"/> implementation that indicates whether this model should be
        /// validated. If <c>null</c>, properties with this <see cref="ModelMetadata"/> are validated.
        /// </summary>
        /// <value>Defaults to <c>null</c>.</value>
        public virtual IPropertyValidationFilter PropertyValidationFilter => null;

        /// <summary>
        /// Gets a value that indicates whether properties or elements of the model should be validated.
        /// </summary>
        public abstract bool ValidateChildren { get; }

        /// <summary>
        /// Gets a collection of metadata items for validators.
        /// </summary>
        public abstract IReadOnlyList<object> ValidatorMetadata { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> for elements of <see cref="ModelType"/> if that <see cref="Type"/>
        /// implements <see cref="IEnumerable"/>.
        /// </summary>
        public Type ElementType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether <see cref="ModelType"/> is a complex type.
        /// </summary>
        /// <remarks>
        /// A complex type is defined as a <see cref="Type"/> which has a
        /// <see cref="TypeConverter"/> that can convert from <see cref="string"/>.
        /// </remarks>
        public bool IsComplexType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not <see cref="ModelType"/> is a <see cref="Nullable{T}"/>.
        /// </summary>
        public bool IsNullableValueType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not <see cref="ModelType"/> is a collection type.
        /// </summary>
        /// <remarks>
        /// A collection type is defined as a <see cref="Type"/> which is assignable to <see cref="ICollection{T}"/>.
        /// </remarks>
        public bool IsCollectionType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not <see cref="ModelType"/> is an enumerable type.
        /// </summary>
        /// <remarks>
        /// An enumerable type is defined as a <see cref="Type"/> which is assignable to
        /// <see cref="IEnumerable"/>, and is not a <see cref="string"/>.
        /// </remarks>
        public bool IsEnumerableType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not <see cref="ModelType"/> allows <c>null</c> values.
        /// </summary>
        public bool IsReferenceOrNullableType { get; private set; }

        /// <summary>
        /// Gets the underlying type argument if <see cref="ModelType"/> inherits from <see cref="Nullable{T}"/>.
        /// Otherwise gets <see cref="ModelType"/>.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="ModelType"/> unless <see cref="IsNullableValueType"/> is <c>true</c>.
        /// </remarks>
        public Type UnderlyingOrModelType { get; private set; }

        /// <summary>
        /// Gets a property getter delegate to get the property value from a model object.
        /// </summary>
        public abstract Func<object, object> PropertyGetter { get; }

        /// <summary>
        /// Gets a property setter delegate to set the property value on a model object.
        /// </summary>
        public abstract Action<object, object> PropertySetter { get; }

        /// <summary>
        /// Gets a display name for the model.
        /// </summary>
        /// <remarks>
        /// <see cref="GetDisplayName()"/> will return the first of the following expressions which has a
        /// non-<c>null</c> value: <c>DisplayName</c>, <c>PropertyName</c>, <c>ModelType.Name</c>.
        /// </remarks>
        /// <returns>The display name.</returns>
        public string GetDisplayName()
        {
            return DisplayName ?? PropertyName ?? ModelType.Name;
        }

        /// <inheritdoc />
        public bool Equals(ModelMetadata other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }
            else
            {
                return Identity.Equals(other.Identity);
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as ModelMetadata);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Normally caching the hashcode would be dangerous, but Identity is deeply immutable so this is safe.
            if (_hashCode == null)
            {
                _hashCode = Identity.GetHashCode();
            }

            return _hashCode.Value;
        }

        private void InitializeTypeInformation()
        {
            Debug.Assert(ModelType != null);

            IsComplexType = !TypeDescriptor.GetConverter(ModelType).CanConvertFrom(typeof(string));
            IsNullableValueType = Nullable.GetUnderlyingType(ModelType) != null;
            IsReferenceOrNullableType = !ModelType.GetTypeInfo().IsValueType || IsNullableValueType;
            UnderlyingOrModelType = Nullable.GetUnderlyingType(ModelType) ?? ModelType;

            var collectionType = ClosedGenericMatcher.ExtractGenericInterface(ModelType, typeof(ICollection<>));
            IsCollectionType = collectionType != null;

            if (ModelType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(ModelType))
            {
                // Do nothing, not Enumerable.
            }
            else if (ModelType.IsArray)
            {
                IsEnumerableType = true;
                ElementType = ModelType.GetElementType();
            }
            else
            {
                IsEnumerableType = true;

                var enumerableType = ClosedGenericMatcher.ExtractGenericInterface(ModelType, typeof(IEnumerable<>));
                ElementType = enumerableType?.GenericTypeArguments[0];

                if (ElementType == null)
                {
                    // ModelType implements IEnumerable but not IEnumerable<T>.
                    ElementType = typeof(object);
                }

                Debug.Assert(
                    ElementType != null,
                    $"Unable to find element type for '{ModelType.FullName}' though IsEnumerableType is true.");
            }
        }

        private string DebuggerToString()
        {
            if (Identity.MetadataKind == ModelMetadataKind.Type)
            {
                return $"ModelMetadata (Type: '{ModelType.Name}')";
            }
            else
            {
                return $"ModelMetadata (Property: '{ContainerType.Name}.{PropertyName}' Type: '{ModelType.Name}')";
            }
        }

        /// <inheritdoc />
        public virtual ModelMetadata GetMetadataForType(Type modelType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public virtual IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType)
        {
            throw new NotImplementedException();
        }
    }
}
