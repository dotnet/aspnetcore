// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Provides programmatic configuration for the MVC framework.
/// </summary>
public class MvcOptions : IEnumerable<ICompatibilitySwitch>
{
    internal const int DefaultMaxModelBindingCollectionSize = FormReader.DefaultValueCountLimit;
    internal const int DefaultMaxModelBindingRecursionDepth = 32;

    private readonly IReadOnlyList<ICompatibilitySwitch> _switches = Array.Empty<ICompatibilitySwitch>();

    private int _maxModelStateErrors = ModelStateDictionary.DefaultMaxAllowedErrors;
    private int _maxModelBindingCollectionSize = DefaultMaxModelBindingCollectionSize;
    private int _maxModelBindingRecursionDepth = DefaultMaxModelBindingRecursionDepth;
    private int? _maxValidationDepth = 32;

    /// <summary>
    /// Creates a new instance of <see cref="MvcOptions"/>.
    /// </summary>
    public MvcOptions()
    {
        CacheProfiles = new Dictionary<string, CacheProfile>(StringComparer.OrdinalIgnoreCase);
        Conventions = new List<IApplicationModelConvention>();
        Filters = new FilterCollection();
        FormatterMappings = new FormatterMappings();
        InputFormatters = new FormatterCollection<IInputFormatter>();
        OutputFormatters = new FormatterCollection<IOutputFormatter>();
        ModelBinderProviders = new List<IModelBinderProvider>();
        ModelBindingMessageProvider = new DefaultModelBindingMessageProvider();
        ModelMetadataDetailsProviders = new List<IMetadataDetailsProvider>();
        ModelValidatorProviders = new List<IModelValidatorProvider>();
        ValueProviderFactories = new List<IValueProviderFactory>();
    }

    /// <summary>
    /// Gets or sets a value that determines if routing should use endpoints internally, or if legacy routing
    /// logic should be used. Endpoint routing is used to match HTTP requests to MVC actions, and to generate
    /// URLs with <see cref="IUrlHelper"/>.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool EnableEndpointRouting { get; set; } = true;

    /// <summary>
    /// Gets or sets the flag which decides whether body model binding (for example, on an
    /// action method parameter with <see cref="FromBodyAttribute"/>) should treat empty
    /// input as valid. <see langword="false"/> by default.
    /// </summary>
    /// <example>
    /// When <see langword="false"/>, actions that model bind the request body (for example,
    /// using <see cref="FromBodyAttribute"/>) will register an error in the
    /// <see cref="ModelStateDictionary"/> if the incoming request body is empty.
    /// </example>
    public bool AllowEmptyInputInBodyModelBinding { get; set; }

    /// <summary>
    /// Gets a Dictionary of CacheProfile Names, <see cref="CacheProfile"/> which are pre-defined settings for
    /// response caching.
    /// </summary>
    public IDictionary<string, CacheProfile> CacheProfiles { get; }

    /// <summary>
    /// Gets a list of <see cref="IApplicationModelConvention"/> instances that will be applied to
    /// the <see cref="ApplicationModel"/> when discovering actions.
    /// </summary>
    public IList<IApplicationModelConvention> Conventions { get; }

    /// <summary>
    /// Gets a collection of <see cref="IFilterMetadata"/> which are used to construct filters that
    /// apply to all actions.
    /// </summary>
    public FilterCollection Filters { get; }

    /// <summary>
    /// Used to specify mapping between the URL Format and corresponding media type.
    /// </summary>
    public FormatterMappings FormatterMappings { get; }

    /// <summary>
    /// Gets a list of <see cref="IInputFormatter"/>s that are used by this application.
    /// </summary>
    public FormatterCollection<IInputFormatter> InputFormatters { get; }

