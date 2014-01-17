using System;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.CoreServices
{
    public class CompilerCache
    {
        private readonly MemoryCache _cache;

        public CompilerCache()
        {
            _cache = MemoryCache.Default;
        }

        public Task<CompilationResult> GetOrAdd(IFileInfo file, Func<Task<CompilationResult>> compile)
        {
            // Generate a content id
            string contentId = file.PhysicalPath + '|' + file.LastModified.Ticks;

            var cachedType = _cache[contentId] as Type;
            if (cachedType == null)
            {
                return CompileWith(contentId, file, compile);
            }

            return Task.FromResult(CompilationResult.Successful(generatedCode: null, type: cachedType));
        }

        private async Task<CompilationResult> CompileWith(string contentId, IFileInfo file, Func<Task<CompilationResult>> compile)
        {
            CompilationResult result = await compile();
            Type compiledType = result.CompiledType;

            var filePaths = new [] { file.PhysicalPath };
            var policy = new CacheItemPolicy();
            policy.ChangeMonitors.Add(new HostFileChangeMonitor(filePaths));

            _cache.Set(contentId, result.CompiledType, policy);
            return result;
        }
    }
}
