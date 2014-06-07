using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var engine = new Microsoft.AspNet.Server.Kestrel.KestrelEngine();
            engine.Start(1);
            using (var server = engine.CreateServer(App))
            {
                Console.WriteLine("Hello World");
                Console.ReadLine();
            }
            engine.Stop();
        }



        private static async Task App(object arg)
        {
            var httpContext = new Microsoft.AspNet.PipelineCore.DefaultHttpContext(
                new Microsoft.AspNet.FeatureModel.FeatureCollection(
                    new Microsoft.AspNet.FeatureModel.FeatureObject(arg)));

            Console.WriteLine("{0} {1}{2}{3}",
                httpContext.Request.Method,
                httpContext.Request.PathBase,
                httpContext.Request.Path,
                httpContext.Request.QueryString);

            httpContext.Response.ContentType = "text/plain";
            await httpContext.Response.WriteAsync("Hello world");
        }
    }
}
