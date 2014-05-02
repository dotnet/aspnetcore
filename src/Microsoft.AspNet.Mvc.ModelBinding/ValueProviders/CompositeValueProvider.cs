// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "It is more fundamentally a value provider than a collection")]
    public class CompositeValueProvider : Collection<IValueProvider>, IEnumerableValueProvider
    {
        public CompositeValueProvider() 
            : base()
        {
        }

        public CompositeValueProvider(IEnumerable<IValueProvider> valueProviders)
            : base(valueProviders.ToList())
        {
        }

        public virtual async Task<bool> ContainsPrefixAsync(string prefix)
        {
            for (int i = 0; i < Count; i++)
            {
                if (await this[i].ContainsPrefixAsync(prefix))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual async Task<ValueProviderResult> GetValueAsync(string key)
        {
            // Performance-sensitive
            // Caching the count is faster for IList<T>
            int itemCount = Items.Count;
            for (int i = 0; i < itemCount; i++)
            {
                IValueProvider vp = Items[i];
                ValueProviderResult result = await vp.GetValueAsync(key);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        public virtual IDictionary<string, string> GetKeysFromPrefix(string prefix)
        {
            foreach (IValueProvider vp in this)
            {
                IDictionary<string, string> result = GetKeysFromPrefixFromProvider(vp, prefix);
                if (result != null && result.Count > 0)
                {
                    return result;
                }
            }
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        internal static IDictionary<string, string> GetKeysFromPrefixFromProvider(IValueProvider provider, string prefix)
        {
            IEnumerableValueProvider enumeratedProvider = provider as IEnumerableValueProvider;
            return (enumeratedProvider != null) ? enumeratedProvider.GetKeysFromPrefix(prefix) : null;
        }

        protected override void InsertItem(int index, [NotNull] IValueProvider item)
        {
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, [NotNull] IValueProvider item)
        {
            base.SetItem(index, item);
        }
    }
}
