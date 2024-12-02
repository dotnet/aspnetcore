// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// A <see cref="IDictionary{TKey, TValue}"/> for view data.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(DictionaryDebugView<string, object?>))]
public class ViewDataDictionary : IDictionary<string, object?>
{
    private readonly IDictionary<string, object?> _data;
    private readonly Type _declaredModelType;
    private readonly IModelMetadataProvider _metadataProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class.
    /// </summary>
    /// <param name="metadataProvider">
    /// <see cref="IModelMetadataProvider"/> instance used to create <see cref="ViewFeatures.ModelExplorer"/>
    /// instances.
    /// </param>
    /// <param name="modelState"><see cref="ModelStateDictionary"/> instance for this scope.</param>
    /// <remarks>For use when creating a <see cref="ViewDataDictionary"/> for a new top-level scope.</remarks>
    public ViewDataDictionary(
        IModelMetadataProvider metadataProvider,
        ModelStateDictionary modelState)
        : this(metadataProvider, modelState, declaredModelType: typeof(object))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class based entirely on an existing
    /// instance.
    /// </summary>
    /// <param name="source"><see cref="ViewDataDictionary"/> instance to copy initial values from.</param>
    /// <remarks>
    /// <para>
    /// For use when copying a <see cref="ViewDataDictionary"/> instance and the declared <see cref="Model"/>
    /// <see cref="Type"/> will not change e.g. when copying from a <see cref="ViewDataDictionary{TModel}"/>
    /// instance to a base <see cref="ViewDataDictionary"/> instance.
    /// </para>
    /// <para>
    /// This constructor should not be used in any context where <see cref="Model"/> may be set to a value
    /// incompatible with the declared type of <paramref name="source"/>.
    /// </para>
    /// </remarks>
    public ViewDataDictionary(ViewDataDictionary source)
        : this(source, source.Model, source._declaredModelType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class.
    /// </summary>
    /// <param name="metadataProvider">
    /// <see cref="IModelMetadataProvider"/> instance used to create <see cref="ViewFeatures.ModelExplorer"/>
    /// instances.
    /// </param>
    /// <remarks>Internal for testing.</remarks>
    internal ViewDataDictionary(IModelMetadataProvider metadataProvider)
        : this(metadataProvider, new ModelStateDictionary())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class.
    /// </summary>
    /// <param name="metadataProvider">
    /// <see cref="IModelMetadataProvider"/> instance used to create <see cref="ViewFeatures.ModelExplorer"/>
    /// instances.
    /// </param>
    /// <param name="declaredModelType">
    /// <see cref="Type"/> of <see cref="Model"/> values expected. Used to set <see cref="ModelMetadata"/>.
    /// </param>
    /// <remarks>
    /// For use when creating a derived <see cref="ViewDataDictionary"/> for a new top-level scope.
    /// </remarks>
    protected ViewDataDictionary(
        IModelMetadataProvider metadataProvider,
        Type declaredModelType)
        : this(metadataProvider, new ModelStateDictionary(), declaredModelType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class.
    /// </summary>
    /// <param name="metadataProvider">
    /// <see cref="IModelMetadataProvider"/> instance used to create <see cref="ViewFeatures.ModelExplorer"/>
    /// instances.
    /// </param>
    /// <param name="modelState"><see cref="ModelStateDictionary"/> instance for this scope.</param>
    /// <param name="declaredModelType">
    /// <see cref="Type"/> of <see cref="Model"/> values expected. Used to set <see cref="ModelMetadata"/>.
    /// </param>
    /// <remarks>
    /// For use when creating a derived <see cref="ViewDataDictionary"/> for a new top-level scope.
    /// </remarks>
    // This is the core constructor called when Model is unknown.
    protected ViewDataDictionary(
        IModelMetadataProvider metadataProvider,
        ModelStateDictionary modelState,
        Type declaredModelType)
        : this(metadataProvider,
               modelState,
               declaredModelType,
               data: new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
               templateInfo: new TemplateInfo())
    {
        ArgumentNullException.ThrowIfNull(metadataProvider);
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(declaredModelType);

        // Base ModelMetadata on the declared type.
        ModelExplorer = _metadataProvider.GetModelExplorerForType(declaredModelType, model: null);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class based in part on an existing
    /// instance.
    /// </summary>
    /// <param name="source"><see cref="ViewDataDictionary"/> instance to copy initial values from.</param>
    /// <param name="declaredModelType">
    /// <see cref="Type"/> of <see cref="Model"/> values expected. Used to set <see cref="ModelMetadata"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// For use when copying a <see cref="ViewDataDictionary"/> instance and new instance's declared
    /// <see cref="Model"/> <see cref="Type"/> is known but <see cref="Model"/> should be copied from the existing
    /// instance e.g. when copying from a base <see cref="ViewDataDictionary"/> instance to a
    /// <see cref="ViewDataDictionary{TModel}"/> instance.
    /// </para>
    /// <para>
    /// This constructor may <c>throw</c> if <c>source.Model</c> is non-<c>null</c> and incompatible with
    /// <paramref name="declaredModelType"/>. Pass <c>model: null</c> to
    /// <see cref="ViewDataDictionary(ViewDataDictionary, object, Type)"/> to ignore <c>source.Model</c>.
    /// </para>
    /// </remarks>
    protected ViewDataDictionary(ViewDataDictionary source, Type declaredModelType)
        : this(source, model: source.Model, declaredModelType: declaredModelType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class based in part on an existing
    /// instance. This constructor is careful to avoid exceptions <see cref="SetModel"/> may throw when
    /// <paramref name="model"/> is <c>null</c>.
    /// </summary>
    /// <param name="source"><see cref="ViewDataDictionary"/> instance to copy initial values from.</param>
    /// <param name="model">Value for the <see cref="Model"/> property.</param>
    /// <param name="declaredModelType">
    /// <see cref="Type"/> of <see cref="Model"/> values expected. Used to set <see cref="ModelMetadata"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// For use when copying a <see cref="ViewDataDictionary"/> instance and new instance's declared
    /// <see cref="Model"/> <see cref="Type"/> and <see cref="Model"/> are known.
    /// </para>
    /// <para>
    /// This constructor may <c>throw</c> if <paramref name="model"/> is non-<c>null</c> and incompatible with
    /// <paramref name="declaredModelType"/>.
    /// </para>
    /// </remarks>
    // This is the core constructor called when Model is known.
    protected ViewDataDictionary(ViewDataDictionary source, object? model, Type declaredModelType)
        : this(source._metadataProvider,
               source.ModelState,
               declaredModelType,
               data: new CopyOnWriteDictionary<string, object?>(source, StringComparer.OrdinalIgnoreCase),
               templateInfo: new TemplateInfo(source.TemplateInfo))
    {
        ArgumentNullException.ThrowIfNull(source);

        // A non-null Model must always be assignable to both _declaredModelType and ModelMetadata.ModelType.
        //
        // ModelMetadata.ModelType should also be assignable to _declaredModelType. Though corner cases exist such
        // as a ViewDataDictionary<List<int>> holding information about an IEnumerable<int> property (because an
        // @model directive matched the runtime type though the view's name did not), we'll throw away the property
        // metadata in those cases -- preserving invariant that ModelType can be assigned to _declaredModelType.
        //
        // More generally, since defensive copies to base VDD and VDD<object> abound, it's important to preserve
        // metadata despite _declaredModelType changes.
        var modelType = model?.GetType();
        var modelOrDeclaredType = modelType ?? declaredModelType;
        if (source.ModelMetadata.MetadataKind == ModelMetadataKind.Type &&
            source.ModelMetadata.ModelType == typeof(object) &&
            modelOrDeclaredType != typeof(object))
        {
            // Base ModelMetadata on new type when there's no property information to preserve and type changes to
            // something besides typeof(object).
            ModelExplorer = _metadataProvider.GetModelExplorerForType(modelOrDeclaredType, model);
        }
        else if (!declaredModelType.IsAssignableFrom(source.ModelMetadata.ModelType))
        {
            // Base ModelMetadata on new type when existing metadata is incompatible with the new declared type.
            ModelExplorer = _metadataProvider.GetModelExplorerForType(modelOrDeclaredType, model);
        }
        else if (modelType != null && !source.ModelMetadata.ModelType.IsAssignableFrom(modelType))
        {
            // Base ModelMetadata on new type when new model is incompatible with the existing metadata.
            ModelExplorer = _metadataProvider.GetModelExplorerForType(modelType, model);
        }
        else if (object.ReferenceEquals(model, source.ModelExplorer.Model))
        {
            // Source's ModelExplorer is already exactly correct.
            ModelExplorer = source.ModelExplorer;
        }
        else
        {
            // The existing metadata is compatible with the value and declared type but it's a new value.
            ModelExplorer = new ModelExplorer(
                _metadataProvider,
                source.ModelExplorer.Container,
                source.ModelMetadata,
                model);
        }

        // Ensure the given Model is compatible with _declaredModelType. Do not do this one of the following
        // special cases:
        // - Constructing a ViewDataDictionary<TModel> where TModel is a non-Nullable value type. This may for
        // example occur when activating a RazorPage<int> and the container is null.
        // - Constructing a ViewDataDictionary<object> immediately before overwriting ModelExplorer with correct
        // information. See TemplateBuilder.Build().
        if (model != null)
        {
            EnsureCompatible(model);
        }
    }

    private ViewDataDictionary(
        IModelMetadataProvider metadataProvider,
        ModelStateDictionary modelState,
        Type declaredModelType,
        IDictionary<string, object?> data,
        TemplateInfo templateInfo)
    {
        _metadataProvider = metadataProvider;
        ModelState = modelState;
        _declaredModelType = declaredModelType;
        _data = data;
        TemplateInfo = templateInfo;
    }

    /// <summary>
    /// Gets or sets the current model.
    /// </summary>
    public object? Model
    {
        get
        {
            return ModelExplorer.Model;
        }
        set
        {
            // Reset ModelExplorer to ensure Model and ModelExplorer.Model remain equal.
            SetModel(value);
        }
    }

    /// <summary>
    /// Gets the <see cref="ModelStateDictionary"/>.
    /// </summary>
    public ModelStateDictionary ModelState { get; }

    /// <summary>
    /// Gets the <see cref="ModelBinding.ModelMetadata"/> for an expression, the <see cref="Model"/> (if
    /// non-<c>null</c>), or the declared <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// Value is never <c>null</c> but may describe the <see cref="object"/> class in some cases. This may for
    /// example occur in controllers.
    /// </remarks>
    public ModelMetadata ModelMetadata
    {
        get
        {
            return ModelExplorer.Metadata;
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="ViewFeatures.ModelExplorer"/> for the <see cref="Model"/>.
    /// </summary>
    public ModelExplorer ModelExplorer { get; set; } = default!;

    /// <summary>
    /// Gets the <see cref="ViewFeatures.TemplateInfo"/>.
    /// </summary>
    public TemplateInfo TemplateInfo { get; }

    #region IDictionary properties
    /// <inheritdoc />
    // Do not just pass through to _data: Indexer should not throw a KeyNotFoundException.
    public object? this[string index]
    {
        get
        {
            _data.TryGetValue(index, out var result);
            return result;
        }
        set
        {
            _data[index] = value;
        }
    }

    /// <inheritdoc />
    public int Count
    {
        get { return _data.Count; }
    }

    /// <inheritdoc />
    public bool IsReadOnly
    {
        get { return _data.IsReadOnly; }
    }

    /// <inheritdoc />
    public ICollection<string> Keys
    {
        get { return _data.Keys; }
    }

    /// <inheritdoc />
    public ICollection<object?> Values
    {
        get { return _data.Values; }
    }
    #endregion

    // for unit testing
    internal IDictionary<string, object?> Data
    {
        get { return _data; }
    }

    /// <summary>
    /// Gets value of named <paramref name="expression"/> in this <see cref="ViewDataDictionary"/>.
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>Value of named <paramref name="expression"/> in this <see cref="ViewDataDictionary"/>.</returns>
    /// <remarks>
    /// Looks up <paramref name="expression"/> in the dictionary first. Falls back to evaluating it against
    /// <see cref="Model"/>.
    /// </remarks>
    public object? Eval(string? expression)
    {
        var info = GetViewDataInfo(expression);
        return info?.Value;
    }

    /// <summary>
    /// Gets value of named <paramref name="expression"/> in this <see cref="ViewDataDictionary"/>, formatted
    /// using given <paramref name="format"/>.
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <param name="format">
    /// The format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>).
    /// </param>
    /// <returns>
    /// Value of named <paramref name="expression"/> in this <see cref="ViewDataDictionary"/>, formatted using
    /// given <paramref name="format"/>.
    /// </returns>
    /// <remarks>
    /// Looks up <paramref name="expression"/> in the dictionary first. Falls back to evaluating it against
    /// <see cref="Model"/>.
    /// </remarks>
    public string? Eval(string? expression, string? format)
    {
        var value = Eval(expression);
        return FormatValue(value, format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Formats the given <paramref name="value"/> using the given <paramref name="format"/>.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="format">
    /// The format string (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>).
    /// </param>
    /// <returns>The formatted <see cref="string"/>.</returns>
    public static string? FormatValue(object? value, string? format)
        => FormatValue(value, format, CultureInfo.CurrentCulture);

    internal static string? FormatValue(object? value, string? format, IFormatProvider formatProvider)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(format))
        {
            return Convert.ToString(value, formatProvider);
        }
        else
        {
            return string.Format(formatProvider, format, value);
        }
    }

    /// <summary>
    /// Gets <see cref="ViewDataInfo"/> for named <paramref name="expression"/> in this
    /// <see cref="ViewDataDictionary"/>.
    /// </summary>
    /// <param name="expression">Expression name, relative to the current model.</param>
    /// <returns>
    /// <see cref="ViewDataInfo"/> for named <paramref name="expression"/> in this
    /// <see cref="ViewDataDictionary"/>.
    /// </returns>
    /// <remarks>
    /// Looks up <paramref name="expression"/> in the dictionary first. Falls back to evaluating it against
    /// <see cref="Model"/>.
    /// </remarks>
    public ViewDataInfo? GetViewDataInfo(string? expression)
    {
        return ViewDataEvaluator.Eval(this, expression);
    }

    /// <summary>
    /// Set <see cref="ModelExplorer"/> to ensure <see cref="Model"/> and <see cref="ModelExplorer.Model"/>
    /// reflect the new <paramref name="value"/>.
    /// </summary>
    /// <param name="value">New <see cref="Model"/> value.</param>
    protected virtual void SetModel(object? value)
    {
        // Update ModelExplorer to reflect the new value. When possible, preserve ModelMetadata to avoid losing
        // property information.
        var modelType = value?.GetType();
        if (ModelMetadata.MetadataKind == ModelMetadataKind.Type &&
            ModelMetadata.ModelType == typeof(object) &&
            modelType != null &&
            modelType != typeof(object))
        {
            // Base ModelMetadata on new type when there's no property information to preserve and type changes to
            // something besides typeof(object).
            ModelExplorer = _metadataProvider.GetModelExplorerForType(modelType, value);
        }
        else if (modelType != null && !ModelMetadata.ModelType.IsAssignableFrom(modelType))
        {
            // Base ModelMetadata on new type when new model is incompatible with the existing metadata. The most
            // common case is _declaredModelType==typeof(object), metadata was copied from another VDD, and user
            // code sets the Model to a new type e.g. within a view component or a view that lacks an @model
            // directive.
            ModelExplorer = _metadataProvider.GetModelExplorerForType(modelType, value);
        }
        else if (object.ReferenceEquals(value, Model))
        {
            // The metadata matches and the model is literally the same; usually nothing to do here.
            if (value == null &&
                !ModelMetadata.IsReferenceOrNullableType &&
                _declaredModelType != ModelMetadata.ModelType)
            {
                // Base ModelMetadata on declared type when setting Model to null, source VDD's Model was never
                // set, and source VDD had a non-Nullable value type. Though _declaredModelType might also be a
                // non-Nullable value type, would need to duplicate logic behind
                // ModelMetadata.IsReferenceOrNullableType to avoid this allocation in the error case.
                ModelExplorer = _metadataProvider.GetModelExplorerForType(_declaredModelType, value);
            }
        }
        else
        {
            // The existing metadata is compatible with the value but it's a new value.
            ModelExplorer = new ModelExplorer(_metadataProvider, ModelExplorer.Container, ModelMetadata, value);
        }

        EnsureCompatible(value);
    }

    // Throw if given value is incompatible with the declared Model Type.
    private void EnsureCompatible(object? value)
    {
        // IsCompatibleObject verifies if the value is either an instance of _declaredModelType or (if value is
        // null) that _declaredModelType is a nullable type.
        var castWillSucceed = IsCompatibleWithDeclaredType(value);
        if (!castWillSucceed)
        {
            string message;
            if (value == null)
            {
                message = Resources.FormatViewData_ModelCannotBeNull(_declaredModelType);
            }
            else
            {
                message = Resources.FormatViewData_WrongTModelType(value.GetType(), _declaredModelType);
            }

            throw new InvalidOperationException(message);
        }
    }

    // Call after updating the ModelExplorer because this uses both _declaredModelType and ModelMetadata. May
    // otherwise get incorrect compatibility errors.
    private bool IsCompatibleWithDeclaredType(object? value)
    {
        if (value == null)
        {
            // In this case ModelMetadata.ModelType matches _declaredModelType.
            return ModelMetadata.IsReferenceOrNullableType;
        }
        else
        {
            return _declaredModelType.IsAssignableFrom(value.GetType());
        }
    }

    #region IDictionary methods
    /// <inheritdoc />
    public void Add(string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        _data.Add(key, value);
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _data.ContainsKey(key);
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _data.Remove(key);
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _data.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<string, object?> item)
    {
        _data.Add(item);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _data.Clear();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<string, object?> item)
    {
        return _data.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        _data.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<string, object?> item)
    {
        return _data.Remove(item);
    }

    /// <inheritdoc />
    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        return _data.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _data.GetEnumerator();
    }
    #endregion
}
