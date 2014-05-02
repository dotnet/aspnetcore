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
            string contentId = file.PhysicalPath + '|' + file.LastModified.Ticks;

            Type compiledType;
            if (!_cache.TryGetValue(contentId, out compiledType))
            {
                CompilationResult result = compile();
                _cache.TryAdd(contentId, result.CompiledType);

                return result;
            }

            return CompilationResult.Successful(generatedCode: null, type: compiledType);
        }
    }
}
