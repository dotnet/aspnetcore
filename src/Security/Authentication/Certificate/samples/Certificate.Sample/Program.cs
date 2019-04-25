using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace idunno.Authentication.Certificate.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 5001, listenOptions =>
                    {
                        listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                        {
                            ServerCertificate = FindHttpsCertificate(),
                            ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                            ClientCertificateValidation = CertificateValidator.DisableChannelValidation
                        });
                    });
                    options.Listen(IPAddress.Loopback, 5000);
                })
                .Build();

        private static X509Certificate2 FindHttpsCertificate()
        {
            // Let's look for a localhost HTTPS server with the Server EKU and just "borrow it".
            List<X509Certificate2> possibleServerCertificates = new List<X509Certificate2>();

            using (var machineStore = new X509Store(StoreLocation.LocalMachine))
            {
                machineStore.Open(OpenFlags.ReadOnly);
                var localhostCertificates = machineStore.Certificates.Find(X509FindType.FindBySubjectName, "localhost", false);
                foreach (var certificate in localhostCertificates)
                {
                    if (certificate.Version >= 3)
                    {
                        List<X509EnhancedKeyUsageExtension> ekuExtensions = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().ToList();
                        if (!ekuExtensions.Any())
                        {
                            possibleServerCertificates.Add(certificate);
                        }
                        else
                        {
                            foreach (var extension in ekuExtensions)
                            {
                                foreach (var oid in extension.EnhancedKeyUsages)
                                {
                                    if (oid.Value.Equals("1.3.6.1.5.5.7.3.1", StringComparison.Ordinal))
                                    {
                                        possibleServerCertificates.Add(certificate);
                                    }
                                }
                            }
                        }
                    }
                }
                machineStore.Close();

            }

            if (possibleServerCertificates.Count == 0)
            {
                throw new Exception("Cannot find a suitable localhost HTTPS certificate");
            }

            Console.WriteLine("Using localhost certificate with thumbprint " + possibleServerCertificates[0].Thumbprint);

            return possibleServerCertificates[0];
        }
    }
}
