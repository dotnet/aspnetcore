
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BasicLinkedApp
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<HelloWorldMiddleware>();
        }
    }

    public class HelloWorldMiddleware
    {
        public HelloWorldMiddleware(RequestDelegate next)
        {

        }
        
        public Task InvokeAsync(HttpContext context)
        {
            return context.Response.WriteAsync("Hello World");
        }
    }
}
