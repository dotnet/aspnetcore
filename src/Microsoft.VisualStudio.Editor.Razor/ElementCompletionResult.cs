// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public abstract class ElementCompletionResult
    {
        private ElementCompletionResult()
        {
        }

        public abstract IReadOnlyDictionary<string, IEnumerable<TagHelperDescriptor>> Completions { get; }

        internal static ElementCompletionResult Create(Dictionary<string, HashSet<TagHelperDescriptor>> completions)
        {
            var readonlyCompletions = completions.ToDictionary(
                key => key.Key,
                value => (IEnumerable<TagHelperDescriptor>)value.Value,
                completions.Comparer);
            var result = new DefaultElementCompletionResult(readonlyCompletions);

            return result;
        }

        private class DefaultElementCompletionResult : ElementCompletionResult
        {
            private readonly IReadOnlyDictionary<string, IEnumerable<TagHelperDescriptor>> _completions;

            public DefaultElementCompletionResult(IReadOnlyDictionary<string, IEnumerable<TagHelperDescriptor>> completions)
            {
                _completions = completions;
            }

            public override IReadOnlyDictionary<string, IEnumerable<TagHelperDescriptor>> Completions => _completions;
        }
    }
}
