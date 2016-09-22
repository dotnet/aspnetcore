// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Dynamic;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class ObjectVisitor
    {
        private readonly IContractResolver _contractResolver;
        private readonly ParsedPath _path;

        public ObjectVisitor(ParsedPath path, IContractResolver contractResolver)
        {
            if (contractResolver == null)
            {
                throw new ArgumentNullException(nameof(contractResolver));
            }

            _path = path;
            _contractResolver = contractResolver;
        }

        public bool TryVisit(ref object target, out IAdapter adapter, out string errorMessage)
        {
            if (target == null)
            {
                adapter = null;
                errorMessage = null;
                return false;
            }

            adapter = SelectAdapater(target);

            // Traverse until the penultimate segment to get the target object and adapter
            for (var i = 0; i < _path.Segments.Count - 1; i++)
            {
                object next;
                if (!adapter.TryTraverse(target, _path.Segments[i], _contractResolver, out next, out errorMessage))
                {
                    adapter = null;
                    return false;
                }

                target = next;
                adapter = SelectAdapater(target);
            }

            errorMessage = null;
            return true;
        }

        private IAdapter SelectAdapater(object targetObject)
        {
            if (targetObject is ExpandoObject)
            {
                return new ExpandoObjectAdapter();
            }
            else if (targetObject is IDictionary)
            {
                return new DictionaryAdapter();
            }
            else if (targetObject is IList)
            {
                return new ListAdapter();
            }
            else
            {
                return new PocoAdapter();
            }
        }
    }
}
