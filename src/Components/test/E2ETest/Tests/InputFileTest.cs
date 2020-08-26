// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests
{
    public class InputFileTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>, IDisposable
    {
        private string _tempDirectory;

        public InputFileTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
            Browser.MountTestComponent<InputFileComponent>();
        }

        [Fact]
        public void CanUploadSingleSmallFile()
        {
            // Create a temporary text file
            var file = TempFile.Create(_tempDirectory, "txt", "This file was uploaded to the browser and read from .NET.");

            // Upload the file
            var inputFile = Browser.FindElement(By.Id("input-file"));
            inputFile.SendKeys(file.Path);

            var fileContainer = Browser.FindElement(By.Id($"file-{file.Name}"));
            var fileSizeElement = fileContainer.FindElement(By.Id("file-size"));
            var fileContentElement = fileContainer.FindElement(By.Id("file-content"));

            // Validate that the file was uploaded correctly
            Browser.Equal(file.Contents.Length.ToString(), () => fileSizeElement.Text);
            Browser.Equal(file.Text, () => fileContentElement.Text);
        }

        [Fact]
        public void CanUploadSingleLargeFile()
        {
            // Create a large text file
            var fileContentSizeInBytes = 1024 * 1024;
            var contentBuilder = new StringBuilder();

            for (int i = 0; i < fileContentSizeInBytes; i++)
            {
                contentBuilder.Append((i % 10).ToString());
            }

            var file = TempFile.Create(_tempDirectory, "txt", contentBuilder.ToString());

            // Upload the file
            var inputFile = Browser.FindElement(By.Id("input-file"));
            inputFile.SendKeys(file.Path);

            var fileContainer = Browser.FindElement(By.Id($"file-{file.Name}"));
            var fileSizeElement = fileContainer.FindElement(By.Id("file-size"));
            var fileContentElement = fileContainer.FindElement(By.Id("file-content"));

            // Validate that the file was uploaded correctly
            Browser.Equal(file.Contents.Length.ToString(), () => fileSizeElement.Text);
            Browser.Equal(file.Text, () => fileContentElement.Text);
        }

        [Fact]
        public void CanUploadMultipleFiles()
        {
            // Create multiple small text files
            var files = Enumerable.Range(1, 3)
                .Select(i => TempFile.Create(_tempDirectory, "txt", $"Contents of file {i}."))
                .ToList();

            // Upload each file
            var inputFile = Browser.FindElement(By.Id("input-file"));
            inputFile.SendKeys(string.Join("\n", files.Select(f => f.Path)));

            // VAlidate that each file was uploaded correctly
            Assert.All(files, file =>
            {
                var fileContainer = Browser.FindElement(By.Id($"file-{file.Name}"));
                var fileSizeElement = fileContainer.FindElement(By.Id("file-size"));
                var fileContentElement = fileContainer.FindElement(By.Id("file-content"));

                Browser.Equal(file.Contents.Length.ToString(), () => fileSizeElement.Text);
                Browser.Equal(file.Text, () => fileContentElement.Text);
            });
        }

        [Fact]
        public void CanUploadAndConvertImageFile()
        {
            var sourceImageId = "image-source";

            // Get the source image base64
            var base64 = Browser.ExecuteJavaScript<string>($@"
                const canvas = document.createElement('canvas');
                const context = canvas.getContext('2d');
                const image = document.getElementById('{sourceImageId}');

                canvas.width = image.naturalWidth;
                canvas.height = image.naturalHeight;
                context.drawImage(image, 0, 0, image.naturalWidth, image.naturalHeight);

                return canvas.toDataURL().split(',').pop();");

            // Save the image file locally
            var file = TempFile.Create(_tempDirectory, "png", Convert.FromBase64String(base64));

            // Re-upload the image file (it will be converted to a JPEG and scaled to fix 640x480)
            var inputFile = Browser.FindElement(By.Id("input-image"));
            inputFile.SendKeys(file.Path);

            // Validate that the image was converted without error and is the correct size
            var uploadedImage = Browser.FindElement(By.Id("image-uploaded"));

            Browser.Equal(480, () => uploadedImage.Size.Width);
            Browser.Equal(480, () => uploadedImage.Size.Height);
        }

        public void Dispose()
        {
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
