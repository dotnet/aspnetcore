// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.AspNet.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class CompilerCache
    {
        private readonly ConcurrentDictionary<string, Type> _cache;

        public CompilerCache()
        {
            _cache = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        }

        public CompilationResult GetOrAdd(IFileInfo file, Func<CompilationResult> compile)
        {
            // Generate a content id
            var contentId = file.PhysicalPath + '|' + file.LastModified.Ticks;

            Type compiledType;
            if (!_cache.TryGetValue(contentId, out compiledType))
            {
                var result = compile();
                _cache.TryAdd(contentId, result.CompiledType);

                return result;
            }

            return CompilationResult.Successful(compiledType);
        }
    }
}
