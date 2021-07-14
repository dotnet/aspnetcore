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
        private int _fileNumber;
        private bool _maxFilesReached;
        private TimeSpan _flushInterval;
        private DateTime _today = DateTime.Now;

        private readonly IOptionsMonitor<W3CLoggerOptions> _options;
        
        private readonly ILogger _logger;
        private readonly List<string> _currentBatch = new List<string>();
        private readonly Task _outputTask;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly object _pathLock = new object();

        internal readonly BlockingCollection<string> _messageQueue = new BlockingCollection<string>(_maxQueuedMessages);

        public FileLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory)
        {
            _options = options;
            var loggerOptions = _options.CurrentValue;

            _path = loggerOptions.LogDirectory;
            // If user supplies no LogDirectory, default to {ContentRoot}/logs.
            // If user supplies a relative path, use {ContentRoot}/{LogDirectory}.
            // If user supplies a full path, use that.
            if (string.IsNullOrEmpty(_path))
            {
                _path = Path.Join(environment.ContentRootPath, "logs");
            }
            else if (!Path.IsPathRooted(_path))
            {
                _path = Path.Join(environment.ContentRootPath, _path);
            }

            _fileName = loggerOptions.FileName;
            _maxFileSize = loggerOptions.FileSizeLimit;
            _maxRetainedFiles = loggerOptions.RetainedFileCountLimit;
            _flushInterval = loggerOptions.FlushInterval;
            _options.OnChange(options =>
            {
                lock (_pathLock)
                {
                    // Clear the cached settings.
                    loggerOptions = options;
                    if (!string.IsNullOrEmpty(loggerOptions.LogDirectory))
                    {
                        _path = loggerOptions.LogDirectory;
                    }
                    _fileName = loggerOptions.FileName;
                    _maxFileSize = loggerOptions.FileSizeLimit;
                    _maxRetainedFiles = loggerOptions.RetainedFileCountLimit;
                    _flushInterval = loggerOptions.FlushInterval;
                }
            });

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
                        await WriteMessagesAsync(_currentBatch, _cancellationTokenSource.Token);
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
                        await Task.Delay(_flushInterval, _cancellationTokenSource.Token);
                    }
                    catch
                    {
                        // Exit if task was canceled
                        return;
                    }
                }
            }
        }

        private async Task WriteMessagesAsync(List<string> messages, CancellationToken cancellationToken)
        {
            // Files are written up to _maxFileSize before rolling to a new file
            DateTime today = DateTime.Now;
            var fullName = GetFullName(today);
            if (_maxFilesReached)
            {
                // Return early if we've already logged that today's file limit has been reached.
                // Need to do this check after the call to GetFullName(), since it resets _maxFilesReached
                // when a new day starts.
                return;
            }
            var fileInfo = new FileInfo(fullName);

            if (!TryCreateDirectory())
            {
                // return early if we fail to create the directory
                return;
            }
            var streamWriter = GetStreamWriter(fullName);

            try
            {
                foreach (var message in messages)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    fileInfo.Refresh();
                    // Roll to new file if _maxFileSize is reached
                    // _maxFileSize could be less than the length of the file header - in that case we still write the first log message before rolling.
                    // Previous instance of FileLoggerProcessor could have already written files to the same directory.
                    // Loop through them until we find what the current _fileNumber should be.
                    if (fileInfo.Exists && fileInfo.Length > _maxFileSize)
                    {
                        streamWriter.Dispose();
                        do
                        {
                            _fileNumber++;
                            if (_fileNumber >= W3CLoggerOptions.MaxFileCount)
                            {
                                streamWriter = null;
                                _maxFilesReached = true;
                                // Return early if log directory is already full
                                Log.MaxFilesReached(_logger, new ApplicationException());
                                return;
                            }
                            fullName = GetFullName(today);
                            fileInfo = new FileInfo(fullName);
                        }
                        while (fileInfo.Exists && fileInfo.Length > _maxFileSize);
                        if (!TryCreateDirectory())
                        {
                            streamWriter = null;
                            // return early if we fail to create the directory
                            return;
                        }
                        streamWriter = GetStreamWriter(fullName);
                    }
                    if (!fileInfo.Exists || fileInfo.Length == 0)
                    {
                        await OnFirstWrite(streamWriter, cancellationToken);
                    }

                    await WriteMessageAsync(message, streamWriter, cancellationToken);
                }
            }
            finally
            {
                RollFiles();
                streamWriter?.Dispose();
            }

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
        internal virtual async Task WriteMessageAsync(string message, StreamWriter streamWriter, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            await streamWriter.WriteLineAsync(message.AsMemory(), cancellationToken);
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
                lock (_pathLock)
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
        }

        public async ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Cancel();
            _messageQueue.CompleteAdding();
            await _outputTask;
        }

        private string GetFullName(DateTime date)
        {
            lock (_pathLock)
            {
                if ((date.Date - _today.Date).Days != 0)
                {
                    _today = date;
                    _fileNumber = 0;
                    _maxFilesReached = false;
                }
                return Path.Combine(_path, FormattableString.Invariant($"{_fileName}{date.Year:0000}{date.Month:00}{date.Day:00}.{_fileNumber:0000}.txt"));
            }
        }

        public virtual Task OnFirstWrite(StreamWriter streamWriter, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _writeMessagesFailed =
                LoggerMessage.Define(
                    LogLevel.Debug,
                    new EventId(1, "WriteMessagesFailed"),
                    "Failed to write all messages.");

            public static void WriteMessagesFailed(ILogger logger, Exception ex) => _writeMessagesFailed(logger, ex);

            private static readonly Action<ILogger, string, Exception> _createDirectoryFailed =
                LoggerMessage.Define<string>(
                    LogLevel.Debug,
                    new EventId(2, "CreateDirectoryFailed"),
                    "Failed to create directory {Path}.");

            public static void CreateDirectoryFailed(ILogger logger, string path, Exception ex) => _createDirectoryFailed(logger, path, ex);

            private static readonly Action<ILogger, Exception> _maxFilesReached =
                LoggerMessage.Define(
                    LogLevel.Warning,
                    new EventId(3, "MaxFilesReached"),
                    "Limit of 10,000 files per day has been reached");

            public static void MaxFilesReached(ILogger logger, Exception ex) => _maxFilesReached(logger, ex);
        }
    }

}
