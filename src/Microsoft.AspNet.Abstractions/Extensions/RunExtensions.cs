using System;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet
{
    public static class RunExtensions
    {
        public static void Run([NotNull] this IBuilder app, [NotNull] RequestDelegate handler)
        {
            app.Use(_ => handler);
        }
    }
}