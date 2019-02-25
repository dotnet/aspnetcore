using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.HeaderPropagation;
using Microsoft.Extensions.Primitives;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HeaderPropagation.Tests
{
    public class HeaderPropagationTest
    {
        public HeaderPropagationTest()
        {
            Handler = new SimpleHandler();
            Configuration = new HeaderPropagationEntry
            {
                InputName = "in",
                OutputName = "out",
            };

            ServiceCollection = new ServiceCollection();

            ContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            };
            ServiceCollection.AddSingleton<IHttpContextAccessor>(ContextAccessor);

            HttpClientBuilder = ServiceCollection.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com"))
                .ConfigureHttpMessageHandlerBuilder(b =>
                {
                    b.PrimaryHandler = Handler;
                });
        }

        private SimpleHandler Handler { get; }
        private HeaderPropagationEntry Configuration { get; }
        public ServiceCollection ServiceCollection { get; }
        public HttpContextAccessor ContextAccessor { get; }
        public IHttpClientBuilder HttpClientBuilder { get; }

        [Fact]
        public async Task AddHeaderPropagation_HeaderInRequest_AddCorrectValue()
        {
            // Arrange
            ContextAccessor.HttpContext.Request.Headers.Add("in", "test");

            HttpClientBuilder.AddHeaderPropagation(o => o.Headers.Add(Configuration));
            var services = ServiceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("example.com");

            // Act
            var response = await client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.True(Handler.Headers.Contains("out"));
            Assert.Equal(new[] { "test" }, Handler.Headers.GetValues("out"));
        }

        [Fact]
        public async Task AddHeaderPropagation_NoHeaderInRequest_DoesNotAddIt()
        {
            // Arrange
            HttpClientBuilder.AddHeaderPropagation(o => o.Headers.Add(Configuration));
            var services = ServiceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("example.com");

            // Act
            var response = await client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.Empty(Handler.Headers);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task AddHeaderPropagation_HeaderEmptyInRequest_DoNotAddIt(string headerValue)
        {
            // Arrange
            ContextAccessor.HttpContext.Request.Headers.Add("in", headerValue);

            HttpClientBuilder.AddHeaderPropagation(o => o.Headers.Add(Configuration));
            var services = ServiceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("example.com");

            // Act
            var response = await client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.False(Handler.Headers.Contains("out"));
        }

        [Theory]
        [InlineData(false, "", new[] { "" })]
        [InlineData(false, null, new[] { "" })]
        [InlineData(false, "42", new[] { "42" })]
        [InlineData(true, "42", new[] { "42", "test" })]
        [InlineData(true, "", new[] { "", "test" })]
        [InlineData(true, null, new[] { "", "test" })]
        public async Task AddHeaderPropagation_HeaderInRequest_HeaderAlreadyInOutgoingRequest(bool alwaysAdd, string outgoingValue, string[] expectedValues)
        {
            // Arrange
            ContextAccessor.HttpContext.Request.Headers.Add("in", "test");
            Configuration.AlwaysAdd = alwaysAdd;

            HttpClientBuilder.AddHeaderPropagation(o => o.Headers.Add(Configuration));
            var services = ServiceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("example.com");

            HttpRequestMessage request = new HttpRequestMessage();
            request.Headers.Add("out", outgoingValue);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.True(Handler.Headers.Contains("out"));
            Assert.Equal(expectedValues, Handler.Headers.GetValues("out"));
        }

        [Theory]
        [InlineData(new[] { "default" }, new[] { "default" })]
        [InlineData(new[] { "default", "other" }, new[] { "default", "other" })]
        public async Task AddHeaderPropagation_NoHeaderInRequest_AddDefaultValue(string[] defaultValues, string[] expectedValues)
        {
            // Arrange
            Configuration.DefaultValues = defaultValues;

            HttpClientBuilder.AddHeaderPropagation(o => o.Headers.Add(Configuration));
            var services = ServiceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("example.com");

            // Act
            var response = await client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.True(Handler.Headers.Contains("out"));
            Assert.Equal(expectedValues, Handler.Headers.GetValues("out"));
        }

        [Theory]
        [InlineData(new[] { "default" }, new[] { "default" })]
        [InlineData(new[] { "default", "other" }, new[] { "default", "other" })]
        public async Task AddHeaderPropagation_NoHeaderInRequest_UseDefaultValuesGenerator(string[] defaultValues, string[] expectedValues)
        {
            // Arrange
            HttpRequestMessage receivedRequest = null;
            HttpContext receivedContext = null;
            Configuration.DefaultValues = "no";
            Configuration.DefaultValuesGenerator = (req, context) =>
            {
                receivedRequest = req;
                receivedContext = context;
                return defaultValues;
            };

            HttpClientBuilder.AddHeaderPropagation(o => o.Headers.Add(Configuration));
            var services = ServiceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("example.com");
            var request = new HttpRequestMessage();

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.True(Handler.Headers.Contains("out"));
            Assert.Equal(expectedValues, Handler.Headers.GetValues("out"));
            Assert.Same(request, receivedRequest);
            Assert.Same(ContextAccessor.HttpContext, receivedContext);
        }

        [Fact]
        public async Task AddHeaderPropagation_NoHeaderInRequest_EmptyDefaultValuesGenerated_DoNotAddit()
        {
            // Arrange
            Configuration.DefaultValuesGenerator = (req, context) => StringValues.Empty;

            HttpClientBuilder.AddHeaderPropagation(o => o.Headers.Add(Configuration));
            var services = ServiceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("example.com");

            // Act
            var response = await client.SendAsync(new HttpRequestMessage());

            // Assert
            Assert.False(Handler.Headers.Contains("out"));
        }

        private class SimpleHandler : DelegatingHandler
        {
            public HttpHeaders Headers { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Headers = request.Headers;
                return Task.FromResult(new HttpResponseMessage());
            }
        }
    }
}
