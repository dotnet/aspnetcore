// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="BindingSources"/> which can represent multiple value-provider data sources.
    /// </summary>
    public class CompositeBindingSource : BindingSource
    {
        /// <summary>
        /// Creates a new <see cref="CompositeBindingSource"/>.
        /// </summary>
        /// <param name="bindingSources">
        /// The set of <see cref="BindingSource"/> entries.
        /// Must be value-provider sources and user input.
        /// </param>
        /// <param name="displayName">The display name for the composite source.</param>
        /// <returns>A <see cref="CompositeBindingSource"/>.</returns>
        public static CompositeBindingSource Create(
            IEnumerable<BindingSource> bindingSources,
            string displayName)
        {
            if (bindingSources == null)
            {
                throw new ArgumentNullException(nameof(bindingSources));
            }

            foreach (var bindingSource in bindingSources)
            {
                if (bindingSource.IsGreedy)
                {
                    var message = Resources.FormatBindingSource_CannotBeGreedy(
                        bindingSource.DisplayName,
                        nameof(CompositeBindingSource));
                    throw new ArgumentException(message, nameof(bindingSources));
                }

                if (!bindingSource.IsFromRequest)
                {
                    var message = Resources.FormatBindingSource_MustBeFromRequest(
                        bindingSource.DisplayName,
                        nameof(CompositeBindingSource));
                    throw new ArgumentException(message, nameof(bindingSources));
                }

                if (bindingSource is CompositeBindingSource)
                {
                    var message = Resources.FormatBindingSource_CannotBeComposite(
                        bindingSource.DisplayName,
                        nameof(CompositeBindingSource));
                    throw new ArgumentException(message, nameof(bindingSources));
                }
            }

            var id = string.Join("&", bindingSources.Select(s => s.Id).OrderBy(s => s, StringComparer.Ordinal));
            return new CompositeBindingSource(id, displayName, bindingSources);
        }

        private CompositeBindingSource(
            string id,
            string displayName,
            IEnumerable<BindingSource> bindingSources)
            : base(id, displayName, isGreedy: false, isFromRequest: true)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (bindingSources == null)
            {
                throw new ArgumentNullException(nameof(bindingSources));
            }

            BindingSources = bindingSources;
        }

        /// <summary>
        /// Gets the set of <see cref="BindingSource"/> entries.
        /// </summary>
        public IEnumerable<BindingSource> BindingSources { get; }

        /// <inheritdoc />
        public override bool CanAcceptDataFrom(BindingSource bindingSource)
        {
            if (bindingSource == null)
            {
                throw new ArgumentNullException(nameof(bindingSource));
            }

            if (bindingSource is CompositeBindingSource)
            {
                var message = Resources.FormatBindingSource_CannotBeComposite(
                    bindingSource.DisplayName,
                    nameof(CanAcceptDataFrom));
                throw new ArgumentException(message, nameof(bindingSource));
            }

            foreach (var source in BindingSources)
            {
                if (source.CanAcceptDataFrom(bindingSource))
                {
                    return true;
                }
            }

            return false;
        }
    }
}