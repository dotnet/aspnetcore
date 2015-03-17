// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A metadata representation of a model type, property or parameter.
    /// </summary>
    public abstract class ModelMetadata
    {
        /// <summary>
        /// The default value of <see cref="ModelMetadata.Order"/>.
        /// </summary>
        public static readonly int DefaultOrder = 10000;

        /// <summary>
        /// Creates a new <see cref="ModelMetadata"/>.
        /// </summary>
        /// <param name="identity">The <see cref="ModelMetadataIdentity"/>.</param>
        protected ModelMetadata([NotNull] ModelMetadataIdentity identity)
        {
            Identity = identity;
        }

        /// <summary>
        /// Gets the container type of this metadata if it represents a property, otherwise <c>null</c>.
        /// </summary>
        public Type ContainerType { get { return Identity.ContainerType; } }

        /// <summary>
        /// Gets a value indicating the kind of metadata element represented by the current instance.
        /// </summary>
        public ModelMetadataKind MetadataKind { get { return Identity.MetadataKind; } }

        /// <summary>
        /// Gets the model type represented by the current instance.
        /// </summary>
        public Type ModelType { get { return Identity.ModelType; } }

        /// <summary>
        /// Gets the property name represented by the current instance.
        /// </summary>
        public string PropertyName
        {
            get
            {
                return Identity.Name;
            }
        }

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
        /// Gets the <see cref="Type"/> of an <see cref="IModelBinder"/> or an
        /// <see cref="IModelBinderProvider"/> of a model if specified explicitly using 
        /// <see cref="IBinderTypeProviderMetadata"/>.
        /// </summary>
        public abstract Type BinderType { get; }

        /// <summary>
        /// Gets a binder metadata for this model.
        /// </summary>
        public abstract BindingSource BindingSource { get; }

        /// <summary>
        /// Gets a value indicating whether or not to convert an empty string value to <c>null</c> when
        /// representing a model as text.
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
        /// Gets the composite format <see cref="string"/> (see
        /// http://msdn.microsoft.com/en-us/library/txafckwd.aspx) used to display the model.
        /// </summary>
        public abstract string DisplayFormatString { get; }

        /// <summary>
        /// Gets the display name of the model.
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Gets the composite format <see cref="string"/> (see
        /// http://msdn.microsoft.com/en-us/library/txafckwd.aspx) used to edit the model.
        /// </summary>
        public abstract string EditFormatString { get; }

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
        /// Gets a value indicating whether or not the model value is read-only. This is only applicable when
        /// the current instance represents a property.
        /// </summary>
        public abstract bool IsReadOnly { get; }

        /// <summary>
        /// Gets a value indicating whether or not the model value is required. This is only applicable when
        /// the current instance represents a property.
        /// </summary>
        public abstract bool IsRequired { get; }

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
        /// Gets the text to display when the model is <c>null</c>.
        /// </summary>
        public abstract string NullDisplayText { get; }

        /// <summary>
        /// Gets the <see cref="IPropertyBindingPredicateProvider"/>, which can determine which properties
        /// should be model bound.
        /// </summary>
        public abstract IPropertyBindingPredicateProvider PropertyBindingPredicateProvider { get; }

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
        /// Gets a collection of metadata items for validators.
        /// </summary>
        public abstract IReadOnlyList<object> ValidatorMetadata { get;}

        /// <summary>
        /// Gets a value indicating whether <see cref="ModelType"/> is a simple type.
        /// </summary>
        /// <remarks>
        /// A simple type is defined as a <see cref="Type"/> which has a
        /// <see cref="System.ComponentModel.TypeConverter"/> that can convert from <see cref="string"/>.
        /// </remarks>
        public bool IsComplexType
        {
            get { return !TypeHelper.HasStringConverter(ModelType); }
        }

        /// <summary>
        /// Gets a value indicating whether or not <see cref="ModelType"/> is a <see cref="Nullable{T}"/>.
        /// </summary>
        public bool IsNullableValueType
        {
            get { return ModelType.IsNullableValueType(); }
        }

        /// <summary>
        /// Gets a value indicating whether or not <see cref="ModelType"/> is a collection type.
        /// </summary>
        /// <remarks>
        /// A collection type is defined as a <see cref="Type"/> which is assignable to
        /// <see cref="System.Collections.IEnumerable"/>, and is not a <see cref="string"/>.
        /// </remarks>
        public bool IsCollectionType
        {
            get { return TypeHelper.IsCollectionType(ModelType); }
        }

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
    }
} 
