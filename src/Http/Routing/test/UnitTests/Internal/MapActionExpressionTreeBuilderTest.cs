// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Internal
{
    public class MapActionExpressionTreeBuilderTest
    {
        [Fact]
        public async Task RequestDelegateInvokesAction()
        {
            var invoked = false;
            void TestAction()
            {
                invoked = true;
            };

            var requestDelegate = MapActionExpressionTreeBuilder.BuildRequestDelegate((Action)TestAction);

            await requestDelegate(null!);

            Assert.True(invoked);
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromBodyParameter()
        {
            Todo originalTodo = new()
            {
                Name = "Write more tests!"
            };

            Todo? deserializedRequestBody = null;

            void TestAction([FromBody] Todo todo)
            {
                deserializedRequestBody = todo;
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/json";

            var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(originalTodo);
            httpContext.Request.Body = new MemoryStream(requestBodyBytes);

            var requestDelegate = MapActionExpressionTreeBuilder.BuildRequestDelegate((Action<Todo>)TestAction);

            await requestDelegate(httpContext);

            Assert.NotNull(deserializedRequestBody);
            Assert.Equal(originalTodo.Name, deserializedRequestBody!.Name);
        }

        [Fact]
        public async Task RequestDelegateWritesComplexReturnValueAsJsonResponseBody()
        {
            Todo originalTodo = new()
            {
                Name = "Write even more tests!"
            };

            Todo TestAction() => originalTodo;

            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            var requestDelegate = MapActionExpressionTreeBuilder.BuildRequestDelegate((Func<Todo>)TestAction);

            await requestDelegate(httpContext);

            var deserializedResponseBody = JsonSerializer.Deserialize<Todo>(responseBodyStream.ToArray(), new JsonSerializerOptions
            {
                // TODO: the output is "{\"id\":0,\"name\":\"Write even more tests!\",\"isComplete\":false}"
                // Verify that the camelCased property names are consistent with MVC and if so whether we should keep the behavior.
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(deserializedResponseBody);
            Assert.Equal(originalTodo.Name, deserializedResponseBody!.Name);
        }

        [Fact]
        public async Task RequestDelegateUsesCustomIResult()
        {
            var resultString = "Still not enough tests!";

            CustomResult TestAction() => new(resultString!);

            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            var requestDelegate = MapActionExpressionTreeBuilder.BuildRequestDelegate((Func<CustomResult>)TestAction);

            await requestDelegate(httpContext);

            var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());

            Assert.Equal(resultString, decodedResponseBody);
        }

        private class Todo
        {
            public int Id { get; set; }
            public string? Name { get; set; } = "Todo";
            public bool IsComplete { get; set; }
        }

        private class FromBodyAttribute : Attribute, IFromBodyMetadata
        {
        }

        private class CustomResult : IResult
        {
            private readonly string _resultString;

            public CustomResult(string resultString)
            {
                _resultString = resultString;
            }

            public Task ExecuteAsync(HttpContext httpContext)
            {
                return httpContext.Response.WriteAsync(_resultString);
            }
        }
    }
}
