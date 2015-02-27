// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for the MVC framework.
    /// </summary>
    public class MvcOptions
    {
        private AntiForgeryOptions _antiForgeryOptions = new AntiForgeryOptions();
        private int _maxModelStateErrors = ModelStateDictionary.DefaultMaxAllowedErrors;

        public MvcOptions()
        {
            Conventions = new List<IApplicationModelConvention>();
            ModelBinders = new List<ModelBinderDescriptor>();
            ViewEngines = new List<ViewEngineDescriptor>();
            ValueProviderFactories = new List<ValueProviderFactoryDescriptor>();
            OutputFormatters = new List<OutputFormatterDescriptor>();
            InputFormatters = new List<InputFormatterDescriptor>();
            Filters = new List<IFilter>();
            FormatterMappings = new FormatterMappings();
            ValidationExcludeFilters = new List<ExcludeValidationDescriptor>();
            ModelMetadataDetailsProviders = new List<IMetadataDetailsProvider>();
            ModelValidatorProviders = new List<ModelValidatorProviderDescriptor>();
            CacheProfiles = new Dictionary<string, CacheProfile>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Provides programmatic configuration for the anti-forgery token system.
        /// </summary>
        public AntiForgeryOptions AntiForgeryOptions
        {
            get
            {
                return _antiForgeryOptions;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                                                    Resources.FormatPropertyOfTypeCannotBeNull("AntiForgeryOptions",
                                                                                               typeof(MvcOptions)));
                }

                _antiForgeryOptions = value;
            }
        }

        /// <summary>
        /// Used to specify mapping between the URL Format and corresponding <see cref="MediaTypeHeaderValue"/>.
        /// </summary>
        public FormatterMappings FormatterMappings { get; }

        /// <summary>
        /// Gets a list of <see cref="IFilter"/> which are used to construct filters that
        /// apply to all actions.
        /// </summary>
        public ICollection<IFilter> Filters { get; private set; }

        /// <summary>
        /// Gets a list of the <see cref="OutputFormatterDescriptor" /> which are used to construct
        /// a list of <see cref="IOutputFormatter"/> by <see cref="IOutputFormattersProvider"/>.
        /// </summary>
        public IList<OutputFormatterDescriptor> OutputFormatters { get; }

        /// <summary>
        /// Gets a list of the <see cref="InputFormatterDescriptor" /> which are used to construct
        /// a list of <see cref="IInputFormatter"/> by <see cref="IInputFormattersProvider"/>.
        /// </summary>
        public IList<InputFormatterDescriptor> InputFormatters { get; }

        /// <summary>
        /// Gets a list of <see cref="ExcludeValidationDescriptor"/> which are used to construct a list
        /// of exclude filters by <see cref="IValidationExcludeFiltersProvider"/>.
        /// </summary>
        public IList<ExcludeValidationDescriptor> ValidationExcludeFilters { get; }

        /// <summary>
        /// Gets or sets the maximum number of validation errors that are allowed by this application before further
        /// errors are ignored.
        /// </summary>
        public int MaxModelValidationErrors
        {
            get { return _maxModelStateErrors; }
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
        /// Get a list of the <see cref="ModelBinderDescriptor" /> used by the
        /// Gets a list of the <see cref="ModelBinderDescriptor" /> used by the
        /// <see cref="ModelBinding.CompositeModelBinder" />.
        /// </summary>
        public IList<ModelBinderDescriptor> ModelBinders { get; }

        /// <summary>
        /// Gets a list of the <see cref="ModelValidatorProviderDescriptor" />s used by
        /// <see cref="ModelBinding.CompositeModelValidatorProvider"/>.
        /// </summary>
        public IList<ModelValidatorProviderDescriptor> ModelValidatorProviders { get; }

        /// <summary>
        /// Gets a list of descriptors that represent <see cref="Rendering.IViewEngine"/> used
        /// by this application.
        /// </summary>
        public IList<ViewEngineDescriptor> ViewEngines { get; }

        /// <summary>
        /// Gets a list of descriptors that represent
        /// <see cref="ModelBinding.IValueProviderFactory"/> used by this application.
        /// </summary>
        public IList<ValueProviderFactoryDescriptor> ValueProviderFactories { get; }

        /// <summary>
        /// Gets a list of <see cref="IApplicationModelConvention"/> instances that will be applied to
        /// the <see cref="ApplicationModel"/> when discovering actions.
        /// </summary>
        public IList<IApplicationModelConvention> Conventions { get; }

        /// <summary>
        /// Gets or sets the flag which causes content negotiation to ignore Accept header 
        /// when it contains the media type */*. <see langword="false"/> by default.
        /// </summary>
        public bool RespectBrowserAcceptHeader { get; set; }

        /// <summary>
        /// Gets a Dictionary of CacheProfile Names, <see cref="CacheProfile"/> which are pre-defined settings for
        /// <see cref="ResponseCacheFilter"/>.
        /// </summary>
        public IDictionary<string, CacheProfile> CacheProfiles { get; }

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
    }
}