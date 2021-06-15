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
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal class FileLoggerProcessor : IDisposable
    {
        private const int _maxQueuedMessages = 1024;

        private readonly string _path;
        private readonly string _fileName;
        private readonly int? _maxFileSize;
        private readonly int? _maxRetainedFiles;
        private int _fileNumber = 1;

        private readonly BlockingCollection<LogMessage> _messageQueue = new BlockingCollection<LogMessage>(_maxQueuedMessages);
        private readonly List<LogMessage> _currentBatch = new List<LogMessage>();
        private Task _outputTask;
        private CancellationTokenSource _cancellationTokenSource;

        public FileLoggerProcessor(IOptionsMonitor<FileLoggerOptions> options)
        {
            var loggerOptions = options.CurrentValue;
            _path = loggerOptions.LogDirectory;
            _fileName = loggerOptions.FileName;
            _maxFileSize = loggerOptions.FileSizeLimit;
            _maxRetainedFiles = loggerOptions.RetainedFileCountLimit;

            // Start message queue processor
            _cancellationTokenSource = new CancellationTokenSource();
            _outputTask = Task.Run(ProcessLogQueue);
        }

        public void EnqueueMessage(LogMessage message)
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
            Directory.CreateDirectory(_path);
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
                    catch
                    {
                        // ignored
                    }

                    _currentBatch.Clear();
                }
            }
        }

        private async Task WriteMessagesAsync(List<LogMessage> messages)
        {
            // Files are grouped by day, and written up to _maxFileSize before rolling to a new file
            foreach (var group in messages.GroupBy(GetGrouping))
            {
                var fullName = GetFullName(group.Key);
                var fileInfo = new FileInfo(fullName);
                var streamWriter = File.AppendText(fullName);

                foreach (var item in group)
                {
                    fileInfo.Refresh();
                    // Roll to new file if _maxFileSize is reached
                    // _maxFileSize could be less than the length of the file header - in that case we still write the first log message before rolling
                    if (fileInfo.Length > _maxFileSize)
                    {
                        _fileNumber++;
                        fullName = GetFullName(group.Key);
                        fileInfo = new FileInfo(fullName);
                        streamWriter.Dispose();
                        streamWriter = File.AppendText(fullName);
                    }
                    if (fileInfo.Length == 0)
                    {
                        await OnFirstWrite(streamWriter);
                    }

                    await WriteMessageAsync(item.Message, streamWriter);
                }
                streamWriter.Dispose();
            }

            RollFiles();
        }

        internal async Task WriteMessageAsync(string message, StreamWriter streamWriter)
        {
            await streamWriter.WriteLineAsync(message);
            await streamWriter.FlushAsync();
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

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _messageQueue.CompleteAdding();
            _outputTask.Wait();
        }

        private string GetFullName(DateOnly group)
        {
            return Path.Combine(_path, $"{_fileName}{group.Year:0000}{group.Month:00}{group.Day:00}{_fileNumber:00}.txt");
        }

        private static DateOnly GetGrouping(LogMessage message)
        {
            return new DateOnly(message.Timestamp.Year, message.Timestamp.Month, message.Timestamp.Day);
        }

        public virtual Task OnFirstWrite(StreamWriter streamWriter)
        {
            return Task.CompletedTask;
        }
    }
}
