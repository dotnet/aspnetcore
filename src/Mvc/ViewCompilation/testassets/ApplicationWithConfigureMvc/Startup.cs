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
            // Add framework services.
            var builder = services.AddMvc();
            ConfigureMvc(builder);
        }

        public void Configure(IApplicationBuilder app, ILoggingBuilder builder)
        {
            builder.AddConsole();
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
                var callback = options.CompilationCallback;
                options.CompilationCallback = context =>
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
