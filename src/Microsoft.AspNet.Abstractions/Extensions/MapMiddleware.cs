using System.Threading.Tasks;

namespace Microsoft.AspNet.Abstractions.Extensions
{
    public class MapMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MapOptions _options;

        public MapMiddleware([NotNull] RequestDelegate next, [NotNull] MapOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task Invoke([NotNull] HttpContext context)
        {
            PathString path = context.Request.Path;
            PathString remainingPath;
            if (path.StartsWithSegments(_options.PathMatch, out remainingPath))
            {
                // Update the path
                PathString pathBase = context.Request.PathBase;
                context.Request.PathBase = pathBase + _options.PathMatch;
                context.Request.Path = remainingPath;

                await _options.Branch(context);

                context.Request.PathBase = pathBase;
                context.Request.Path = path;
            }
            else
            {
                await _next(context);
            }
        }
    }
}