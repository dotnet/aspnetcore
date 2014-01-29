using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class CompilerCache
    {
        private readonly ConcurrentDictionary<string, Type> _cache;

        public CompilerCache()
        {
            _cache = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<CompilationResult> GetOrAdd(IFileInfo file, Func<Task<CompilationResult>> compile)
        {
            // Generate a content id
            string contentId = file.PhysicalPath + '|' + file.LastModified.Ticks;

            Type compiledType;
            if (!_cache.TryGetValue(contentId, out compiledType))
            {
                CompilationResult result = await compile();
                _cache.TryAdd(contentId, result.CompiledType);

                return result;
            }

            return CompilationResult.Successful(generatedCode: null, type: compiledType);
        }
    }
}
