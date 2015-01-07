// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents a model binder which understands <see cref="IFormatterBinderMetadata"/> and uses
    /// InputFomatters to bind the model to request's body.
    /// </summary>
    public class BodyModelBinder : MetadataAwareBinder<IFormatterBinderMetadata>
    {
        private readonly ActionContext _actionContext;
        private readonly IInputFormatterSelector _formatterSelector;
        private readonly IBodyModelValidator _bodyModelValidator;
        private readonly IValidationExcludeFiltersProvider _bodyValidationExcludeFiltersProvider;

        public BodyModelBinder([NotNull] IContextAccessor<ActionContext> context,
                               [NotNull] IInputFormatterSelector selector,
                               [NotNull] IBodyModelValidator bodyModelValidator,
                               [NotNull] IValidationExcludeFiltersProvider bodyValidationExcludeFiltersProvider)
        {
            _actionContext = context.Value;
            _formatterSelector = selector;
            _bodyModelValidator = bodyModelValidator;
            _bodyValidationExcludeFiltersProvider = bodyValidationExcludeFiltersProvider;
        }

        protected override async Task<bool> BindAsync(
            ModelBindingContext bindingContext,
            IFormatterBinderMetadata metadata)
        {
            var formatterContext = new InputFormatterContext(_actionContext, bindingContext.ModelType);
            var formatter = _formatterSelector.SelectFormatter(formatterContext);

            if (formatter == null)
            {
                var unsupportedContentType = Resources.FormatUnsupportedContentType(
                    bindingContext.OperationBindingContext.HttpContext.Request.ContentType);
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, unsupportedContentType);

                // Should always return true so that the model binding process ends here.
                return true;
            }

            bindingContext.Model = await formatter.ReadAsync(formatterContext);

            // Validate the deserialized object
            var validationContext = new ModelValidationContext(
                bindingContext.OperationBindingContext.MetadataProvider,
                bindingContext.OperationBindingContext.ValidatorProvider,
                bindingContext.ModelState,
                bindingContext.ModelMetadata,
                containerMetadata: null,
                excludeFromValidationFilters: _bodyValidationExcludeFiltersProvider.ExcludeFilters);
            _bodyModelValidator.Validate(validationContext, bindingContext.ModelName);
            return true;
        }
    }
}
