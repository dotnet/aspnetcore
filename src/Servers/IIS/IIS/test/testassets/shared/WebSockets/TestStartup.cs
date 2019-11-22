using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.IISIntegration.FunctionalTests
{
    public static class TestStartup
    {
        public static void Register(IApplicationBuilder app, object startup)
        {
            var type = startup.GetType();
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var parameters = method.GetParameters();
                if (method.Name != "Configure" &&
                    parameters.Length == 1)
                {
                    Action<IApplicationBuilder> appfunc = null;
                    if (parameters[0].ParameterType == typeof(IApplicationBuilder))
                    {
                        appfunc = innerAppBuilder => method.Invoke(startup, new[] { innerAppBuilder });
                    }
                    else if (parameters[0].ParameterType == typeof(HttpContext))
                    {
                        appfunc = innerAppBuilder => innerAppBuilder.Run(ctx => (Task)method.Invoke(startup, new[] { ctx }));
                    }

                    if (appfunc != null)
                    {
                        app.Map("/" + method.Name, appfunc);
                    }
                }
            }
        }
    }
}
