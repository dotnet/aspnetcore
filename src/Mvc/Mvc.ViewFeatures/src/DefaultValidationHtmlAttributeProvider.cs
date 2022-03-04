// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Default implementation of <see cref="ValidationHtmlAttributeProvider"/>.
    /// </summary>
    public class DefaultValidationHtmlAttributeProvider : ValidationHtmlAttributeProvider
    {
        private readonly IModelMetadataProvider _metadataProvider;
        private readonly ClientValidatorCache _clientValidatorCache;
        private readonly IClientModelValidatorProvider _clientModelValidatorProvider;

        /// <summary>
        /// Initializes a new <see cref="DefaultValidationHtmlAttributeProvider"/> instance.
        /// </summary>
        /// <param name="optionsAccessor">The accessor for <see cref="MvcViewOptions"/>.</param>
        /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="clientValidatorCache">The <see cref="ClientValidatorCache"/> that provides
        /// a list of <see cref="IClientModelValidator"/>s.</param>
        public DefaultValidationHtmlAttributeProvider(
            IOptions<MvcViewOptions> optionsAccessor,
            IModelMetadataProvider metadataProvider,
            ClientValidatorCache clientValidatorCache)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            if (clientValidatorCache == null)
            {
                throw new ArgumentNullException(nameof(clientValidatorCache));
            }

            _clientValidatorCache = clientValidatorCache;
            _metadataProvider = metadataProvider;

            var clientValidatorProviders = optionsAccessor.Value.ClientModelValidatorProviders;
            _clientModelValidatorProvider = new CompositeClientModelValidatorProvider(clientValidatorProviders);
        }

        /// <inheritdoc />
        public override void AddValidationAttributes(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            IDictionary<string, string> attributes)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            if (modelExplorer == null)
            {
                throw new ArgumentNullException(nameof(modelExplorer));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            var formContext = viewContext.ClientValidationEnabled ? viewContext.FormContext : null;
            if (formContext == null)
            {
                return;
            }

            var validators = _clientValidatorCache.GetValidators(
                modelExplorer.Metadata,
                _clientModelValidatorProvider);
            if (validators.Count > 0)
            {
                var validationContext = new ClientModelValidationContext(
                    viewContext,
                    modelExplorer.Metadata,
                    _metadataProvider,
                    attributes);

                for (var i = 0; i < validators.Count; i++)
                {
                    var validator = validators[i];
                    validator.AddValidation(validationContext);
                }
            }
        }
    }
}
