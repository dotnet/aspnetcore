// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite
{
    internal class IISRewriteMapCollection : IEnumerable<IISRewriteMap>
    {
        private readonly Dictionary<string, IISRewriteMap> _rewriteMaps = new Dictionary<string, IISRewriteMap>();

        public void Add(IISRewriteMap rewriteMap)
        {
            if (rewriteMap != null)
            {
                _rewriteMaps[rewriteMap.Name] = rewriteMap;
            }
        }

        public int Count => _rewriteMaps.Count;

        public IISRewriteMap this[string key]
        {
            get
            {
                IISRewriteMap value;
                return _rewriteMaps.TryGetValue(key, out value) ? value : null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _rewriteMaps.Values.GetEnumerator();
        }

        public IEnumerator<IISRewriteMap> GetEnumerator()
        {
            return _rewriteMaps.Values.GetEnumerator();
        }
    }
}