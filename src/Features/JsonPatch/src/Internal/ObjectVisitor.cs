// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class ObjectVisitor
    {
        private readonly IContractResolver _contractResolver;
        private readonly ParsedPath _path;

        public ObjectVisitor(ParsedPath path, IContractResolver contractResolver)
        {
            _path = path;
            _contractResolver = contractResolver ?? throw new ArgumentNullException(nameof(contractResolver));
        }

        public bool TryVisit(ref object target, out IAdapter adapter, out string errorMessage)
        {
            if (target == null)
            {
                adapter = null;
                errorMessage = null;
                return false;
            }

            adapter = SelectAdapter(target);

            // Traverse until the penultimate segment to get the target object and adapter
            for (var i = 0; i < _path.Segments.Count - 1; i++)
            {
                if (!adapter.TryTraverse(target, _path.Segments[i], _contractResolver, out var next, out errorMessage))
                {
                    adapter = null;
                    return false;
                }

                target = next;
                adapter = SelectAdapter(target);
            }

            errorMessage = null;
            return true;
        }

        private IAdapter SelectAdapter(object targetObject)
        {
            var jsonContract = _contractResolver.ResolveContract(targetObject.GetType());

            if (targetObject is IList)
            {
                return new ListAdapter();
            }
            else if (jsonContract is JsonDictionaryContract jsonDictionaryContract)
            {
                var type = typeof(DictionaryAdapter<,>).MakeGenericType(jsonDictionaryContract.DictionaryKeyType, jsonDictionaryContract.DictionaryValueType);
                return (IAdapter)Activator.CreateInstance(type);
            }
            else if (jsonContract is JsonDynamicContract)
            {
                return new DynamicObjectAdapter();
            }
            else
            {
                return new PocoAdapter();
            }
        }
    }
}
