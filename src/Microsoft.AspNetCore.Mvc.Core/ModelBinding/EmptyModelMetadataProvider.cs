// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class EmptyModelMetadataProvider : DefaultModelMetadataProvider
    {
        public EmptyModelMetadataProvider()
            : base(new DefaultCompositeMetadataDetailsProvider(new IMetadataDetailsProvider[]
            {
                new MessageOnlyBindingProvider()
            }))
        {
        }

        private class MessageOnlyBindingProvider : IBindingMetadataProvider
        {
            private readonly ModelBindingMessageProvider _messageProvider = CreateMessageProvider();

            public void CreateBindingMetadata(BindingMetadataProviderContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                // Don't bother with ModelBindingMessageProvider copy constructor. No other provider can change the
                // delegates.
                context.BindingMetadata.ModelBindingMessageProvider = _messageProvider;
            }

            private static ModelBindingMessageProvider CreateMessageProvider()
            {
                return new ModelBindingMessageProvider
                {
                    MissingBindRequiredValueAccessor = Resources.FormatModelBinding_MissingBindRequiredMember,
                    MissingKeyOrValueAccessor = Resources.FormatKeyValuePair_BothKeyAndValueMustBePresent,
                    ValueMustNotBeNullAccessor = Resources.FormatModelBinding_NullValueNotValid,
                    AttemptedValueIsInvalidAccessor = Resources.FormatModelState_AttemptedValueIsInvalid,
                    UnknownValueIsInvalidAccessor = Resources.FormatModelState_UnknownValueIsInvalid,
                    ValueIsInvalidAccessor = Resources.FormatHtmlGeneration_ValueIsInvalid,
                    ValueMustBeANumberAccessor = Resources.FormatHtmlGeneration_ValueMustBeNumber,
                };
            }
        }
    }
}