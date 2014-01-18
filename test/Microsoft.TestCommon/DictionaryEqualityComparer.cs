// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.TestCommon
{
    public class DictionaryEqualityComparer : IEqualityComparer<IDictionary<string, object>>
    {
        public bool Equals(IDictionary<string, object> x, IDictionary<string, object> y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }

            foreach (string key in x.Keys)
            {
                object xVal = x[key];
                object yVal;
                if (!y.TryGetValue(key, out yVal))
                {
                    return false;
                }

                if (xVal == null)
                {
                    if (yVal == null)
                    {
                        continue;
                    }

                    return false;
                }

                if (!xVal.Equals(yVal))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(IDictionary<string, object> obj)
        {
            return 1;
        }
    }
}
