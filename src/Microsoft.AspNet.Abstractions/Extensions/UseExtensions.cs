using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet
{
    public static class UseExtensions
    {
        /// <summary>
        /// Use middleware defined in-line.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="middleware">A function that handles the request or calls the given next function.</param>
        /// <returns></returns>
        public static IBuilder Use(this IBuilder app, Func<HttpContext, Func<Task>, Task> middleware)
        {
            return app.Use(next =>
            {
                return context =>
                {
                    Func<Task> simpleNext = () => next(context);
                    return middleware(context, simpleNext);
                };
            });
        }
    }
}