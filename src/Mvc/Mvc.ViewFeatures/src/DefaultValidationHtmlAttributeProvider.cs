// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

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
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        ArgumentNullException.ThrowIfNull(metadataProvider);
        ArgumentNullException.ThrowIfNull(clientValidatorCache);

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
        ArgumentNullException.ThrowIfNull(viewContext);
        ArgumentNullException.ThrowIfNull(modelExplorer);
        ArgumentNullException.ThrowIfNull(attributes);

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
