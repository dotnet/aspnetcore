using System;
using Microsoft.AspNet.Builder;

namespace DeveloperExceptionPageSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.Run(context =>
            {
                throw new Exception(string.Concat(
                    "Demonstration exception. The list:", "\r\n",
                    "New Line 1", "\n",
                    "New Line 2", Environment.NewLine,
                    "New Line 3"));
            });
        }
    }
}