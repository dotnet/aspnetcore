// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <summary>
    /// Encapsulates information that describes a <typeparamref name="TOption"/> option on <see cref="MvcOptions"/> .
    /// </summary>
    /// <typeparam name="TOption">The type of the option.</typeparam>
    public class OptionDescriptor<TOption> : IOptionDescriptor<TOption>
    {
        /// <summary>
        /// Creates a new instance of <see cref="OptionDescriptor{TOption}"/>.
        /// </summary>
        /// <param name="type">A type that represents <typeparamref name="TOption"/>.</param>
        public OptionDescriptor([NotNull] Type type)
        {
            var optionType = typeof(TOption);
            if (!optionType.IsAssignableFrom(type))
            {
                var message = Resources.FormatTypeMustDeriveFromType(type.FullName, optionType.FullName);
                throw new ArgumentException(message, "type");
            }

            OptionType = type;
        }

        /// <summary>
        /// Creates a new instance of <see cref="OptionDescriptor{TOption}"/> with the specified instance.
        /// </summary>
        /// <param name="option">An instance of <typeparamref name="TOption"/> that the descriptor represents.</param>
        public OptionDescriptor([NotNull] TOption option)
        {
            Instance = option;
            OptionType = option.GetType();
        }

        /// <summary>
        /// Gets the type of the <typeparamref name="TOption"/> described by this
        /// <see cref="OptionDescriptor{TOption}"/>.
        /// </summary>
        public Type OptionType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the instance of <typeparamref name="TOption"/> described by this
        /// <see cref="OptionDescriptor{TOption}"/>.
        /// </summary>
        public TOption Instance
        {
            get;
            private set;
        }
    }
}