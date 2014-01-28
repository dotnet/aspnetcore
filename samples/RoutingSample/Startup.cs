using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.HttpMethod;
using Microsoft.AspNet.Routing.Lambda;
using Microsoft.AspNet.Routing.Legacy;
using Microsoft.AspNet.Routing.Owin;
using Microsoft.AspNet.Routing.Template;
using Microsoft.AspNet.Routing.Tree;
using Owin;

namespace RoutingSample
{
    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configuring the router places it in the OWIN pipeline and retuns an IRouteBuilder which is used
            // To create routes - all routes are directed to nested pipelines.
            var router = app.UseRouter();


            // At the simplest level - we can support microframework style routing. 
            //
            // This route matches a prefix of the request path, and a specific HTTP method.
            router.Get("1/echo", async (context) => 
            {
                string url = (string)context["owin.RequestPath"];
                await WriteAsync(context, url);
            });


            // This route takes a lambda, and can apply arbitrary criteria for routing without needing to couple
            // to an object model.
            router.On(context => ((string)context["owin.RequestPath"]).StartsWith("2"), async (context) =>
            {
                string url = (string)context["owin.RequestPath"];
                await WriteAsync(context, url);
            });


            // The return value is an IRouteEndpoint - extension method friendly for adding more routes and different
            // route types. 
            //
            // All of these routes go to the same delegate.
            router.Get("3/Store", async (context) =>
            {
                string method = (string)context["owin.RequestMethod"];
                await WriteAsync(context, method);
            })
            .Post("3/Store/Checkout")
            .On(context => ((string)context["owin.RequestPath"]).StartsWith("3/api"));


            // Routing to a middleware using IAppBuilder -- allowing routing to a more complex pipeline.
            router.ForApp((builder) => builder.Use(async (context, next) =>
            {
                await context.Response.WriteAsync("Hello, World!");
            }))
            .Get("4/Hello")
            .Post("4/Hello/World");

            
            // Nested Router
            router.ForApp((builder) => 
            {
                var nested = builder.UseRouter();

                nested.Get(async (context) => await WriteAsync(context, "Get"));
                nested.Post(async (context) => await WriteAsync(context, "Post"));
            })
            .Get("5/Store");



            // MVC/WebAPI Stuff below - using 'tradition' template routes.


            // Routing with parameter capturing - the route data is stored in the owin context
            router.ForApp((builder) => builder.Use(async (context, next) =>
            {
                string controller = (string)context.Environment.GetRouteMatchValues()["controller"];
                await context.Response.WriteAsync(controller);

            }))
            .AddTemplateRoute("6/api/{controller}", new HttpRouteValueDictionary(new { controller = "Home" }));


            // Routing with data tokens - these are added to the context when a route matches
            // This technique can be used for MVC/Web API to perform action selection as part of routing
            router.ForApp((builder) => builder.Use(async (context, next) =>
            {
                string stringValue = (string)context.Environment["myapp_StringValue"];
                await context.Response.WriteAsync(stringValue);

            }))
            .AddTemplateRoute("7", null, null, data: new HttpRouteValueDictionary(new { myapp_StringValue = "cool" }));


            // The route engine can be provided as a parameter to the app builder function so that it can be
            // captured and used inside of the application.
            //
            // It's also provided as part of the owin context on a routed request for apps that would prefer
            // a stateless style.
            router.ForApp((builder, engine) => builder.Use(async (context, next) =>
            {
                if (Object.Equals(engine, context.Environment.GetRouteEngine()))
                {
                    await context.Response.WriteAsync(engine.GetType().ToString());
                }
                else
                {
                    await next();
                }
            }))
            .AddTemplateRoute("8");


            // Generating a link by name
            router.ForApp((builder, engine) => builder.Use(async (context, next) =>
            {
                string url = engine.GetUrl("ByName", context.Environment, new HttpRouteValueDictionary(new { })).Url;
                await context.Response.WriteAsync(url);
            }))
            .AddTemplateRoute("ByName", "9/{value1}", new HttpRouteValueDictionary(new { value1 = "Cool" }));


            // Tree Routing
            var tree = router.AddTreeRoute();
            tree.Path("10/api").Parameter("controller").Endpoint(router.ForApp((builder) => builder.Use(async (context, next) =>
            {
                string url = context.Request.Uri.PathAndQuery;
                await context.Response.WriteAsync(url);
            })));


            tree.Build();
        }

        private static Task WriteAsync(IDictionary<string, object> context, string value)
        {
            var response = (Stream)context["owin.ResponseBody"];

            var bytes = Encoding.UTF8.GetBytes(value);
            return response.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
