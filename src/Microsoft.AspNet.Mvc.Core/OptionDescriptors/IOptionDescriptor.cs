// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <summary>
    /// Encapsulates information that describes a <typeparamref name="TOption"/> option on <see cref="MvcOptions"/>.
    /// </summary>
    /// <typeparam name="TOption">The type of the option.</typeparam>
    public interface IOptionDescriptor<out TOption>
    {
        /// <summary>
        /// Gets the type of the <typeparamref name="TOption"/> described by this
        /// <see cref="IOptionDescriptor{TOption}"/>.
        /// </summary>
        Type OptionType { get; }

        /// <summary>
        /// Gets the instance of <typeparamref name="TOption"/> described by this
        /// <see cref="IOptionDescriptor{TOption}"/>.
        /// </summary>
        TOption Instance { get; }
    }
}