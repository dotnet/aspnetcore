// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace Microsoft.AspNetCore.Http.Features
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

        [Theory]
        // FileBufferingReadStream transitions to disk storage after 30kb, and stops pooling buffers at 1mb.
        [InlineData(true, 1024)]
        [InlineData(false, 1024)]
        [InlineData(true, 40 * 1024)]
        [InlineData(false, 40 * 1024)]
        [InlineData(true, 4 * 1024 * 1024)]
        [InlineData(false, 4 * 1024 * 1024)]
        public async Task ReadFormAsync_MultipartWithFieldAndMediumFile_ReturnsParsedFormCollection(bool bufferRequest, int fileSize)
        {
            var fileContents = CreateFile(fileSize);
            var formContent = CreateMultipartWithFormAndFile(fileContents);
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
            using (var body = file.OpenReadStream())
            {
                Assert.True(body.CanSeek);
                CompareStreams(fileContents, body);
            }

            await responseFeature.CompleteAsync();
        }

        private Stream CreateFile(int size)
        {
            var stream = new MemoryStream(size);
            var bytes = Encoding.ASCII.GetBytes("HelloWorld_ABCDEFGHIJKLMNOPQRSTUVWXYZ.abcdefghijklmnopqrstuvwxyz,0123456789;");
            int written = 0;
            while (written < size)
            {
                var toWrite = Math.Min(size - written, bytes.Length);
                stream.Write(bytes, 0, toWrite);
                written += toWrite;
            }
            stream.Position = 0;
            return stream;
        }

        private Stream CreateMultipartWithFormAndFile(Stream fileContents)
        {
            var stream = new MemoryStream();
            var header =
"--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: form-data; name=\"description\"\r\n" +
"\r\n" +
"Foo\r\n" +
"--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: form-data; name=\"myfile1\"; filename=\"temp.html\"\r\n" +
"Content-Type: text/html\r\n" +
"\r\n";
            var footer =
"\r\n--WebKitFormBoundary5pDRpGheQXaM8k3T--";

            var bytes = Encoding.ASCII.GetBytes(header);
            stream.Write(bytes, 0, bytes.Length);

            fileContents.CopyTo(stream);
            fileContents.Position = 0;

            bytes = Encoding.ASCII.GetBytes(footer);
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            return stream;
        }

        private void CompareStreams(Stream streamA, Stream streamB)
        {
            Assert.Equal(streamA.Length, streamB.Length);
            byte[] bytesA = new byte[1024], bytesB = new byte[1024];
            var readA = streamA.Read(bytesA, 0, bytesA.Length);
            var readB = streamB.Read(bytesB, 0, bytesB.Length);
            Assert.Equal(readA, readB);
            var loops = 0;
            while (readA > 0)
            {
                for (int i = 0; i < readA; i++)
                {
                    if (bytesA[i] != bytesB[i])
                    {
                        throw new Exception($"Value mismatch at loop {loops}, index {i}; A:{bytesA[i]}, B:{bytesB[i]}");
                    }
                }

                readA = streamA.Read(bytesA, 0, bytesA.Length);
                readB = streamB.Read(bytesB, 0, bytesB.Length);
                Assert.Equal(readA, readB);
                loops++;
            }
        }
    }
}
