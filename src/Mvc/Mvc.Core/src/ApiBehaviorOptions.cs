// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Options used to configure behavior for types annotated with <see cref="ApiControllerAttribute"/>.
    /// </summary>
    public class ApiBehaviorOptions : IEnumerable<ICompatibilitySwitch>
    {
        private readonly CompatibilitySwitch<bool> _suppressMapClientErrors;
        private readonly CompatibilitySwitch<bool> _suppressUseValidationProblemDetailsForInvalidModelStateResponses;
        private readonly CompatibilitySwitch<bool> _allowInferringBindingSourceForCollectionTypesAsFromQuery;
        private readonly ICompatibilitySwitch[] _switches;

        private Func<ActionContext, IActionResult> _invalidModelStateResponseFactory;

        /// <summary>
        /// Creates a new instance of <see cref="ApiBehaviorOptions"/>.
        /// </summary>
        public ApiBehaviorOptions()
        {
            _suppressMapClientErrors = new CompatibilitySwitch<bool>(nameof(SuppressMapClientErrors));
            _suppressUseValidationProblemDetailsForInvalidModelStateResponses = new CompatibilitySwitch<bool>(nameof(SuppressUseValidationProblemDetailsForInvalidModelStateResponses));
            _allowInferringBindingSourceForCollectionTypesAsFromQuery = new CompatibilitySwitch<bool>(nameof(AllowInferringBindingSourceForCollectionTypesAsFromQuery));
            _switches = new[]
            {
                _suppressMapClientErrors,
                _suppressUseValidationProblemDetailsForInvalidModelStateResponses,
                _allowInferringBindingSourceForCollectionTypesAsFromQuery
            };
        }

        /// <summary>
        /// Delegate invoked on actions annotated with <see cref="ApiControllerAttribute"/> to convert invalid
        /// <see cref="ModelStateDictionary"/> into an <see cref="IActionResult"/>
        /// </summary>
        public Func<ActionContext, IActionResult> InvalidModelStateResponseFactory
        {
            get => _invalidModelStateResponseFactory;
            set => _invalidModelStateResponseFactory = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets a value that determines if the filter that returns an <see cref="BadRequestObjectResult"/> when
        /// <see cref="ActionContext.ModelState"/> is invalid is suppressed. <seealso cref="InvalidModelStateResponseFactory"/>.
        /// </summary>
        public bool SuppressModelStateInvalidFilter { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if model binding sources are inferred for action parameters on controllers annotated
        /// with <see cref="ApiControllerAttribute"/> is suppressed.
        /// <para>
        /// When enabled, the following sources are inferred:
        /// Parameters that appear as route values, are assumed to be bound from the path (<see cref="BindingSource.Path"/>).
        /// Parameters of type <see cref="IFormFile"/> and <see cref="IFormFileCollection"/> are assumed to be bound from form.
        /// Parameters that are complex (<see cref="ModelMetadata.IsComplexType"/>) are assumed to be bound from the body (<see cref="BindingSource.Body"/>).
        /// All other parameters are assumed to be bound from the query.
        /// </para>
        /// </summary>
        public bool SuppressInferBindingSourcesForParameters { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if an <c>multipart/form-data</c> consumes action constraint is added to parameters
        /// that are bound from form data.
        /// </summary>
        public bool SuppressConsumesConstraintForFormFileParameters { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if controllers with <see cref="ApiControllerAttribute"/>
        /// transform certain certain client errors.
        /// <para>
        /// When <c>false</c>, a result filter is added to API controller actions that transforms <see cref="IClientErrorActionResult"/>.
        /// By default, <see cref="ClientErrorMapping"/> is used to map <see cref="IClientErrorActionResult"/> to a
        /// <see cref="ProblemDetails"/> instance (returned as the value for <see cref="ObjectResult"/>).
        /// </para>
        /// <para>
        /// To customize the output of the filter (for e.g. to return a different error type), register a custom
        /// implementation of of <see cref="IClientErrorFactory"/> in the service collection.
        /// </para>
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/> if the version is
        /// <see cref="CompatibilityVersion.Version_2_2"/> or later; <see langword="false"/> otherwise.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property is associated with a compatibility switch and can provide a different behavior depending on
        /// the configured compatibility version for the application. See <see cref="CompatibilityVersion"/> for
        /// guidance and examples of setting the application's compatibility version.
        /// </para>
        /// <para>
        /// Configuring the desired value of the compatibility switch by calling this property's setter will take
        /// precedence over the value implied by the application's <see cref="CompatibilityVersion"/>.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_1"/> or
        /// lower then this setting will have the value <see langword="false"/> unless explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_2"/> or
        /// higher then this setting will have the value <see langword="true"/> unless explicitly configured.
        /// </para>
        /// </remarks>
        public bool SuppressMapClientErrors
        {
            // Note: When compatibility switches are removed in 3.0, this property should be retained as a regular boolean property.
            get => _suppressMapClientErrors.Value;
            set => _suppressMapClientErrors.Value = value;
        }

        /// <summary>
        /// Gets or sets a value that determines if controllers annotated with <see cref="ApiControllerAttribute"/> respond using
        /// <see cref="ValidationProblemDetails"/> in <see cref="InvalidModelStateResponseFactory"/>.
        /// <para>
        /// When <see langword="true"/>, <see cref="SuppressModelStateInvalidFilter"/> returns errors in <see cref="ModelStateDictionary"/>
        /// as a <see cref="ValidationProblemDetails"/>. Otherwise, <see cref="SuppressModelStateInvalidFilter"/> returns the errors
        /// in the format determined by <see cref="SerializableError"/>.
        /// </para>
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/> if the version is
        /// <see cref="CompatibilityVersion.Version_2_2"/> or later; <see langword="false"/> otherwise.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property is associated with a compatibility switch and can provide a different behavior depending on
        /// the configured compatibility version for the application. See <see cref="CompatibilityVersion"/> for
        /// guidance and examples of setting the application's compatibility version.
        /// </para>
        /// <para>
        /// Configuring the desired value of the compatibility switch by calling this property's setter will take
        /// precedence over the value implied by the application's <see cref="CompatibilityVersion"/>.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_1"/> or
        /// lower then this setting will have the value <see langword="false"/> unless explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_2"/> or
        /// higher then this setting will have the value <see langword="true"/> unless explicitly configured.
        /// </para>
        /// </remarks>
        public bool SuppressUseValidationProblemDetailsForInvalidModelStateResponses
        {
            get => _suppressUseValidationProblemDetailsForInvalidModelStateResponses.Value;
            set => _suppressUseValidationProblemDetailsForInvalidModelStateResponses.Value = value;
        }

        /// <summary>
        /// Gets or sets a value that determines if <see cref="BindingInfo.BindingSource"/> for collection types
        /// (<see cref="ModelMetadata.IsCollectionType"/>).
        /// <para>
        /// When <see langword="true" />, the binding source for collection types is inferred as <see cref="BindingSource.Query"/>.
        /// Otherwise <see cref="BindingSource.Body"/> is inferred.
        /// </para>
        /// </summary>
        /// <value>
        /// The default value is <see langword="false"/> if the version is
        /// <see cref="CompatibilityVersion.Version_2_2"/> or later; <see langword="true"/> otherwise.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property is associated with a compatibility switch and can provide a different behavior depending on
        /// the configured compatibility version for the application. See <see cref="CompatibilityVersion"/> for
        /// guidance and examples of setting the application's compatibility version.
        /// </para>
        /// <para>
        /// Configuring the desired value of the compatibility switch by calling this property's setter takes
        /// precedence over the value implied by the application's <see cref="CompatibilityVersion"/>.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_1"/> or
        /// lower then this setting will have the value <see langword="true"/> unless explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_2"/> or
        /// higher then this setting will have the value <see langword="false"/> unless explicitly configured.
        /// </para>
        /// </remarks>
        public bool AllowInferringBindingSourceForCollectionTypesAsFromQuery
        {
            get => _allowInferringBindingSourceForCollectionTypesAsFromQuery.Value;
            set => _allowInferringBindingSourceForCollectionTypesAsFromQuery.Value = value;
        }

        /// <summary>
        /// Gets a map of HTTP status codes to <see cref="ClientErrorData"/>. Configured values
        /// are used to transform <see cref="IClientErrorActionResult"/> to an <see cref="ObjectResult"/>
        /// instance where the <see cref="ObjectResult.Value"/> is <see cref="ProblemDetails"/>.
        /// <para>
        /// Use of this feature can be disabled by resetting <see cref="SuppressMapClientErrors"/>.
        /// </para>
        /// </summary>
        public IDictionary<int, ClientErrorData> ClientErrorMapping { get; } =
            new Dictionary<int, ClientErrorData>();

        IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator()
        {
            return ((IEnumerable<ICompatibilitySwitch>)_switches).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
    }
}
