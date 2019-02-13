using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class AsyncActionsTests : IClassFixture<MvcTestFixture<BasicWebSite.Startup>>
    {
        public AsyncActionsTests(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task AsyncVoidAction_ReturnsOK()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/AsyncVoidAction");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, responseBody.Length);
        }

        [Fact]
        public async Task TaskAction_ReturnsOK()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/TaskAction");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, responseBody.Length);
        }

        [Fact]
        public async Task TaskExceptionAction_ReturnsCorrectError()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/TaskExceptionAction");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Action exception message: This is a custom exception.", responseBody);
        }

        [Fact]
        public async Task TaskOfObjectAction_ReturnsJsonFormattedObject()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/TaskOfObjectAction?message=Alpha");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"text\":\"Alpha\"}", responseBody);
        }

        [Fact]
        public async Task TaskOfObjectExceptionAction_ReturnsCorrectError()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/TaskOfObjectExceptionAction?message=Alpha");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Action exception message: This is a custom exception.", responseBody);
        }

        [Fact]
        public async Task TaskOfIActionResultAction_ReturnsString()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/TaskOfIActionResultAction?message=Beta");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Beta", responseBody);
        }

        [Fact]
        public async Task TaskOfIActionResultExceptionAction_ReturnsCorrectError()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/TaskOfIActionResultExceptionAction?message=Beta");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Action exception message: This is a custom exception.", responseBody);
        }

        [Fact]
        public async Task TaskOfContentResultAction_ReturnsString()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/TaskOfContentResultAction?message=Gamma");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Gamma", responseBody);
        }

        [Fact]
        public async Task TaskOfContentResultExceptionAction_ReturnsCorrectError()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/TaskOfContentResultExceptionAction?message=Gamma");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Action exception message: This is a custom exception.", responseBody);
        }

        [Fact]
        public async Task PreCompletedValueTaskOfObjectAction_ReturnsJsonFormattedObject()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/PreCompletedValueTaskOfObjectAction?message=Delta");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"text\":\"Delta\"}", responseBody);
        }

        [Fact]
        public async Task PreCompletedValueTaskOfObjectExceptionAction_ReturnsCorrectError()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/PreCompletedValueTaskOfObjectExceptionAction?message=Delta");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Action exception message: This is a custom exception.", responseBody);
        }

        [Fact]
        public async Task PreCompletedValueTaskOfIActionResultAction_ReturnsString()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/PreCompletedValueTaskOfIActionResultAction?message=Epsilon");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Epsilon", responseBody);
        }

        [Fact]
        public async Task PreCompletedValueTaskOfIActionResultExceptionAction_ReturnsCorrectError()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/PreCompletedValueTaskOfIActionResultExceptionAction?message=Epsilon");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Action exception message: This is a custom exception.", responseBody);
        }

        [Fact]
        public async Task PreCompletedValueTaskOfContentResultAction_ReturnsString()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/PreCompletedValueTaskOfContentResultAction?message=Zeta");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Zeta", responseBody);
        }

        [Fact]
        public async Task PreCompletedValueTaskOfContentResultExceptionAction_ReturnsCorrectError()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/PreCompletedValueTaskOfContentResultExceptionAction?message=Zeta");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Action exception message: This is a custom exception.", responseBody);
        }

        [Fact]
        public async Task CustomAwaitableVoidAction_ReturnsOK()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/CustomAwaitableVoidAction");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, responseBody.Length);
        }

        [Fact]
        public async Task CustomAwaitableVoidExceptionAction_ReturnsCorrectError()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/CustomAwaitableVoidExceptionAction");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Action exception message: This is a custom exception.", responseBody);
        }

        [Fact]
        public async Task CustomAwaitableOfObjectAction_ReturnsJsonFormattedObject()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/CustomAwaitableOfObjectAction?message=Eta");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"text\":\"Eta\"}", responseBody);
        }

        [Fact]
        public async Task CustomAwaitableOfObjectExceptionAction_ReturnsCorrectError()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/CustomAwaitableOfObjectExceptionAction?message=Eta");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Action exception message: This is a custom exception.", responseBody);
        }

        [Fact]
        public async Task CustomAwaitableOfIActionResultAction_ReturnsString()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/CustomAwaitableOfIActionResultAction?message=Theta");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Theta", responseBody);
        }

        [Fact]
        public async Task CustomAwaitableOfIActionResultExceptionAction_ReturnsCorrectError()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/CustomAwaitableOfIActionResultExceptionAction?message=Theta");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Action exception message: This is a custom exception.", responseBody);
        }

        [Fact]
        public async Task CustomAwaitableOfContentResultAction_ReturnsString()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/CustomAwaitableOfContentResultAction?message=Iota");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Iota", responseBody);
        }

        [Fact]
        public async Task CustomAwaitableOfContentResultExceptionAction_ReturnsCorrectError()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/AsyncActions/CustomAwaitableOfContentResultExceptionAction?message=Iota");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Action exception message: This is a custom exception.", responseBody);
        }
    }
}
