// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class ObjectVisitor
    {
        private readonly IAdapterFactory _adapterFactory;
        private readonly IContractResolver _contractResolver;
        private readonly ParsedPath _path;

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectVisitor"/>.
        /// </summary>
        /// <param name="path">The path of the JsonPatch operation</param>
        /// <param name="contractResolver">The <see cref="IContractResolver"/>.</param>
        public ObjectVisitor(ParsedPath path, IContractResolver contractResolver)
            :this(path, contractResolver, new AdapterFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectVisitor"/>.
        /// </summary>
        /// <param name="path">The path of the JsonPatch operation</param>
        /// <param name="contractResolver">The <see cref="IContractResolver"/>.</param>
        /// <param name="adapterFactory">The <see cref="IAdapterFactory"/> to use when creating adaptors.</param>
        public ObjectVisitor(ParsedPath path, IContractResolver contractResolver, IAdapterFactory adapterFactory)
        {
            _path = path;
            _contractResolver = contractResolver ?? throw new ArgumentNullException(nameof(contractResolver));
            _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
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

                // If we hit a null on an interior segment then we need to stop traversing.
                if (next == null)
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
            return _adapterFactory.Create(targetObject, _contractResolver);
        }
    }
}
