// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations.Internal
{
    public class FileExtensionsAttributeAdapter : AttributeAdapterBase<FileExtensionsAttribute>
    {
        private readonly string _extensions;
        private readonly string _formattedExtensions;

        public FileExtensionsAttributeAdapter(FileExtensionsAttribute attribute, IStringLocalizer stringLocalizer)
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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-fileextensions", GetErrorMessage(context));
            MergeAttribute(context.Attributes, "data-val-fileextensions-extensions", _extensions);
        }

        /// <inheritdoc />
        public override string GetErrorMessage(ModelValidationContextBase validationContext)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }

            return GetErrorMessage(
                validationContext.ModelMetadata,
                validationContext.ModelMetadata.GetDisplayName(),
                _formattedExtensions);
        }
    }
}