    /// <summary>
    /// Gets or sets a value that determines if the inference of <see cref="RequiredAttribute"/> for
    /// properties and parameters of non-nullable reference types is suppressed. If <c>false</c>
    /// (the default), then all non-nullable reference types will behave as-if <c>[Required]</c> has
    /// been applied. If <c>true</c>, this behavior will be suppressed; nullable reference types and
    /// non-nullable reference types will behave the same for the purposes of validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option controls whether MVC model binding and validation treats nullable and non-nullable
    /// reference types differently.
    /// </para>
    /// <para>
    /// By default, MVC will treat a non-nullable reference type parameters and properties as-if
    /// <c>[Required]</c> has been applied, resulting in validation errors when no value was bound.
    /// </para>
    /// <para>
    /// MVC does not support non-nullable reference type annotations on type arguments and type parameter
    /// constraints. The framework will not infer any validation attributes for generic-typed properties
    /// or collection elements.
    /// </para>
    /// </remarks>
    public bool SuppressImplicitRequiredAttributeForNonNullableReferenceTypes { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if buffering is disabled for input formatters that
    /// synchronously read from the HTTP request body.
    /// </summary>
    public bool SuppressInputFormatterBuffering { get; set; }

    /// <summary>
    /// Gets or sets the flag that determines if buffering is disabled for output formatters that
    /// synchronously write to the HTTP response body.
    /// </summary>
    public bool SuppressOutputFormatterBuffering { get; set; }

    /// <summary>
    /// Gets or sets the flag that determines if MVC should use action invoker extensibility. This will allow
    /// custom <see cref="IActionInvokerFactory"/> and <see cref="IActionInvokerProvider"/> execute during the request pipeline.
    /// </summary>
    /// <remarks>This only applies when <see cref="EnableEndpointRouting"/> is true.</remarks>
    /// <value>Defaults to <see langword="false" /> indicating that action invokers are unused by default.</value>
    public bool EnableActionInvokers { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of validation errors that are allowed by this application before further
    /// errors are ignored.
    /// </summary>
    public int MaxModelValidationErrors
    {
        get => _maxModelStateErrors;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            _maxModelStateErrors = value;
        }
    }

    /// <summary>
    /// Gets a list of <see cref="IModelBinderProvider"/>s used by this application.
    /// </summary>
    public IList<IModelBinderProvider> ModelBinderProviders { get; }

    /// <summary>
    /// Gets the default <see cref="ModelBinding.Metadata.ModelBindingMessageProvider"/>. Changes here are copied to the
    /// <see cref="ModelMetadata.ModelBindingMessageProvider"/> property of all <see cref="ModelMetadata"/>
    /// instances unless overridden in a custom <see cref="IBindingMetadataProvider"/>.
    /// </summary>
    public DefaultModelBindingMessageProvider ModelBindingMessageProvider { get; }

    /// <summary>
    /// Gets a list of <see cref="IMetadataDetailsProvider"/> instances that will be used to
    /// create <see cref="ModelMetadata"/> instances.
    /// </summary>
    /// <remarks>
    /// A provider should implement one or more of the following interfaces, depending on what
    /// kind of details are provided:
    /// <ul>
    /// <li><see cref="IBindingMetadataProvider"/></li>
    /// <li><see cref="IDisplayMetadataProvider"/></li>
    /// <li><see cref="IValidationMetadataProvider"/></li>
    /// </ul>
    /// </remarks>
    public IList<IMetadataDetailsProvider> ModelMetadataDetailsProviders { get; }

    /// <summary>
    /// Gets a list of <see cref="IModelValidatorProvider"/>s used by this application.
    /// </summary>
    public IList<IModelValidatorProvider> ModelValidatorProviders { get; }

    /// <summary>
    /// Gets a list of <see cref="IOutputFormatter"/>s that are used by this application.
    /// </summary>
    public FormatterCollection<IOutputFormatter> OutputFormatters { get; }

    /// <summary>
    /// Gets or sets the flag which causes content negotiation to ignore Accept header
    /// when it contains the media type <c>*/*</c>. <see langword="false"/> by default.
    /// </summary>
    public bool RespectBrowserAcceptHeader { get; set; }

    /// <summary>
    /// Gets or sets the flag which decides whether an HTTP 406 Not Acceptable response
    /// will be returned if no formatter has been selected to format the response.
    /// <see langword="false"/> by default.
    /// </summary>
    public bool ReturnHttpNotAcceptable { get; set; }

    /// <summary>
    /// Gets a list of <see cref="IValueProviderFactory"/> used by this application.
    /// </summary>
    public IList<IValueProviderFactory> ValueProviderFactories { get; }

    /// <summary>
    /// Gets or sets the SSL port that is used by this application when <see cref="RequireHttpsAttribute"/>
    /// is used. If not set the port won't be specified in the secured URL e.g. https://localhost/path.
    /// </summary>
    public int? SslPort { get; set; }

    /// <summary>
    /// Gets or sets the default value for the Permanent property of <see cref="RequireHttpsAttribute"/>.
    /// </summary>
    public bool RequireHttpsPermanent { get; set; }

    /// <summary>
    /// Gets or sets the maximum depth to constrain the validation visitor when validating. Set to <see langword="null" />
    /// to disable this feature.
    /// <para>
    /// <see cref="ValidationVisitor"/> traverses the object graph of the model being validated. For models
    /// that are very deep or are infinitely recursive, validation may result in stack overflow.
    /// </para>
    /// <para>
    /// When not <see langword="null"/>, <see cref="ValidationVisitor"/> will throw if
    /// traversing an object exceeds the maximum allowed validation depth.
    /// </para>
    /// </summary>
    /// <value>
    /// The default value is <c>32</c>.
    /// </value>
    public int? MaxValidationDepth
    {
        get => _maxValidationDepth;
        set
        {
            if (value != null && value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _maxValidationDepth = value;
        }
    }

    /// <summary>
    /// Gets or sets a value that determines whether the validation visitor will perform validation of a complex type
    /// if validation fails for any of its children.
    /// <seealso cref="ValidationVisitor.ValidateComplexTypesIfChildValidationFails"/>
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool ValidateComplexTypesIfChildValidationFails { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if MVC will remove the suffix "Async" applied to
    /// controller action names.
    /// <para>
    /// <see cref="ControllerActionDescriptor.ActionName"/> is used to construct the route to the action as
    /// well as in view lookup. When <see langword="true"/>, MVC will trim the suffix "Async" applied
    /// to action method names.
    /// For example, the action name for <c>ProductsController.ListProductsAsync</c> will be
    /// canonicalized as <c>ListProducts.</c>. Consequently, it will be routeable at
    /// <c>/Products/ListProducts</c> with views looked up at <c>/Views/Products/ListProducts.cshtml</c>.
    /// </para>
    /// <para>
    /// This option does not affect values specified using <see cref="ActionNameAttribute"/>.
    /// </para>
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool SuppressAsyncSuffixInActionNames { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum size of a complex collection to model bind. When this limit is reached, the model
    /// binding system will throw an <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When binding a collection, some element binders may succeed unconditionally and model binding may run out
    /// of memory. This limit constrains such unbounded collection growth; it is a safeguard against incorrect
    /// model binders and models.
    /// </para>
    /// <para>
    /// This limit does not <em>correct</em> the bound model. The <see cref="InvalidOperationException"/> instead
    /// informs the developer of an issue in their model or model binder. The developer must correct that issue.
    /// </para>
    /// <para>
    /// This limit does not apply to collections of simple types. When
    /// <see cref="CollectionModelBinder{TElement}"/> relies entirely on <see cref="IValueProvider"/>s, it cannot
    /// create collections larger than the available data.
    /// </para>
    /// <para>
    /// A very high value for this option (<c>int.MaxValue</c> for example) effectively removes the limit and is
    /// not recommended.
    /// </para>
    /// </remarks>
    /// <value>The default value is <c>1024</c>, matching <see cref="FormReader.DefaultValueCountLimit"/>.</value>
    public int MaxModelBindingCollectionSize
    {
        get => _maxModelBindingCollectionSize;
        set
        {
            // Disallowing an empty collection would cause the CollectionModelBinder to throw unconditionally.
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

            _maxModelBindingCollectionSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum recursion depth of the model binding system. The
    /// <see cref="DefaultModelBindingContext"/> will throw an <see cref="InvalidOperationException"/> if more than
    /// this number of <see cref="IModelBinder"/>s are on the stack. That is, an attempt to recurse beyond this
    /// level will fail.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For some self-referential models, some binders may succeed unconditionally and model binding may result in
    /// stack overflow. This limit constrains such unbounded recursion; it is a safeguard against incorrect model
    /// binders and models. This limit also protects against very deep model type hierarchies lacking
    /// self-references.
    /// </para>
    /// <para>
    /// This limit does not <em>correct</em> the bound model. The <see cref="InvalidOperationException"/> instead
    /// informs the developer of an issue in their model. The developer must correct that issue.
    /// </para>
    /// <para>
    /// A very high value for this option (<c>int.MaxValue</c> for example) effectively removes the limit and is
    /// not recommended.
    /// </para>
    /// </remarks>
    /// <value>The default value is <c>32</c>, matching the default <see cref="MaxValidationDepth"/> value.</value>
    public int MaxModelBindingRecursionDepth
    {
        get => _maxModelBindingRecursionDepth;
        set
        {
            // Disallowing one model binder (if supported) would cause the model binding system to throw
            // unconditionally. DefaultModelBindingContext always allows a top-level binder i.e. its own creation.
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 1);

            _maxModelBindingRecursionDepth = value;
        }
    }

    /// <summary>
    /// Gets or sets the most number of entries of an <see cref="IAsyncEnumerable{T}"/> that
    /// that <see cref="ObjectResultExecutor"/> will buffer.
    /// <para>
    /// When <see cref="ObjectResult.Value" /> is an instance of <see cref="IAsyncEnumerable{T}"/>,
    /// <see cref="ObjectResultExecutor"/> will eagerly read the enumeration and add to a synchronous collection
    /// prior to invoking the selected formatter.
    /// This property determines the most number of entries that the executor is allowed to buffer.
    /// </para>
    /// </summary>
    /// <value>Defaults to <c>8192</c>.</value>
    public int MaxIAsyncEnumerableBufferLimit { get; set; } = 8192;

    IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator() => _switches.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
}
