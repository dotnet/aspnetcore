// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using BasicTestApp;
using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

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

        Navigate(ServerPathBase);
        Browser.MountTestComponent<InputFileComponent>();
    }

    [Fact]
    public void CanUploadSingleSmallFile()
    {
        // Create a temporary text file
        var file = TempFile.Create(_tempDirectory, "txt", "This file was uploaded to the browser and read from .NET.");

        // Upload the file
        var inputFile = Browser.Exists(By.Id("input-file"));
        inputFile.SendKeys(file.Path);

        var fileContainer = Browser.Exists(By.Id($"file-{file.Name}"));
        var fileNameElement = fileContainer.FindElement(By.Id("file-name"));
        var fileLastModifiedElement = fileContainer.FindElement(By.Id("file-last-modified"));
        var fileSizeElement = fileContainer.FindElement(By.Id("file-size"));
        var fileContentTypeElement = fileContainer.FindElement(By.Id("file-content-type"));
        var fileContentElement = fileContainer.FindElement(By.Id("file-content"));

        // Validate that the file was uploaded correctly and all fields are present
        Browser.False(() => string.IsNullOrWhiteSpace(fileNameElement.Text));
        Browser.NotEqual(default, () => DateTimeOffset.Parse(fileLastModifiedElement.Text, CultureInfo.InvariantCulture));
        Browser.Equal(file.Contents.Length.ToString(CultureInfo.InvariantCulture), () => fileSizeElement.Text);
        Browser.Equal("text/plain", () => fileContentTypeElement.Text);
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
            contentBuilder.Append((i % 10).ToString(CultureInfo.InvariantCulture));
        }

        var file = TempFile.Create(_tempDirectory, "txt", contentBuilder.ToString());

        // Upload the file
        var inputFile = Browser.Exists(By.Id("input-file"));
        inputFile.SendKeys(file.Path);

        var fileContainer = Browser.Exists(By.Id($"file-{file.Name}"));
        var fileNameElement = fileContainer.FindElement(By.Id("file-name"));
        var fileLastModifiedElement = fileContainer.FindElement(By.Id("file-last-modified"));
        var fileSizeElement = fileContainer.FindElement(By.Id("file-size"));
        var fileContentTypeElement = fileContainer.FindElement(By.Id("file-content-type"));
        var fileContentElement = fileContainer.FindElement(By.Id("file-content"));

        // Validate that the file was uploaded correctly and all fields are present
        Browser.False(() => string.IsNullOrWhiteSpace(fileNameElement.Text));
        Browser.NotEqual(default, () => DateTimeOffset.Parse(fileLastModifiedElement.Text, CultureInfo.InvariantCulture));
        Browser.Equal(file.Contents.Length.ToString(CultureInfo.InvariantCulture), () => fileSizeElement.Text);
        Browser.Equal("text/plain", () => fileContentTypeElement.Text);
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
        var inputFile = Browser.Exists(By.Id("input-file"));
        inputFile.SendKeys(string.Join("\n", files.Select(f => f.Path)));

        // Validate that each file was uploaded correctly
        Assert.All(files, file =>
        {
            var fileContainer = Browser.Exists(By.Id($"file-{file.Name}"));
            var fileNameElement = fileContainer.FindElement(By.Id("file-name"));
            var fileLastModifiedElement = fileContainer.FindElement(By.Id("file-last-modified"));
            var fileSizeElement = fileContainer.FindElement(By.Id("file-size"));
            var fileContentTypeElement = fileContainer.FindElement(By.Id("file-content-type"));
            var fileContentElement = fileContainer.FindElement(By.Id("file-content"));

            // Validate that the file was uploaded correctly and all fields are present
            Browser.False(() => string.IsNullOrWhiteSpace(fileNameElement.Text));
            Browser.NotEqual(default, () => DateTimeOffset.Parse(fileLastModifiedElement.Text, CultureInfo.InvariantCulture));
            Browser.Equal(file.Contents.Length.ToString(CultureInfo.InvariantCulture), () => fileSizeElement.Text);
            Browser.Equal("text/plain", () => fileContentTypeElement.Text);
            Browser.Equal(file.Text, () => fileContentElement.Text);
        });
    }

    [Fact]
    public void CanUploadAndConvertImageFile()
    {
        var sourceImageId = "image-source";
        var imageStatus = Browser.Exists(By.Id("image-status"));
        Browser.Equal("ready", () => imageStatus.Text);

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
        var inputFile = Browser.Exists(By.Id("input-image"));
        inputFile.SendKeys(file.Path);

        // Validate that the image was converted without error and is the correct size
        var uploadedImage = Browser.Exists(By.Id("image-uploaded"));

        Browser.Equal(480, () => uploadedImage.Size.Width);
        Browser.Equal(480, () => uploadedImage.Size.Height);
    }

    [Fact]
    public void ThrowsWhenTooManyFilesAreSelected()
    {
        var maxAllowedFilesElement = Browser.Exists(By.Id("max-allowed-files"));
        maxAllowedFilesElement.Clear();
        maxAllowedFilesElement.SendKeys("1\n");

        // Save two files locally
        var file1 = TempFile.Create(_tempDirectory, "txt", "This is file 1.");
        var file2 = TempFile.Create(_tempDirectory, "txt", "This is file 2.");

        // Select both files
        var inputFile = Browser.Exists(By.Id("input-file"));
        inputFile.SendKeys($"{file1.Path}\n{file2.Path}");

        // Validate that the proper exception is thrown
        var exceptionMessage = Browser.Exists(By.Id("exception-message"));
        Browser.Equal("The maximum number of files accepted is 1, but 2 were supplied.", () => exceptionMessage.Text);
    }

    [Fact]
    public void ThrowsWhenOversizedFileIsSelected()
    {
        var maxFileSizeElement = Browser.Exists(By.Id("max-file-size"));
        maxFileSizeElement.Clear();
        maxFileSizeElement.SendKeys("10\n");

        // Save a file that exceeds the specified file size limit
        var file = TempFile.Create(_tempDirectory, "txt", "This file is over 10 bytes long.");

        // Select the file
        var inputFile = Browser.Exists(By.Id("input-file"));
        inputFile.SendKeys(file.Path);

        // Validate that the proper exception is thrown
        var exceptionMessage = Browser.Exists(By.Id("exception-message"));
        Browser.Equal("Supplied file with size 32 bytes exceeds the maximum of 10 bytes.", () => exceptionMessage.Text);
    }

    [Fact]
    public void CanClearFilesByInvokingCancelEvent()
    {
        // Upload a file first
        var file = TempFile.Create(_tempDirectory, "txt", "This is a test file.");
        var inputFile = Browser.Exists(By.Id("input-file"));
        inputFile.SendKeys(file.Path);

        // Verify the file was uploaded
        var fileContainer = Browser.Exists(By.Id($"file-{file.Name}"));

        // Get the file count element
        var fileCount = Browser.Exists(By.Id("file-count"));
        Browser.Equal("1", () => fileCount.Text);

        // Trigger the cancel event via JavaScript to simulate canceling the file dialog
        Browser.ExecuteJavaScript(@"
            const inputElement = document.getElementById('input-file');
            inputElement.dispatchEvent(new Event('cancel'));
        ");

        // Wait a moment for the event to be processed and verify the file list was cleared
        Browser.Equal("0", () => Browser.Exists(By.Id("file-count")).Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnChangeExceptionIsHandledByErrorBoundary(bool asynchronous)
    {
        Browser.MountTestComponent<InputFileExceptionComponent>();
        if (asynchronous)
        {
            Browser.Exists(By.Id("throw-asynchronously")).Click();
        }

        var file = TempFile.Create(_tempDirectory, "txt", "InputFile exception handling");
        Browser.Exists(By.Id("exception-input-file")).SendKeys(file.Path);

        Browser.Equal("InputFile change failed", () => Browser.Exists(By.Id("input-file-error")).Text);
        Browser.Empty(() => Browser.FindElements(By.Id("outer-input-file-error")));
        Browser.Equal("none", () => Browser.Exists(By.Id("blazor-error-ui")).GetCssValue("display"));
        Browser.Exists(By.Id("input-file-increment")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("input-file-count")).Text);

        Browser.Exists(By.Id("throw-on-change")).Click();
        Browser.Exists(By.Id("recover-input-file")).Click();
        Browser.Exists(By.Id("exception-input-file")).SendKeys(file.Path);

        Browser.Equal(file.Name, () => Browser.Exists(By.Id("selected-file-name")).Text);
        Browser.Empty(() => Browser.FindElements(By.Id("input-file-error")));
        Browser.Equal("none", () => Browser.Exists(By.Id("blazor-error-ui")).GetCssValue("display"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnChangeExceptionWithoutErrorBoundaryIsFatal(bool asynchronous)
    {
        Browser.MountTestComponent<InputFileExceptionComponent>();
        Browser.Exists(By.Id("use-error-boundary")).Click();
        if (asynchronous)
        {
            Browser.Exists(By.Id("throw-asynchronously")).Click();
        }

        var sink = _serverFixture.ExecutionMode == ExecutionMode.Server
            ? _serverFixture.Host.Services.GetRequiredService<TestSink>()
            : null;
        var previousLogCount = sink?.Writes.Count ?? 0;
        var file = TempFile.Create(_tempDirectory, "txt", "InputFile exception handling");
        Browser.Exists(By.Id("exception-input-file")).SendKeys(file.Path);

        AssertFatalError(sink, previousLogCount);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void OnChangeCompletesAfterInputIsRemoved(bool useErrorBoundary, bool throwOnChange)
    {
        Browser.MountTestComponent<InputFileExceptionComponent>();
        if (!useErrorBoundary)
        {
            Browser.Exists(By.Id("use-error-boundary")).Click();
        }
        if (!throwOnChange)
        {
            Browser.Exists(By.Id("throw-on-change")).Click();
        }
        Browser.Exists(By.Id("remove-input")).Click();
        var sink = _serverFixture.ExecutionMode == ExecutionMode.Server
            ? _serverFixture.Host.Services.GetRequiredService<TestSink>()
            : null;
        var previousLogCount = sink?.Writes.Count ?? 0;
        var file = TempFile.Create(_tempDirectory, "txt", "InputFile disposal");
        Browser.Exists(By.Id("exception-input-file")).SendKeys(file.Path);
        Browser.Equal("True", () => Browser.Exists(By.Id("callback-pending")).Text);
        Browser.Empty(() => Browser.FindElements(By.Id("exception-input-file")));
        Browser.Exists(By.Id("complete-callback")).Click();

        if (throwOnChange && !useErrorBoundary)
        {
            AssertFatalError(sink, previousLogCount);
            return;
        }

        if (throwOnChange)
        {
            Browser.Equal("InputFile change failed", () => Browser.Exists(By.Id("input-file-error")).Text);
            Browser.Empty(() => Browser.FindElements(By.Id("outer-input-file-error")));
        }
        else
        {
            Browser.Equal(file.Name, () => Browser.Exists(By.Id("selected-file-name")).Text);
            Browser.Empty(() => Browser.FindElements(By.Id("input-file-error")));
        }
        Browser.Equal("none", () => Browser.Exists(By.Id("blazor-error-ui")).GetCssValue("display"));
        Browser.Exists(By.Id("input-file-increment")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("input-file-count")).Text);
    }

    private void AssertFatalError(TestSink sink, int previousLogCount)
    {
        Browser.Equal("block", () => Browser.Exists(By.Id("blazor-error-ui")).GetCssValue("display"));
        if (sink is not null)
        {
            Browser.True(() => sink.Writes.Skip(previousLogCount).Any(write =>
                write.EventId.Name == "CircuitUnhandledException" &&
                write.Exception is InvalidOperationException { Message: "InputFile change failed" }));
            var failure = sink.Writes.Skip(previousLogCount).First(write =>
                write.EventId.Name == "CircuitUnhandledException" &&
                write.Exception is InvalidOperationException { Message: "InputFile change failed" });
            var circuitId = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object>>>(failure.State)
                .Single(pair => pair.Key == "CircuitId").Value;
            Browser.True(() => sink.Writes.Skip(previousLogCount).Any(write =>
                write.EventId.Name == "DisposeSucceeded" &&
                write.State is IEnumerable<KeyValuePair<string, object>> state &&
                state.Any(pair => pair.Key == "CircuitId" && Equals(pair.Value, circuitId))));
        }
        else
        {
            var logs = new List<LogEntry>();
            Browser.True(() =>
            {
                logs.AddRange(Browser.Manage().Logs.GetLog(LogType.Browser));
                return logs.Any(log => log.Message.Contains("InputFile change failed", StringComparison.Ordinal));
            });
        }
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
            Path = System.IO.Path.Combine(tempDirectory, Name);
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
