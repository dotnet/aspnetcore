using AspNetCoreSdkTests.Templates;
using AspNetCoreSdkTests.Util;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace AspNetCoreSdkTests
{
    [TestFixture]
    public class TemplateTests
    {
        private static readonly TimeSpan _sleepBetweenHttpRequests = TimeSpan.FromMilliseconds(100);

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler()
        {
            // Allow self-signed certs
            ServerCertificateCustomValidationCallback = (m, c, ch, p) => true
        });

        [Test]
        [TestCaseSource(typeof(TemplateData), nameof(TemplateData.Current))]
        public void Restore(Template template, NuGetConfig nuGetConfig)
        {
            using (var context = new DotNetContext())
            {
                context.New(template);
                context.Restore(nuGetConfig);

                CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterRestore, context.GetObjFiles());
            }
        }

        [Test]
        [TestCaseSource(typeof(TemplateData), nameof(TemplateData.Current))]
        public void Build(Template template, NuGetConfig nuGetConfig)
        {
            using (var context = new DotNetContext())
            {
                context.New(template);
                context.Restore(nuGetConfig);
                context.Build();

                CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterBuild, context.GetObjFiles());
                CollectionAssert.AreEquivalent(template.ExpectedBinFilesAfterBuild, context.GetBinFiles());
            }
        }

        [Test]
        [TestCaseSource(typeof(TemplateData), nameof(TemplateData.CurrentWebApplications))]
        public void Run(Template template, NuGetConfig nuGetConfig)
        {
            using (var context = new DotNetContext())
            {
                context.New(template);
                context.Restore(nuGetConfig);
                var (httpUrl, httpsUrl) = context.Run();

                Assert.AreEqual(HttpStatusCode.OK, GetAsync(new Uri(new Uri(httpUrl), template.RelativeUrl)).StatusCode);
                Assert.AreEqual(HttpStatusCode.OK, GetAsync(new Uri(new Uri(httpsUrl), template.RelativeUrl)).StatusCode);
            }
        }

        [Test]
        [TestCaseSource(typeof(TemplateData), nameof(TemplateData.Current))]
        public void Publish(Template template, NuGetConfig nuGetConfig)
        {
            using (var context = new DotNetContext())
            {
                context.New(template);
                context.Restore(nuGetConfig);
                context.Publish();

                CollectionAssert.AreEquivalent(template.ExpectedFilesAfterPublish, context.GetPublishFiles());
            }
        }

        private HttpResponseMessage GetAsync(Uri requestUri)
        {
            while (true)
            {
                try
                {
                    return _httpClient.GetAsync(requestUri).Result;
                }
                catch
                {
                    Thread.Sleep(_sleepBetweenHttpRequests);
                }
            }
        }
    }
}
