using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Certificate.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) => null; // TODO: fix
            //WebHost.CreateDefaultBuilder(args)
            //    .UseStartup<Startup>()
            //    .ConfigureKestrel(options =>
            //    {
            //        options.Listen(IPAddress.Loopback, 5001, listenOptions =>
            //        {
            //            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            //            {
            //                ServerCertificate = FindHttpsCertificate(),
            //                ClientCertificateMode = ClientCertificateMode.RequireCertificate,
            //            });
            //        });
            //        options.Listen(IPAddress.Loopback, 5000);
            //    })
            //    .UseStartup<Startup>()
            //    .Build();
    }
}
