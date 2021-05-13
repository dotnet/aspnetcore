// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BasicTestApp;
using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using PlaywrightSharp;
using Microsoft.AspNetCore.BrowserTesting;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class InputFileTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        private string _tempDirectory;

        public InputFileTest(
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(serverFixture, output)
        {
        }

        private IPage _page;
        private IBrowserContext _browser;

        protected override async Task InitializeCoreAsync(TestContext context)
        {
            await base.InitializeCoreAsync(context);

            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            //Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);

            _browser = await BrowserManager.GetBrowserInstance(BrowserKind.Chromium, BrowserContextInfo);
            var page = await _browser.NewPageAsync();
            var url = _serverFixture.RootUri + "subdir";
            var response = await page.GoToAsync(url);

            Assert.True(response.Ok, "Got: " + response.StatusText + "from: " + url);
            Output.WriteLine("Loaded page");

            await MountTestComponentAsync<InputFileComponent>(page);

            _page = page;
        }

        private async Task VerifyFile(TempFile file)
        {
            // Upload the file
            await _page.SetInputFilesAsync("#input-file", file.Path);

            var fileContainer = await _page.WaitForSelectorAsync($"[id='file-{file.Name}']");
            Assert.NotNull(fileContainer);
            var fileNameElement = await fileContainer.QuerySelectorAsync("#file-name");
            Assert.NotNull(fileNameElement);
            var fileLastModifiedElement = await fileContainer.QuerySelectorAsync("#file-last-modified");
            Assert.NotNull(fileLastModifiedElement);
            var fileSizeElement = await fileContainer.QuerySelectorAsync("#file-size");
            Assert.NotNull(fileSizeElement);
            var fileContentTypeElement = await fileContainer.QuerySelectorAsync("#file-content-type");
            Assert.NotNull(fileContentTypeElement);
            var fileContentElement = await fileContainer.QuerySelectorAsync("#file-content");
            Assert.NotNull(fileContentElement);

            // Validate that the file was uploaded correctly and all fields are present
            Assert.False(string.IsNullOrWhiteSpace(await fileNameElement.GetTextContentAsync()));
            Assert.NotEqual(default, DateTimeOffset.Parse(await fileLastModifiedElement.GetTextContentAsync(), CultureInfo.InvariantCulture));
            Assert.Equal(file.Contents.Length.ToString(CultureInfo.InvariantCulture), await fileSizeElement.GetTextContentAsync());
            Assert.Equal("text/plain", await fileContentTypeElement.GetTextContentAsync());
            Assert.Equal(file.Text, await fileContentElement.GetTextContentAsync());
        }

        private async Task VerifyFiles(IEnumerable<TempFile> files)
        {
            // Upload the files
            var filePaths = files
                .Select(i => i.Path)
                .ToArray();

            await _page.SetInputFilesAsync("#input-file", filePaths);

            foreach (var file in files)
            {
                var fileContainer = await _page.WaitForSelectorAsync($"[id='file-{file.Name}']");
                Assert.NotNull(fileContainer);
                var fileNameElement = await fileContainer.QuerySelectorAsync("#file-name");
                Assert.NotNull(fileNameElement);
                var fileLastModifiedElement = await fileContainer.QuerySelectorAsync("#file-last-modified");
                Assert.NotNull(fileLastModifiedElement);
                var fileSizeElement = await fileContainer.QuerySelectorAsync("#file-size");
                Assert.NotNull(fileSizeElement);
                var fileContentTypeElement = await fileContainer.QuerySelectorAsync("#file-content-type");
                Assert.NotNull(fileContentTypeElement);
                var fileContentElement = await fileContainer.QuerySelectorAsync("#file-content");
                Assert.NotNull(fileContentElement);

                // Validate that the file was uploaded correctly and all fields are present
                Assert.False(string.IsNullOrWhiteSpace(await fileNameElement.GetTextContentAsync()));
                Assert.NotEqual(default, DateTimeOffset.Parse(await fileLastModifiedElement.GetTextContentAsync(), CultureInfo.InvariantCulture));
                Assert.Equal(file.Contents.Length.ToString(CultureInfo.InvariantCulture), await fileSizeElement.GetTextContentAsync());
                Assert.Equal("text/plain", await fileContentTypeElement.GetTextContentAsync());
                Assert.Equal(file.Text, await fileContentElement.GetTextContentAsync());
            }
        }

        [Fact]
        public async Task CanUploadSingleSmallFile()
        {
            // Create a temporary text file
            var file = TempFile.Create(_tempDirectory, "txt", "This file was uploaded to the browser and read from .NET.");
            await VerifyFile(file);
        }

        [Fact]
        public async Task CanUploadSingleLargeFile()
        {
            // Create a large text file
            var fileContentSizeInBytes = 1024 * 1024;
            var contentBuilder = new StringBuilder();

            for (int i = 0; i < fileContentSizeInBytes; i++)
            {
                contentBuilder.Append((i % 10).ToString(CultureInfo.InvariantCulture));
            }

            var file = TempFile.Create(_tempDirectory, "txt", contentBuilder.ToString());

            await VerifyFile(file);
        }

        [Fact]
        public async Task CanUploadMultipleFiles()
        {
            // Create multiple small text files
            var files = Enumerable.Range(1, 3)
                .Select(i => TempFile.Create(_tempDirectory, "txt", $"Contents of file {i}."))
                .ToList();

            await VerifyFiles(files);
        }

        [Fact]
        public async Task CanUploadAndConvertImageFile()
        {
            var sourceImageId = "image-source";

            // Get the source image base64
            var base64 = await _page.EvaluateAsync<string>($@"
                const canvas = document.createElement('canvas');
                const context = canvas.getContext('2d');
                const image = document.getElementById('{sourceImageId}');

                canvas.width = image.naturalWidth;
                canvas.height = image.naturalHeight;
                context.drawImage(image, 0, 0, image.naturalWidth, image.naturalHeight);

                canvas.toDataURL().split(',').pop();");

            // Save the image file locally
            var file = TempFile.Create(_tempDirectory, "png", Convert.FromBase64String(base64));

            // Re-upload the image file (it will be converted to a JPEG and scaled to fix 640x480)
            var inputFile = await _page.QuerySelectorAsync("#input-image");
            await inputFile.SetInputFilesAsync(file.Path);

            // Validate that the image was converted without error and is the correct size
            var uploadedImage = await _page.WaitForSelectorAsync("#image-uploaded");
            Assert.NotNull(uploadedImage);
            var box = await uploadedImage.GetBoundingBoxAsync();
            Assert.Equal(480, box.Height);
            Assert.Equal(480, box.Width);
        }

        protected async Task ClearAndType(string selector, string value)
        {
            await _page.EvalOnSelectorAsync(selector, "e => e.value = ''");
            var element = await _page.QuerySelectorAsync(selector);
            await element.TypeAsync(value);
        }

        [Fact]
        public async Task ThrowsWhenTooManyFilesAreSelected()
        {
            await ClearAndType("#max-allowed-files", "1\n");

            // Save two files locally
            var file1 = TempFile.Create(_tempDirectory, "txt", "This is file 1.");
            var file2 = TempFile.Create(_tempDirectory, "txt", "This is file 2.");

            // Select both files
            await _page.SetInputFilesAsync("#input-file", new string[] { file1.Path, file2.Path });

            // Validate that the proper exception is thrown
            var exceptionMessage = await _page.QuerySelectorAsync("#exception-message");
            Assert.Equal("The maximum number of files accepted is 1, but 2 were supplied.", await exceptionMessage.GetTextContentAsync());
        }

        [Fact]
        public async Task ThrowsWhenOversizedFileIsSelected()
        {
            await ClearAndType("#max-file-size", "10\n");

            // Save a file that exceeds the specified file size limit
            var file = TempFile.Create(_tempDirectory, "txt", "This file is over 10 bytes long.");

            // Select the file
            await _page.SetInputFilesAsync("#input-file", file.Path);

            // Validate that the proper exception is thrown
            var exceptionMessage = await _page.QuerySelectorAsync("#exception-message");
            Assert.Equal("Supplied file with size 32 bytes exceeds the maximum of 10 bytes.", await exceptionMessage.GetTextContentAsync());
        }

        public override void Dispose()
        {
            base.Dispose();
            Directory.Delete(_tempDirectory, recursive: true);
        }

        private struct TempFile
        {
            public string Name { get; }
            public string Path { get; }
            public byte[] Contents { get; }

            public string Text => Encoding.ASCII.GetString(Contents);

            private TempFile(string tempDirectory, string extension, byte[] contents)
            {
                Name = $"{Guid.NewGuid():N}.{extension}";
                Path = $"{tempDirectory}\\{Name}";
                Contents = contents;
            }

            public static TempFile Create(string tempDirectory, string extension, byte[] contents)
            {
                var file = new TempFile(tempDirectory, extension, contents);

                File.WriteAllBytes(file.Path, contents);

                return file;
            }

            public static TempFile Create(string tempDirectory, string extension, string text)
                => Create(tempDirectory, extension, Encoding.ASCII.GetBytes(text));
        }
    }
}
