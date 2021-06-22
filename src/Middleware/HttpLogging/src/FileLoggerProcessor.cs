// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal class FileLoggerProcessor : IAsyncDisposable
    {
        private const int _maxQueuedMessages = 1024;

        private string _path;
        private string _fileName;
        private int? _maxFileSize;
        private int? _maxRetainedFiles;
        private int _fileNumber = 1;
        private TimeSpan _flushPeriod;

        private IOptionsMonitor<W3CLoggerOptions> _options;
        private readonly BlockingCollection<string> _messageQueue = new BlockingCollection<string>(_maxQueuedMessages);
        private readonly ILogger _logger;
        private readonly List<string> _currentBatch = new List<string>();
        private Task _outputTask;
        private CancellationTokenSource _cancellationTokenSource;

        public FileLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory)
        {
            _options = options;
            var loggerOptions = _options.CurrentValue;
            _path = loggerOptions.LogDirectory;
            if (string.IsNullOrEmpty(_path))
            {
                _path = Path.Join(environment.ContentRootPath, "logs", DateTimeOffset.Now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
            }
            _fileName = loggerOptions.FileName;
            _maxFileSize = loggerOptions.FileSizeLimit;
            _maxRetainedFiles = loggerOptions.RetainedFileCountLimit;
            _flushPeriod = loggerOptions.FlushPeriod;

            _logger = factory.CreateLogger(typeof(FileLoggerProcessor));

            // Start message queue processor
            _cancellationTokenSource = new CancellationTokenSource();
            _outputTask = Task.Run(ProcessLogQueue);
        }

        public void EnqueueMessage(string message)
        {
            if (!_messageQueue.IsAddingCompleted)
            {
                try
                {
                    _messageQueue.Add(message);
                    return;
                }
                catch (InvalidOperationException) { }
            }
        }

        private async Task ProcessLogQueue()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                while (_messageQueue.TryTake(out var message))
                {
                    _currentBatch.Add(message);
                }
                if (_currentBatch.Count > 0)
                {
                    try
                    {
                        await WriteMessagesAsync(_currentBatch);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteMessagesFailed(_logger, ex);
                    }

                    _currentBatch.Clear();
                }
                else
                {
                    try
                    {
                        await Task.Delay(_flushPeriod, _cancellationTokenSource.Token);
                    }
                    catch
                    {
                        // Exit if task was canceled
                        return;
                    }
                }
            }
        }

        private async Task WriteMessagesAsync(List<string> messages)
        {
            // Files are grouped by day, and written up to _maxFileSize before rolling to a new file
            DateTime today = DateTime.Now;
            var fullName = GetFullName(today);
            var fileInfo = new FileInfo(fullName);
            if (!TryCreateDirectory())
            {
                // return early if we fail to create the directory
                return;
            }
            var streamWriter = GetStreamWriter(fullName);

            foreach (var message in messages)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                fileInfo.Refresh();
                // Roll to new file if _maxFileSize is reached
                // _maxFileSize could be less than the length of the file header - in that case we still write the first log message before rolling
                if (fileInfo.Exists && fileInfo.Length > _maxFileSize)
                {
                    _fileNumber++;
                    fullName = GetFullName(today);
                    fileInfo = new FileInfo(fullName);
                    streamWriter.Dispose();
                    if (!TryCreateDirectory())
                    {
                        // return early if we fail to create the directory
                        return;
                    }
                    streamWriter = GetStreamWriter(fullName);
                }
                if (!fileInfo.Exists || fileInfo.Length == 0)
                {
                    await OnFirstWrite(streamWriter);
                }

                await WriteMessageAsync(message, streamWriter);
            }
            streamWriter.Dispose();

            RollFiles();
        }

        internal bool TryCreateDirectory()
        {
            if (!Directory.Exists(_path))
            {
                try
                {
                    Directory.CreateDirectory(_path);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.CreateDirectoryFailed(_logger, _path, ex);
                    return false;
                }
            }
            return true;
        }

        // Virtual for testing
        internal virtual async Task WriteMessageAsync(string message, StreamWriter streamWriter)
        {
            await streamWriter.WriteLineAsync(message);
            await streamWriter.FlushAsync();
        }

        // Virtual for testing
        internal virtual StreamWriter GetStreamWriter(string fileName)
        {
            return File.AppendText(fileName);
        }

        private void RollFiles()
        {
            if (_maxRetainedFiles > 0)
            {
                var files = new DirectoryInfo(_path)
                    .GetFiles(_fileName + "*")
                    .OrderByDescending(f => f.Name)
                    .Skip(_maxRetainedFiles.Value);

                foreach (var item in files)
                {
                    item.Delete();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Cancel();
            _messageQueue.CompleteAdding();
            _outputTask.Wait();
            await ValueTask.CompletedTask;
        }

        private string GetFullName(DateTime date)
        {
            return Path.Combine(_path, FormattableString.Invariant($"{_fileName}{date.Year:0000}{date.Month:00}{date.Day:00}.{_fileNumber}.txt"));
        }

        public virtual Task OnFirstWrite(StreamWriter streamWriter)
        {
            return Task.CompletedTask;
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _writeMessagesFailed =
                LoggerMessage.Define(
                    LogLevel.Debug,
                    new EventId(1, "WriteMessagesFailed"),
                    "FileLoggerProcessor failed to write all messages.");

            public static void WriteMessagesFailed(ILogger logger, Exception ex) => _writeMessagesFailed(logger, ex);

            private static readonly Action<ILogger, string, Exception> _createDirectoryFailed =
                LoggerMessage.Define<string>(
                    LogLevel.Debug,
                    new EventId(2, "CreateDirectoryFailed"),
                    "Failed to create directory {Path}.");

            public static void CreateDirectoryFailed(ILogger logger, string path, Exception ex) => _createDirectoryFailed(logger, path, ex);
        }
    }
}
