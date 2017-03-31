// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Read / write <see cref="IModelBindingMessageProvider"/> implementation.
    /// </summary>
    public class ModelBindingMessageProvider : IModelBindingMessageProvider
    {
        private Func<string, string> _missingBindRequiredValueAccessor;
        private Func<string> _missingKeyOrValueAccessor;
        private Func<string> _missingRequestBodyRequiredValueAccessor;
        private Func<string, string> _valueMustNotBeNullAccessor;
        private Func<string, string, string> _attemptedValueIsInvalidAccessor;
        private Func<string, string> _unknownValueIsInvalidAccessor;
        private Func<string, string> _valueIsInvalidAccessor;
        private Func<string, string> _valueMustBeANumberAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBindingMessageProvider"/> class.
        /// </summary>
        public ModelBindingMessageProvider()
        {
            MissingBindRequiredValueAccessor = Resources.FormatModelBinding_MissingBindRequiredMember;
            MissingKeyOrValueAccessor = Resources.FormatKeyValuePair_BothKeyAndValueMustBePresent;
            MissingRequestBodyRequiredValueAccessor = Resources.FormatModelBinding_MissingRequestBodyRequiredMember;
            ValueMustNotBeNullAccessor = Resources.FormatModelBinding_NullValueNotValid;
            AttemptedValueIsInvalidAccessor = Resources.FormatModelState_AttemptedValueIsInvalid;
            UnknownValueIsInvalidAccessor = Resources.FormatModelState_UnknownValueIsInvalid;
            ValueIsInvalidAccessor = Resources.FormatHtmlGeneration_ValueIsInvalid;
            ValueMustBeANumberAccessor = Resources.FormatHtmlGeneration_ValueMustBeNumber;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBindingMessageProvider"/> class based on
        /// <paramref name="originalProvider"/>.
        /// </summary>
        /// <param name="originalProvider">The <see cref="ModelBindingMessageProvider"/> to duplicate.</param>
        public ModelBindingMessageProvider(ModelBindingMessageProvider originalProvider)
        {
            if (originalProvider == null)
            {
                throw new ArgumentNullException(nameof(originalProvider));
            }

            MissingBindRequiredValueAccessor = originalProvider.MissingBindRequiredValueAccessor;
            MissingKeyOrValueAccessor = originalProvider.MissingKeyOrValueAccessor;
            MissingRequestBodyRequiredValueAccessor = originalProvider.MissingRequestBodyRequiredValueAccessor;
            ValueMustNotBeNullAccessor = originalProvider.ValueMustNotBeNullAccessor;
            AttemptedValueIsInvalidAccessor = originalProvider.AttemptedValueIsInvalidAccessor;
            UnknownValueIsInvalidAccessor = originalProvider.UnknownValueIsInvalidAccessor;
            ValueIsInvalidAccessor = originalProvider.ValueIsInvalidAccessor;
            ValueMustBeANumberAccessor = originalProvider.ValueMustBeANumberAccessor;
        }

        /// <inheritdoc/>
        public Func<string, string> MissingBindRequiredValueAccessor
        {
            get
            {
                return _missingBindRequiredValueAccessor;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _missingBindRequiredValueAccessor = value;
            }
        }

        /// <inheritdoc/>
        public Func<string> MissingKeyOrValueAccessor
        {
            get
            {
                return _missingKeyOrValueAccessor;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _missingKeyOrValueAccessor = value;
            }
        }

        /// <inheritdoc/>
        public Func<string> MissingRequestBodyRequiredValueAccessor
        {
            get
            {
                return _missingRequestBodyRequiredValueAccessor;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _missingRequestBodyRequiredValueAccessor = value;
            }
        }

        /// <inheritdoc/>
        public Func<string, string> ValueMustNotBeNullAccessor
        {
            get
            {
                return _valueMustNotBeNullAccessor;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _valueMustNotBeNullAccessor = value;
            }
        }

        /// <inheritdoc/>
        public Func<string, string, string> AttemptedValueIsInvalidAccessor
        {
            get
            {
                return _attemptedValueIsInvalidAccessor;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _attemptedValueIsInvalidAccessor = value;
            }
        }

        /// <inheritdoc/>
        public Func<string, string> UnknownValueIsInvalidAccessor
        {
            get
            {
                return _unknownValueIsInvalidAccessor;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _unknownValueIsInvalidAccessor = value;
            }
        }

        /// <inheritdoc/>
        public Func<string, string> ValueIsInvalidAccessor
        {
            get
            {
                return _valueIsInvalidAccessor;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _valueIsInvalidAccessor = value;
            }
        }

        /// <inheritdoc/>
        public Func<string, string> ValueMustBeANumberAccessor
        {
            get
            {
                return _valueMustBeANumberAccessor;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _valueMustBeANumberAccessor = value;
            }
        }
    }
}
