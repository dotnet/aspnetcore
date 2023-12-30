// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

internal sealed class FileExtensionsAttributeAdapter : AttributeAdapterBase<FileExtensionsAttribute>
{
    private readonly string _extensions;
    private readonly string _formattedExtensions;

    public FileExtensionsAttributeAdapter(FileExtensionsAttribute attribute, IStringLocalizer? stringLocalizer)
        : base(attribute, stringLocalizer)
    {
        // Build the extension list based on how the JQuery Validation's 'extension' method expects it
        // https://jqueryvalidation.org/extension-method/

        // These lines follow the same approach as the FileExtensionsAttribute.
        var normalizedExtensions = Attribute.Extensions.Replace(" ", string.Empty).Replace(".", string.Empty).ToLowerInvariant();
        var parsedExtensions = normalizedExtensions.Split(',').Select(e => "." + e);
        _formattedExtensions = string.Join(", ", parsedExtensions);
        _extensions = string.Join(",", parsedExtensions);
    }

    /// <inheritdoc />
    public override void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-fileextensions", GetErrorMessage(context));
        MergeAttribute(context.Attributes, "data-val-fileextensions-extensions", _extensions);
    }

    /// <inheritdoc />
    public override string GetErrorMessage(ModelValidationContextBase validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        return GetErrorMessage(
            validationContext.ModelMetadata,
            validationContext.ModelMetadata.GetDisplayName(),
            _formattedExtensions);
    }
}
