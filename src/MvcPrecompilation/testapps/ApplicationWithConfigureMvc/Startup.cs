using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApplicationWithConfigureStartup
{
    public class Startup : IDesignTimeMvcBuilderConfiguration
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
            // Add framework services.
            var builder = services.AddMvc();
            ConfigureMvc(builder);
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public void ConfigureMvc(IMvcBuilder builder)
        {
            builder.AddRazorOptions(options =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var callback = options.CompilationCallback;
                options.CompilationCallback = context =>
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    callback(context);
                    foreach (var tree in context.Compilation.SyntaxTrees)
                    {
                        var rewrittenRoot = new RazorRewriter().Visit(tree.GetRoot());
                        var rewrittenTree = tree.WithRootAndOptions(rewrittenRoot, tree.Options);
                        context.Compilation = context.Compilation.ReplaceSyntaxTree(tree, rewrittenTree);
                    }
                };
            });
        }
    }
}
