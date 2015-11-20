// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
#if DOTNET5_4
using System.Reflection;
#endif
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.OptionsModel;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// An implementation of <see cref="IModelValidatorProvider"/> which provides validators
    /// for attributes which derive from <see cref="ValidationAttribute"/>. It also provides
    /// a validator for types which implement <see cref="IValidatableObject"/>.
    /// </summary>
    public class DataAnnotationsModelValidatorProvider : IModelValidatorProvider
    {
        private readonly IOptions<MvcDataAnnotationsLocalizationOptions> _options;
        private readonly IStringLocalizerFactory _stringLocalizerFactory;

        /// <summary>
        /// Create a new instance of <see cref="DataAnnotationsModelValidatorProvider"/>.
        /// </summary>
        /// <param name="options">The <see cref="IOptions{MvcDataAnnotationsLocalizationOptions}"/>.</param>
        /// <param name="stringLocalizerFactory">The <see cref="IStringLocalizerFactory"/>.</param>
        public DataAnnotationsModelValidatorProvider(
            IOptions<MvcDataAnnotationsLocalizationOptions> options,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            _options = options;
            _stringLocalizerFactory = stringLocalizerFactory;
        }

        public void GetValidators(ModelValidatorProviderContext context)
        {
            IStringLocalizer stringLocalizer = null;
            if (_stringLocalizerFactory != null && _options.Value.DataAnnotationLocalizerProvider != null)
            {
                stringLocalizer = _options.Value.DataAnnotationLocalizerProvider(
                    context.ModelMetadata.ContainerType ?? context.ModelMetadata.ModelType,
                    _stringLocalizerFactory);
            }

            for (var i = 0; i < context.ValidatorMetadata.Count; i++)
            {
                var attribute = context.ValidatorMetadata[i] as ValidationAttribute;
                if (attribute == null)
                {
                    continue;
                }

                var validator = new DataAnnotationsModelValidator(attribute, stringLocalizer);

                // Inserts validators based on whether or not they are 'required'. We want to run
                // 'required' validators first so that we get the best possible error message.
                if (attribute is RequiredAttribute)
                {
                    context.Validators.Insert(0, validator);
                }
                else
                {
                    context.Validators.Add(validator);
                }
            }

            // Produce a validator if the type supports IValidatableObject
            if (typeof(IValidatableObject).IsAssignableFrom(context.ModelMetadata.ModelType))
            {
                context.Validators.Add(new ValidatableObjectAdapter());
            }
        }
    }
}
