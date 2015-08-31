// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.WebUtilities;
using Xunit;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class FormFeatureTests
    {
        [Fact]
        public async Task ReadFormAsync_SimpleData_ReturnsParsedFormCollection()
        {
            // Arrange
            var formContent = Encoding.UTF8.GetBytes("foo=bar&baz=2");
            var context = new DefaultHttpContext();
            context.Request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
            context.Request.Body = new MemoryStream(formContent);

            // Not cached yet
            var formFeature = context.Features.Get<IFormFeature>();
            Assert.Null(formFeature);

            // Act
            var formCollection = await context.Request.ReadFormAsync();

            // Assert
            Assert.Equal("bar", formCollection["foo"]);
            Assert.Equal("2", formCollection["baz"]);

            // Cached
            formFeature = context.Features.Get<IFormFeature>();
            Assert.NotNull(formFeature);
            Assert.NotNull(formFeature.Form);
            Assert.Same(formFeature.Form, formCollection);
        }

        [Fact]
        public async Task ReadFormAsync_EmptyKeyAtEndAllowed()
        {
            // Arrange
            var formContent = Encoding.UTF8.GetBytes("=bar");
            var body = new MemoryStream(formContent);

            var formCollection = await FormReader.ReadFormAsync(body);

            // Assert
            Assert.Equal("bar", formCollection[""].FirstOrDefault());
        }

        [Fact]
        public async Task ReadFormAsync_EmptyKeyWithAdditionalEntryAllowed()
        {
            // Arrange
            var formContent = Encoding.UTF8.GetBytes("=bar&baz=2");
            var body = new MemoryStream(formContent);

            var formCollection = await FormReader.ReadFormAsync(body);

            // Assert
            Assert.Equal("bar", formCollection[""].FirstOrDefault());
            Assert.Equal("2", formCollection["baz"].FirstOrDefault());
        }

        [Fact]
        public async Task ReadFormAsync_EmptyValuedAtEndAllowed()
        {
            // Arrange
            var formContent = Encoding.UTF8.GetBytes("foo=");
            var body = new MemoryStream(formContent);

            var formCollection = await FormReader.ReadFormAsync(body);

            // Assert
            Assert.Equal("", formCollection["foo"].FirstOrDefault());
        }

        [Fact]
        public async Task ReadFormAsync_EmptyValuedWithAdditionalEntryAllowed()
        {
            // Arrange
            var formContent = Encoding.UTF8.GetBytes("foo=&baz=2");
            var body = new MemoryStream(formContent);

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

        [Fact]
        public async Task ReadForm_EmptyMultipart_ReturnsParsedFormCollection()
        {
            var formContent = Encoding.UTF8.GetBytes(EmptyMultipartForm);
            var context = new DefaultHttpContext();
            context.Request.ContentType = MultipartContentType;
            context.Request.Body = new MemoryStream(formContent);

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
        }

        [Fact]
        public async Task ReadForm_MultipartWithField_ReturnsParsedFormCollection()
        {
            var formContent = Encoding.UTF8.GetBytes(MultipartFormWithField);
            var context = new DefaultHttpContext();
            context.Request.ContentType = MultipartContentType;
            context.Request.Body = new MemoryStream(formContent);

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
        }

        [Fact]
        public async Task ReadFormAsync_MultipartWithFile_ReturnsParsedFormCollection()
        {
            var formContent = Encoding.UTF8.GetBytes(MultipartFormWithFile);
            var context = new DefaultHttpContext();
            context.Request.ContentType = MultipartContentType;
            context.Request.Body = new MemoryStream(formContent);

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
            Assert.Equal("text/html", file.ContentType);
            Assert.Equal(@"form-data; name=""myfile1""; filename=""temp.html""", file.ContentDisposition);
            var body = file.OpenReadStream();
            using (var reader = new StreamReader(body))
            {
                var content = reader.ReadToEnd();
                Assert.Equal(content, "<html><body>Hello World</body></html>");
            }
        }

        [Fact]
        public async Task ReadFormAsync_MultipartWithFieldAndFile_ReturnsParsedFormCollection()
        {
            var formContent = Encoding.UTF8.GetBytes(MultipartFormWithFieldAndFile);
            var context = new DefaultHttpContext();
            context.Request.ContentType = MultipartContentType;
            context.Request.Body = new MemoryStream(formContent);

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
                var content = reader.ReadToEnd();
                Assert.Equal(content, "<html><body>Hello World</body></html>");
            }
        }
    }
}
