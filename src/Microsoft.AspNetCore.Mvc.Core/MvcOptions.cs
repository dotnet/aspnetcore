// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for the MVC framework.
    /// </summary>
    public class MvcOptions : IEnumerable<ICompatibilitySwitch>
    {
        private int _maxModelStateErrors = ModelStateDictionary.DefaultMaxAllowedErrors;

        // See CompatibilitySwitch.cs for guide on how to implement these.
        private readonly CompatibilitySwitch<bool> _allowBindingHeaderValuesToNonStringModelTypes;
        private readonly CompatibilitySwitch<bool> _allowCombiningAuthorizeFilters;
        private readonly CompatibilitySwitch<InputFormatterExceptionPolicy> _inputFormatterExceptionPolicy;
        private readonly CompatibilitySwitch<bool> _suppressBindingUndefinedValueToEnumType;
        private readonly ICompatibilitySwitch[] _switches;

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

            _allowCombiningAuthorizeFilters = new CompatibilitySwitch<bool>(nameof(AllowCombiningAuthorizeFilters));
            _allowBindingHeaderValuesToNonStringModelTypes = new CompatibilitySwitch<bool>(nameof(AllowBindingHeaderValuesToNonStringModelTypes));
            _inputFormatterExceptionPolicy = new CompatibilitySwitch<InputFormatterExceptionPolicy>(nameof(InputFormatterExceptionPolicy), InputFormatterExceptionPolicy.AllExceptions);
            _suppressBindingUndefinedValueToEnumType = new CompatibilitySwitch<bool>(nameof(SuppressBindingUndefinedValueToEnumType));

            _switches = new ICompatibilitySwitch[]
            {
                _allowCombiningAuthorizeFilters,
                _allowBindingHeaderValuesToNonStringModelTypes,
                _inputFormatterExceptionPolicy,
                _suppressBindingUndefinedValueToEnumType,
            };
        }

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
        /// Gets or sets a value that determines if policies on instances of <see cref="AuthorizeFilter" />
        /// will be combined into a single effective policy. The default value of the property is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Authorization policies are designed such that multiple authorization policies applied to an endpoint
        /// should be combined and executed a single policy. The <see cref="AuthorizeFilter"/> (commonly applied
        /// by <see cref="AuthorizeAttribute"/>) can be applied globally, to controllers, and to actions - which
        /// specifies multiple authorization policies for an action. In all ASP.NET Core releases prior to 2.1
        /// these multiple policies would not combine as intended. This compatibility switch configures whether the
        /// old (unintended) behavior or the new combining behavior will be used when multiple authorization policies
        /// are applied.
        /// </para>
        /// <para>
        /// This property is associated with a compatibility switch and can provide a different behavior depending on 
        /// the configured compatibility version for the application. See <see cref="CompatibilityVersion"/> for 
        /// guidance and examples of setting the application's compatibility version.
        /// </para>
        /// <para>
        /// Configuring the desired value of the compatibility switch by calling this property's setter will take precedence
        /// over the value implied by the application's <see cref="CompatibilityVersion"/>.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_0"/> then
        /// this setting will have the value <c>false</c> unless explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_1"/> or
        /// higher then this setting will have the value <c>true</c> unless explicitly configured.
        /// </para>
        /// </remarks>
        public bool AllowCombiningAuthorizeFilters
        {
            get => _allowCombiningAuthorizeFilters.Value;
            set => _allowCombiningAuthorizeFilters.Value = value;
        }

        /// <summary>
        /// Gets or sets a value that determines if <see cref="HeaderModelBinder"/> should bind to types other than
        /// <see cref="String"/> or a collection of <see cref="String"/>. If set to <c>true</c>,
        /// <see cref="HeaderModelBinder"/> would bind to simple types (like <see cref="String"/>, <see cref="Int32"/>,
        /// <see cref="Enum"/>, <see cref="Boolean"/> etc.) or a collection of simple types. The default value of the
        /// property is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is associated with a compatibility switch and can provide a different behavior depending on 
        /// the configured compatibility version for the application. See <see cref="CompatibilityVersion"/> for 
        /// guidance and examples of setting the application's compatibility version.
        /// </para>
        /// <para>
        /// Configuring the desired value of the compatibility switch by calling this property's setter will take precedence
        /// over the value implied by the application's <see cref="CompatibilityVersion"/>.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_0"/> then
        /// this setting will have the value <c>false</c> unless explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_1"/> or
        /// higher then this setting will have the value <c>true</c> unless explicitly configured.
        /// </para>
        /// </remarks>
        public bool AllowBindingHeaderValuesToNonStringModelTypes
        {
            get => _allowBindingHeaderValuesToNonStringModelTypes.Value;
            set => _allowBindingHeaderValuesToNonStringModelTypes.Value = value;
        }

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
        /// Gets or sets a value which determines how the model binding system interprets exceptions thrown by an <see cref="IInputFormatter"/>.
        /// The default value of the property is <see cref="InputFormatterExceptionPolicy.AllExceptions"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is associated with a compatibility switch and can provide a different behavior depending on 
        /// the configured compatibility version for the application. See <see cref="CompatibilityVersion"/> for 
        /// guidance and examples of setting the application's compatibility version.
        /// </para>
        /// <para>
        /// Configuring the desired value of the compatibility switch by calling this property's setter will take precedence
        /// over the value implied by the application's <see cref="CompatibilityVersion"/>.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_0"/> then
        /// this setting will have the value <see cref="InputFormatterExceptionPolicy.AllExceptions"/> unless
        /// explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_1"/> or
        /// higher then this setting will have the value
        /// <see cref="InputFormatterExceptionPolicy.MalformedInputExceptions"/> unless explicitly configured.
        /// </para>
        /// </remarks>
        public InputFormatterExceptionPolicy InputFormatterExceptionPolicy
        {
            get => _inputFormatterExceptionPolicy.Value;
            set => _inputFormatterExceptionPolicy.Value = value;
        }

        /// <summary>
        /// Gets a list of <see cref="IInputFormatter"/>s that are used by this application.
        /// </summary>
        public FormatterCollection<IInputFormatter> InputFormatters { get; }

        /// <summary>
        /// Gets or sets an value indicating whether the model binding system will bind undefined values to 
        /// enum types. The default value of the property is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is associated with a compatibility switch and can provide a different behavior depending on 
        /// the configured compatibility version for the application. See <see cref="CompatibilityVersion"/> for 
        /// guidance and examples of setting the application's compatibility version.
        /// </para>
        /// <para>
        /// Configuring the desired value of the compatibility switch by calling this property's setter will take precedence
        /// over the value implied by the application's <see cref="CompatibilityVersion"/>.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_0"/> then
        /// this setting will have the value <c>false</c> unless explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_1"/> or
        /// higher then this setting will have the value <c>true</c> unless explicitly configured.
        /// </para>
        /// </remarks>
        public bool SuppressBindingUndefinedValueToEnumType
        {
            get => _suppressBindingUndefinedValueToEnumType.Value;
            set => _suppressBindingUndefinedValueToEnumType.Value = value;
        }

        /// <summary>
        /// Gets or sets the flag to buffer the request body in input formatters. Default is <c>false</c>.
        /// </summary>
        public bool SuppressInputFormatterBuffering { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of validation errors that are allowed by this application before further
        /// errors are ignored.
        /// </summary>
        public int MaxModelValidationErrors
        {
            get => _maxModelStateErrors;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

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
        /// when it contains the media type */*. <see langword="false"/> by default.
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

        IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator()
        {
            return ((IEnumerable<ICompatibilitySwitch>)_switches).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
    }
}
