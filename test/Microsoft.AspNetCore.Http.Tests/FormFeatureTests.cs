// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace Microsoft.AspNetCore.Http.Features.Internal
{
    public class FormFeatureTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadFormAsync_SimpleData_ReturnsParsedFormCollection(bool bufferRequest)
        {
            var formContent = Encoding.UTF8.GetBytes("foo=bar&baz=2");
            var context = new DefaultHttpContext();
            var responseFeature = new FakeResponseFeature();
            context.Features.Set<IHttpResponseFeature>(responseFeature);
            context.Request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
            context.Request.Body = new NonSeekableReadStream(formContent);

            if (bufferRequest)
            {
                context.Request.EnableRewind();
            }

            // Not cached yet
            var formFeature = context.Features.Get<IFormFeature>();
            Assert.Null(formFeature);

            var formCollection = await context.Request.ReadFormAsync();

            Assert.Equal("bar", formCollection["foo"]);
            Assert.Equal("2", formCollection["baz"]);
            Assert.Equal(bufferRequest, context.Request.Body.CanSeek);
            if (bufferRequest)
            {
                Assert.Equal(0, context.Request.Body.Position);
            }

            // Cached
            formFeature = context.Features.Get<IFormFeature>();
            Assert.NotNull(formFeature);
            Assert.NotNull(formFeature.Form);
            Assert.Same(formFeature.Form, formCollection);

            // Cleanup
            await responseFeature.CompleteAsync();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadFormAsync_EmptyKeyAtEndAllowed(bool bufferRequest)
        {
            var formContent = Encoding.UTF8.GetBytes("=bar");
            Stream body = new MemoryStream(formContent);
            if (!bufferRequest)
            {
                body = new NonSeekableReadStream(body);
            }

            var formCollection = await FormReader.ReadFormAsync(body);

            Assert.Equal("bar", formCollection[""].FirstOrDefault());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadFormAsync_EmptyKeyWithAdditionalEntryAllowed(bool bufferRequest)
        {
            var formContent = Encoding.UTF8.GetBytes("=bar&baz=2");
            Stream body = new MemoryStream(formContent);
            if (!bufferRequest)
            {
                body = new NonSeekableReadStream(body);
            }

            var formCollection = await FormReader.ReadFormAsync(body);

            Assert.Equal("bar", formCollection[""].FirstOrDefault());
            Assert.Equal("2", formCollection["baz"].FirstOrDefault());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadFormAsync_EmptyValuedAtEndAllowed(bool bufferRequest)
        {
            // Arrange
            var formContent = Encoding.UTF8.GetBytes("foo=");
            Stream body = new MemoryStream(formContent);
            if (!bufferRequest)
            {
                body = new NonSeekableReadStream(body);
            }

            var formCollection = await FormReader.ReadFormAsync(body);

            // Assert
            Assert.Equal("", formCollection["foo"].FirstOrDefault());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadFormAsync_EmptyValuedWithAdditionalEntryAllowed(bool bufferRequest)
        {
            // Arrange
            var formContent = Encoding.UTF8.GetBytes("foo=&baz=2");
            Stream body = new MemoryStream(formContent);
            if (!bufferRequest)
            {
                body = new NonSeekableReadStream(body);
            }

            var formCollection = await FormReader.ReadFormAsync(body);

            // Assert
            Assert.Equal("", formCollection["foo"].FirstOrDefault());
            Assert.Equal("2", formCollection["baz"].FirstOrDefault());
        }

        private const string MultipartContentType = "multipart/form-data; boundary=WebKitFormBoundary5pDRpGheQXaM8k3T";
        private const string EmptyMultipartForm =
"--WebKitFormBoundary5pDRpGheQXaM8k3T--";
        // Note that CRLF (\r\n) is required. You can't use multi-line C# strings here because the line breaks on Linux are just LF.
        private const string MultipartFormWithField =
"--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: form-data; name=\"description\"\r\n" +
"\r\n" +
"Foo\r\n" +
"--WebKitFormBoundary5pDRpGheQXaM8k3T--";
        private const string MultipartFormWithFile =
"--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: form-data; name=\"myfile1\"; filename=\"temp.html\"\r\n" +
"Content-Type: text/html\r\n" +
"\r\n" +
"<html><body>Hello World</body></html>\r\n" +
"--WebKitFormBoundary5pDRpGheQXaM8k3T--";
        private const string MultipartFormWithFieldAndFile =
"--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: form-data; name=\"description\"\r\n" +
"\r\n" +
"Foo\r\n" +
"--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: form-data; name=\"myfile1\"; filename=\"temp.html\"\r\n" +
"Content-Type: text/html\r\n" +
"\r\n" +
"<html><body>Hello World</body></html>\r\n" +
"--WebKitFormBoundary5pDRpGheQXaM8k3T--";

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadForm_EmptyMultipart_ReturnsParsedFormCollection(bool bufferRequest)
        {
            var formContent = Encoding.UTF8.GetBytes(EmptyMultipartForm);
            var context = new DefaultHttpContext();
            var responseFeature = new FakeResponseFeature();
            context.Features.Set<IHttpResponseFeature>(responseFeature);
            context.Request.ContentType = MultipartContentType;
            context.Request.Body = new NonSeekableReadStream(formContent);

            if (bufferRequest)
            {
                context.Request.EnableRewind();
            }

            // Not cached yet
            var formFeature = context.Features.Get<IFormFeature>();
            Assert.Null(formFeature);

            var formCollection = context.Request.Form;

            Assert.NotNull(formCollection);

            // Cached
            formFeature = context.Features.Get<IFormFeature>();
            Assert.NotNull(formFeature);
            Assert.NotNull(formFeature.Form);
            Assert.Same(formCollection, formFeature.Form);
            Assert.Same(formCollection, await context.Request.ReadFormAsync());

            // Content
            Assert.Equal(0, formCollection.Count);
            Assert.NotNull(formCollection.Files);
            Assert.Equal(0, formCollection.Files.Count);

            // Cleanup
            await responseFeature.CompleteAsync();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadForm_MultipartWithField_ReturnsParsedFormCollection(bool bufferRequest)
        {
            var formContent = Encoding.UTF8.GetBytes(MultipartFormWithField);
            var context = new DefaultHttpContext();
            var responseFeature = new FakeResponseFeature();
            context.Features.Set<IHttpResponseFeature>(responseFeature);
            context.Request.ContentType = MultipartContentType;
            context.Request.Body = new NonSeekableReadStream(formContent);

            if (bufferRequest)
            {
                context.Request.EnableRewind();
            }

            // Not cached yet
            var formFeature = context.Features.Get<IFormFeature>();
            Assert.Null(formFeature);

            var formCollection = context.Request.Form;

            Assert.NotNull(formCollection);

            // Cached
            formFeature = context.Features.Get<IFormFeature>();
            Assert.NotNull(formFeature);
            Assert.NotNull(formFeature.Form);
            Assert.Same(formCollection, formFeature.Form);
            Assert.Same(formCollection, await context.Request.ReadFormAsync());

            // Content
            Assert.Equal(1, formCollection.Count);
            Assert.Equal("Foo", formCollection["description"]);

            Assert.NotNull(formCollection.Files);
            Assert.Equal(0, formCollection.Files.Count);

            // Cleanup
            await responseFeature.CompleteAsync();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadFormAsync_MultipartWithFile_ReturnsParsedFormCollection(bool bufferRequest)
        {
            var formContent = Encoding.UTF8.GetBytes(MultipartFormWithFile);
            var context = new DefaultHttpContext();
            var responseFeature = new FakeResponseFeature();
            context.Features.Set<IHttpResponseFeature>(responseFeature);
            context.Request.ContentType = MultipartContentType;
            context.Request.Body = new NonSeekableReadStream(formContent);

            if (bufferRequest)
            {
                context.Request.EnableRewind();
            }

            // Not cached yet
            var formFeature = context.Features.Get<IFormFeature>();
            Assert.Null(formFeature);

            var formCollection = await context.Request.ReadFormAsync();

            Assert.NotNull(formCollection);

            // Cached
            formFeature = context.Features.Get<IFormFeature>();
            Assert.NotNull(formFeature);
            Assert.NotNull(formFeature.Form);
            Assert.Same(formFeature.Form, formCollection);
            Assert.Same(formCollection, context.Request.Form);

            // Content
            Assert.Equal(0, formCollection.Count);

            Assert.NotNull(formCollection.Files);
            Assert.Equal(1, formCollection.Files.Count);

            var file = formCollection.Files["myfile1"];
            Assert.Equal("myfile1", file.Name);
            Assert.Equal("temp.html", file.FileName);
            Assert.Equal("text/html", file.ContentType);
            Assert.Equal(@"form-data; name=""myfile1""; filename=""temp.html""", file.ContentDisposition);
            var body = file.OpenReadStream();
            using (var reader = new StreamReader(body))
            {
                Assert.True(body.CanSeek);
                var content = reader.ReadToEnd();
                Assert.Equal(content, "<html><body>Hello World</body></html>");
            }

            await responseFeature.CompleteAsync();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadFormAsync_MultipartWithFieldAndFile_ReturnsParsedFormCollection(bool bufferRequest)
        {
            var formContent = Encoding.UTF8.GetBytes(MultipartFormWithFieldAndFile);
            var context = new DefaultHttpContext();
            var responseFeature = new FakeResponseFeature();
            context.Features.Set<IHttpResponseFeature>(responseFeature);
            context.Request.ContentType = MultipartContentType;
            context.Request.Body = new NonSeekableReadStream(formContent);

            if (bufferRequest)
            {
                context.Request.EnableRewind();
            }

            // Not cached yet
            var formFeature = context.Features.Get<IFormFeature>();
            Assert.Null(formFeature);

            var formCollection = await context.Request.ReadFormAsync();

            Assert.NotNull(formCollection);

            // Cached
            formFeature = context.Features.Get<IFormFeature>();
            Assert.NotNull(formFeature);
            Assert.NotNull(formFeature.Form);
            Assert.Same(formFeature.Form, formCollection);
            Assert.Same(formCollection, context.Request.Form);

            // Content
            Assert.Equal(1, formCollection.Count);
            Assert.Equal("Foo", formCollection["description"]);

            Assert.NotNull(formCollection.Files);
            Assert.Equal(1, formCollection.Files.Count);

            var file = formCollection.Files["myfile1"];
            Assert.Equal("text/html", file.ContentType);
            Assert.Equal(@"form-data; name=""myfile1""; filename=""temp.html""", file.ContentDisposition);
            var body = file.OpenReadStream();
            using (var reader = new StreamReader(body))
            {
                Assert.True(body.CanSeek);
                var content = reader.ReadToEnd();
                Assert.Equal(content, "<html><body>Hello World</body></html>");
            }

            await responseFeature.CompleteAsync();
        }
    }
}
