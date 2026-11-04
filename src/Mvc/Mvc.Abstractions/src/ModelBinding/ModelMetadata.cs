// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A metadata representation of a model type, property or parameter.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public abstract class ModelMetadata : IEquatable<ModelMetadata?>, IModelMetadataProvider
{
    /// <summary>
    /// The default value of <see cref="ModelMetadata.Order"/>.
    /// </summary>
    public static readonly int DefaultOrder = 10000;

    internal const string RequiresUnreferencedCodeMessage = "Resolving this property is not compatible with trimming, as it requires dynamic access to code that is not referenced statically.";
    internal const string RequiresDynamicCodeMessage = "Resolving this property may require dynamic code generation.";

    /// <summary>
    /// Exposes a feature switch to disable generating model metadata with reflection-heavy strategies.
    /// This is primarily intended for use in Minimal API-based scenarios where information is derived from
    /// IParameterBindingMetadata
    /// </summary>
    [FeatureSwitchDefinition("Microsoft.AspNetCore.Mvc.ApiExplorer.IsEnhancedModelMetadataSupported")]
    [FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
    [FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
    private static bool IsEnhancedModelMetadataSupported { get; } =
        AppContext.TryGetSwitch("Microsoft.AspNetCore.Mvc.ApiExplorer.IsEnhancedModelMetadataSupported", out var isEnhancedModelMetadataSupported) ? isEnhancedModelMetadataSupported : true;

    private int? _hashCode;
    private IReadOnlyList<ModelMetadata>? _boundProperties;
    private IReadOnlyDictionary<ModelMetadata, ModelMetadata>? _parameterMapping;
    private IReadOnlyDictionary<ModelMetadata, ModelMetadata>? _boundConstructorPropertyMapping;
    private Exception? _recordTypeValidatorsOnPropertiesError;
    private bool _recordTypeConstructorDetailsCalculated;

    /// <summary>
    /// Creates a new <see cref="ModelMetadata"/>.
    /// </summary>
    /// <param name="identity">The <see cref="ModelMetadataIdentity"/>.</param>
    protected ModelMetadata(ModelMetadataIdentity identity)
    {
        Identity = identity;

        InitializeTypeInformation();
        if (IsEnhancedModelMetadataSupported)
        {
            InitializeDynamicTypeInformation();
        }
    }

    /// <summary>
    /// Gets the type containing the property if this metadata is for a property; <see langword="null"/> otherwise.
    /// </summary>
    public Type? ContainerType => Identity.ContainerType;

    /// <summary>
    /// Gets the metadata for <see cref="ContainerType"/> if this metadata is for a property;
    /// <see langword="null"/> otherwise.
    /// </summary>
    public virtual ModelMetadata? ContainerMetadata
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
    /// Gets the name of the parameter or property if this metadata is for a parameter or property;
    /// <see langword="null"/> otherwise i.e. if this is the metadata for a type.
    /// </summary>
    public string? Name => Identity.Name;

    /// <summary>
    /// Gets the name of the parameter if this metadata is for a parameter; <see langword="null"/> otherwise.
    /// </summary>
    public string? ParameterName => MetadataKind == ModelMetadataKind.Parameter ? Identity.Name : null;

    /// <summary>
    /// Gets the name of the property if this metadata is for a property; <see langword="null"/> otherwise.
    /// </summary>
    public string? PropertyName => MetadataKind == ModelMetadataKind.Property ? Identity.Name : null;

    /// <summary>
    /// Gets the key for the current instance.
    /// </summary>
    protected internal ModelMetadataIdentity Identity { get; }

    /// <summary>
    /// Gets a collection of additional information about the model.
    /// </summary>
    public abstract IReadOnlyDictionary<object, object> AdditionalValues { get; }

    /// <summary>
    /// Gets the collection of <see cref="ModelMetadata"/> instances for the model's properties.
    /// </summary>
    public abstract ModelPropertyCollection Properties { get; }

    internal IReadOnlyList<ModelMetadata> BoundProperties
    {
        get
        {
            // In record types, each constructor parameter in the primary constructor is also a settable property with the same name.
            // Executing model binding on these parameters twice may have detrimental effects, such as duplicate ModelState entries,
            // or failures if a model expects to be bound exactly ones.
            // Consequently when binding to a constructor, we only bind and validate the subset of properties whose names
            // haven't appeared as parameters.
            if (BoundConstructor is null)
            {
                return Properties;
            }

            if (_boundProperties is null)
            {
                var boundParameters = BoundConstructor.BoundConstructorParameters!;
                var boundProperties = new List<ModelMetadata>();

                foreach (var metadata in Properties)
                {
                    if (!boundParameters.Any(p =>
                        string.Equals(p.ParameterName, metadata.PropertyName, StringComparison.Ordinal)
                        && p.ModelType == metadata.ModelType))
                    {
                        boundProperties.Add(metadata);
                    }
                }

                _boundProperties = boundProperties;
            }

            return _boundProperties;
        }
    }

    /// <summary>
    /// A mapping from parameters to their corresponding properties on a record type.
    /// </summary>
    internal IReadOnlyDictionary<ModelMetadata, ModelMetadata> BoundConstructorParameterMapping
    {
        get
        {
            Debug.Assert(BoundConstructor != null, "This API can be only called for types with bound constructors.");
            CalculateRecordTypeConstructorDetails();

            return _parameterMapping;
        }
    }

    /// <summary>
    /// A mapping from properties to their corresponding constructor parameter on a record type.
    /// This is the inverse mapping of <see cref="BoundConstructorParameterMapping"/>.
    /// </summary>
    internal IReadOnlyDictionary<ModelMetadata, ModelMetadata> BoundConstructorPropertyMapping
    {
        get
        {
            Debug.Assert(BoundConstructor != null, "This API can be only called for types with bound constructors.");
            CalculateRecordTypeConstructorDetails();

            return _boundConstructorPropertyMapping;
        }
    }

    /// <summary>
    /// Gets <see cref="ModelMetadata"/> instance for a constructor of a record type that is used during binding and validation.
    /// </summary>
    public virtual ModelMetadata? BoundConstructor { get; }

    /// <summary>
    /// Gets the collection of <see cref="ModelMetadata"/> instances for parameters on a <see cref="BoundConstructor"/>.
    /// This is only available when <see cref="MetadataKind"/> is <see cref="ModelMetadataKind.Constructor"/>.
    /// </summary>
    public virtual IReadOnlyList<ModelMetadata>? BoundConstructorParameters { get; }

    /// <summary>
    /// Gets the name of a model if specified explicitly using <see cref="IModelNameProvider"/>.
    /// </summary>
    public abstract string? BinderModelName { get; }

    /// <summary>
    /// Gets the <see cref="Type"/> of an <see cref="IModelBinder"/> of a model if specified explicitly using
    /// <see cref="IBinderTypeProviderMetadata"/>.
    /// </summary>
    public abstract Type? BinderType { get; }

    /// <summary>
    /// Gets a binder metadata for this model.
    /// </summary>
    public abstract BindingSource? BindingSource { get; }

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
    public abstract string? DataTypeName { get; }

    /// <summary>
    /// Gets the description of the model.
    /// </summary>
    public abstract string? Description { get; }

    /// <summary>
    /// Gets the format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) used to display the
    /// model.
    /// </summary>
    public abstract string? DisplayFormatString { get; }

    /// <summary>
    /// Gets the display name of the model.
    /// </summary>
    public abstract string? DisplayName { get; }

    /// <summary>
    /// Gets the format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) used to edit the model.
    /// </summary>
    public abstract string? EditFormatString { get; }

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
    public abstract ModelMetadata? ElementMetadata { get; }

    /// <summary>
    /// Gets the ordered and grouped display names and values of all <see cref="Enum"/> values in
    /// <see cref="UnderlyingOrModelType"/>.
    /// </summary>
    /// <value>
    /// An <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{EnumGroupAndName, String}"/> of mappings between
    /// <see cref="Enum"/> field groups, names and values. <c>null</c> if <see cref="IsEnum"/> is <c>false</c>.
    /// </value>
    public abstract IEnumerable<KeyValuePair<EnumGroupAndName, string>>? EnumGroupedDisplayNamesAndValues { get; }

    /// <summary>
    /// Gets the names and values of all <see cref="Enum"/> values in <see cref="UnderlyingOrModelType"/>.
    /// </summary>
    /// <value>
    /// An <see cref="IReadOnlyDictionary{String, String}"/> of mappings between <see cref="Enum"/> field names
    /// and values. <c>null</c> if <see cref="IsEnum"/> is <c>false</c>.
    /// </value>
    public abstract IReadOnlyDictionary<string, string>? EnumNamesAndValues { get; }

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
    /// <value>The order value of the current metadata.</value>
    /// <remarks>
    /// <para>For example this property is used to order items in <see cref="Properties"/>.</para>
    /// <para>The default order is <c>10000</c>.</para>
    /// </remarks>
    public abstract int Order { get; }

    /// <summary>
    /// Gets the text to display as a placeholder value for an editor.
    /// By default, this is configured using <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.Prompt" />.
    /// </summary>
    public abstract string? Placeholder { get; }

    /// <summary>
    /// Gets the text to display when the model is <c>null</c>.
    /// </summary>
    public abstract string? NullDisplayText { get; }

    /// <summary>
    /// Gets the <see cref="IPropertyFilterProvider"/>, which can determine which properties
    /// should be model bound.
    /// </summary>
    public abstract IPropertyFilterProvider? PropertyFilterProvider { get; }

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
    public abstract string? SimpleDisplayProperty { get; }

    /// <summary>
    /// Gets a string used by the templating system to discover display-templates and editor-templates.
    /// Use <see cref="System.ComponentModel.DataAnnotations.UIHintAttribute" /> to specify.
    /// </summary>
    public abstract string? TemplateHint { get; }

    /// <summary>
    /// Gets an <see cref="IPropertyValidationFilter"/> implementation that indicates whether this model should be
    /// validated. If <c>null</c>, properties with this <see cref="ModelMetadata"/> are validated.
    /// </summary>
    /// <value>Defaults to <c>null</c>.</value>
    public virtual IPropertyValidationFilter? PropertyValidationFilter => null;

    /// <summary>
    /// Gets a value that indicates whether properties or elements of the model should be validated.
    /// </summary>
    public abstract bool ValidateChildren { get; }

    /// <summary>
    /// Gets a value that indicates if the model, or one of its properties or elements, has associated validators.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, validation can be assume that the model is valid (<see cref="ModelValidationState.Valid"/>) without
    /// inspecting the object graph.
    /// </remarks>
    public virtual bool? HasValidators { get; }

    /// <summary>
    /// Gets a collection of metadata items for validators.
    /// </summary>
    public abstract IReadOnlyList<object> ValidatorMetadata { get; }

    private Type? _elementType;

    /// <summary>
    /// Gets the <see cref="Type"/> for elements of <see cref="ModelType"/> if that <see cref="Type"/>
    /// implements <see cref="IEnumerable"/>.
    /// </summary>
    public Type? ElementType
    {
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        get
        {
            if (!IsEnhancedModelMetadataSupported)
            {
                throw new NotSupportedException("ElementType is not initialized when `Microsoft.AspNetCore.Mvc.ApiExplorer.IsEnhancedModelMetadataSupported` is false.");
            }
            return _elementType;
        }
        private set
        {
            _elementType = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="ModelType"/> is a complex type.
    /// </summary>
    /// <remarks>
    /// A complex type is defined as a <see cref="Type"/> without a <see cref="TypeConverter"/> that can convert
    /// from <see cref="string"/> and without a <c>TryParse</c> method. Most POCO and <see cref="IEnumerable"/> types are therefore complex.
    /// Most, if not all, BCL value types are simple types.
    /// </remarks>
    public bool IsComplexType
    {
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        get
        {
            return !IsConvertibleType && !IsParseableType;
        }
    }

    /// <summary>
    /// Gets a value indicating whether or not <see cref="ModelType"/> is a <see cref="Nullable{T}"/>.
    /// </summary>
    public bool IsNullableValueType { get; private set; }

    private bool? _isCollectionType;

    /// <summary>
    /// Gets a value indicating whether or not <see cref="ModelType"/> is a collection type.
    /// </summary>
    /// <remarks>
    /// A collection type is defined as a <see cref="Type"/> which is assignable to <see cref="ICollection{T}"/>.
    /// </remarks>
    public bool IsCollectionType
    {
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        get
        {
            if (_isCollectionType == null)
            {
                throw new NotSupportedException("IsCollectionType is not initialized when `Microsoft.AspNetCore.Mvc.ApiExplorer.IsEnhancedModelMetadataSupported` is false.");
            }
            return _isCollectionType.Value;
        }
        private set
        {
            _isCollectionType = value;
        }
    }

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
    public Type UnderlyingOrModelType { get; private set; } = default!;

    private bool? _isParseableType;

    /// <summary>
    /// Gets a value indicating whether or not <see cref="ModelType"/> has a TryParse method.
    /// </summary>
    internal virtual bool IsParseableType
    {
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        get
        {
            if (!_isParseableType.HasValue)
            {
                throw new NotSupportedException("IsParseableType is not initialized when `Microsoft.AspNetCore.Mvc.ApiExplorer.IsEnhancedModelMetadataSupported` is false.");
            }
            return _isParseableType.Value;
        }
        private set
        {
            _isParseableType = value;
        }
    }

    private bool? _isConvertibleType;

    /// <summary>
    /// Gets a value indicating whether or not <see cref="ModelType"/> has a <see cref="TypeConverter"/>
    /// from <see cref="string"/>.
    /// </summary>
    internal bool IsConvertibleType
    {
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        get
        {
            if (!_isConvertibleType.HasValue)
            {
                throw new NotSupportedException("IsConvertibleType is not initialized when `Microsoft.AspNetCore.Mvc.ApiExplorer.IsEnhancedModelMetadataSupported` is false.");

            }
            return _isConvertibleType.Value;
        }
        private set
        {
            _isConvertibleType = value;
        }
    }

    /// <summary>
    /// Gets a value indicating the NullabilityState of the value or reference type.
    /// </summary>
    /// <remarks>
    /// The state will be set for Parameters and Properties <see cref="ModelMetadataKind"/>
    /// otherwise the state will be <c>NullabilityState.Unknown</c>
    /// </remarks>
    internal NullabilityState NullabilityState { get; set; }

    /// <summary>
    /// Gets a property getter delegate to get the property value from a model object.
    /// </summary>
    public abstract Func<object, object?>? PropertyGetter { get; }

    /// <summary>
    /// Gets a property setter delegate to set the property value on a model object.
    /// </summary>
    public abstract Action<object, object?>? PropertySetter { get; }

    /// <summary>
    /// Gets a delegate that invokes the bound constructor <see cref="BoundConstructor" /> if non-<see langword="null" />.
    /// </summary>
    public virtual Func<object?[], object>? BoundConstructorInvoker => null;

    /// <summary>
    /// Gets a value that determines if validators can be constructed using metadata exclusively defined on the property.
    /// </summary>
    internal virtual bool PropertyHasValidators => false;

    /// <summary>
    /// Gets the name of a model, if specified explicitly, to be used on <see cref="ValidationEntry"/>
    /// </summary>
    internal virtual string? ValidationModelName { get; }

    /// <summary>
    /// Gets the value that indicates if the parameter has a default value set.
    /// This is only available when <see cref="MetadataKind"/> is <see cref="ModelMetadataKind.Parameter"/> otherwise it will be false.
    /// </summary>
    internal bool HasDefaultValue { get; private set; }

    /// <summary>
    /// Throws if the ModelMetadata is for a record type with validation on properties.
    /// </summary>
    internal void ThrowIfRecordTypeHasValidationOnProperties()
    {
        CalculateRecordTypeConstructorDetails();
        if (_recordTypeValidatorsOnPropertiesError != null)
        {
            throw _recordTypeValidatorsOnPropertiesError;
        }
    }

    [RequiresUnreferencedCode("Finding the TryParse method via reflection is not trim compatible.")]
    [RequiresDynamicCode("Finding the TryParse method via reflection is not native AOT compatible.")]
    internal static Func<ParameterExpression, Expression, Expression>? FindTryParseMethod(Type modelType)
    {
        if (modelType.IsByRef)
        {
            // ByRef is no supported in this case and
            // will be reported later for the user.
            return null;
        }

        modelType = Nullable.GetUnderlyingType(modelType) ?? modelType;
        return ParameterBindingMethodCache.NonThrowingInstance.FindTryParseMethod(modelType);
    }

    [MemberNotNull(nameof(_parameterMapping), nameof(_boundConstructorPropertyMapping))]
    private void CalculateRecordTypeConstructorDetails()
    {
        if (_recordTypeConstructorDetailsCalculated)
        {
            Debug.Assert(_parameterMapping != null);
            Debug.Assert(_boundConstructorPropertyMapping != null);
            return;
        }

        var boundParameters = BoundConstructor!.BoundConstructorParameters!;
        var parameterMapping = new Dictionary<ModelMetadata, ModelMetadata>();
        var propertyMapping = new Dictionary<ModelMetadata, ModelMetadata>();

        foreach (var parameter in boundParameters)
        {
            var property = Properties.FirstOrDefault(p =>
                string.Equals(p.Name, parameter.ParameterName, StringComparison.Ordinal) &&
                p.ModelType == parameter.ModelType);

            if (property != null)
            {
                parameterMapping[parameter] = property;
                propertyMapping[property] = parameter;

                if (property.PropertyHasValidators)
                {
                    // When constructing the mapping of parameters -> properties, also determine
                    // if the property has any validators (without looking at metadata on the type).
                    // This will help us throw during validation if a user defines validation attributes
                    // on the property of a record type.
                    _recordTypeValidatorsOnPropertiesError = new InvalidOperationException(
                        Resources.FormatRecordTypeHasValidationOnProperties(ModelType, property.Name));
                }
            }
        }

        _recordTypeConstructorDetailsCalculated = true;
        _parameterMapping = parameterMapping;
        _boundConstructorPropertyMapping = propertyMapping;
    }

    /// <summary>
    /// Gets a display name for the model.
    /// </summary>
    /// <remarks>
    /// <see cref="GetDisplayName()"/> will return the first of the following expressions which has a
    /// non-<see langword="null"/> value: <see cref="DisplayName"/>, <see cref="Name"/>, or <c>ModelType.Name</c>.
    /// </remarks>
    /// <returns>The display name.</returns>
    public string GetDisplayName()
    {
        return DisplayName ?? Name ?? ModelType.Name;
    }

    /// <inheritdoc />
    public bool Equals(ModelMetadata? other)
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
    public override bool Equals(object? obj)
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

        IsNullableValueType = Nullable.GetUnderlyingType(ModelType) != null;
        IsReferenceOrNullableType = !ModelType.IsValueType || IsNullableValueType;
        UnderlyingOrModelType = Nullable.GetUnderlyingType(ModelType) ?? ModelType;
        HasDefaultValue = MetadataKind == ModelMetadataKind.Parameter && Identity.ParameterInfo!.HasDefaultValue;

        var nullabilityContext = new NullabilityInfoContext();
        var nullability = MetadataKind switch
        {
            ModelMetadataKind.Parameter => Identity.ParameterInfo != null ? nullabilityContext.Create(Identity.ParameterInfo!) : null,
            ModelMetadataKind.Property => Identity.PropertyInfo != null ? nullabilityContext.Create(Identity.PropertyInfo!) : null,
            _ => null
        };
        NullabilityState = nullability?.ReadState ?? NullabilityState.Unknown;

        if (ModelType.IsArray)
        {
            IsEnumerableType = true;
            ElementType = ModelType.GetElementType()!;
        }
    }

    [RequiresUnreferencedCode("Using ModelMetadata with IsEnhancedModelMetadataSupport=true is not trim compatible.")]
    [RequiresDynamicCode("Using ModelMetadata with IsEnhancedModelMetadataSupport=true is not native AOT compatible.")]
    private void InitializeDynamicTypeInformation()
    {
        Debug.Assert(ModelType != null);
        IsConvertibleType = TypeDescriptor.GetConverter(ModelType).CanConvertFrom(typeof(string));
        IsParseableType = FindTryParseMethod(ModelType) is not null;

        var collectionType = ClosedGenericMatcher.ExtractGenericInterface(ModelType, typeof(ICollection<>));
        _isCollectionType = collectionType != null;

        if (ModelType != typeof(string) && !ModelType.IsArray && typeof(IEnumerable).IsAssignableFrom(ModelType))
        {
            IsEnumerableType = true;
            var enumerableType = ClosedGenericMatcher.ExtractGenericInterface(ModelType, typeof(IEnumerable<>));
            // Apply fallback when ModelType implements IEnumerable but not IEnumerable<T>.
            ElementType = enumerableType?.GenericTypeArguments[0] ?? typeof(object);

            Debug.Assert(
                ElementType != null,
                $"Unable to find element type for '{ModelType.FullName}' though IsEnumerableType is true.");
        }
    }

    private string DebuggerToString()
    {
        switch (MetadataKind)
        {
            case ModelMetadataKind.Parameter:
                return $"ModelMetadata (Parameter: '{ParameterName}' Type: '{ModelType.Name}')";
            case ModelMetadataKind.Property:
                return $"ModelMetadata (Property: '{ContainerType!.Name}.{PropertyName}' Type: '{ModelType.Name}')";
            case ModelMetadataKind.Type:
                return $"ModelMetadata (Type: '{ModelType.Name}')";
            case ModelMetadataKind.Constructor:
                return $"ModelMetadata (Constructor: '{ModelType.Name}')";
            default:
                return $"Unsupported MetadataKind '{MetadataKind}'.";
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
