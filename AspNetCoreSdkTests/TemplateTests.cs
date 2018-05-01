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
        public void RestoreBuildRunPublish(Template template, NuGetConfig nuGetConfig)
        {
            using (var context = new DotNetContext(template))
            {
                context.New();

                context.Restore(nuGetConfig);
                CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterRestore, context.GetObjFiles());

                context.Build();
                CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterBuild, context.GetObjFiles());
                CollectionAssert.AreEquivalent(template.ExpectedBinFilesAfterBuild, context.GetBinFiles());

                if (template.Type == TemplateType.WebApplication)
                {
                    var (httpUrl, httpsUrl) = context.Run();
                    Assert.AreEqual(HttpStatusCode.OK, GetAsync(new Uri(new Uri(httpUrl), template.RelativeUrl)).StatusCode);
                    Assert.AreEqual(HttpStatusCode.OK, GetAsync(new Uri(new Uri(httpsUrl), template.RelativeUrl)).StatusCode);
                }

                context.Publish();
                CollectionAssert.AreEquivalent(template.ExpectedFilesAfterPublish, context.GetPublishFiles());

                if (template.Type == TemplateType.WebApplication)
                {
                    var (httpUrl, httpsUrl) = context.Exec();
                    Assert.AreEqual(HttpStatusCode.OK, GetAsync(new Uri(new Uri(httpUrl), template.RelativeUrl)).StatusCode);
                    Assert.AreEqual(HttpStatusCode.OK, GetAsync(new Uri(new Uri(httpsUrl), template.RelativeUrl)).StatusCode);
                }
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
