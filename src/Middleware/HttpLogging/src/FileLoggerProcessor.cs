// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging;

internal partial class FileLoggerProcessor : IAsyncDisposable
{
    private const int _maxQueuedMessages = 1024;

    private string _path;
    private string _fileName;
    private int? _maxFileSize;
    private int? _maxRetainedFiles;
    private int _fileNumber;
    private bool _maxFilesReached;
    private TimeSpan _flushInterval;
    private W3CLoggingFields _fields;
    private DateTime _today;
    private bool _firstFile = true;

    private readonly IOptionsMonitor<W3CLoggerOptions> _options;
    private readonly BlockingCollection<string> _messageQueue = new BlockingCollection<string>(_maxQueuedMessages);
    private readonly ILogger _logger;
    private readonly List<string> _currentBatch = new List<string>();
    private readonly Task _outputTask;
    private readonly CancellationTokenSource _cancellationTokenSource;

    // Internal to allow for testing
    internal ISystemDateTime SystemDateTime { get; set; } = new SystemDateTime();

    private readonly object _pathLock = new object();
    private ISet<string> _additionalHeaders;

    public FileLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory)
    {
        _logger = factory.CreateLogger(typeof(FileLoggerProcessor));

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
        _fields = loggerOptions.LoggingFields;
        _additionalHeaders = W3CLoggerOptions.FilterRequestHeaders(loggerOptions);

        _options.OnChange(options =>
        {
            lock (_pathLock)
            {
                // Clear the cached settings.
                loggerOptions = options;

                // Move to a new file if the fields have changed
                if (_fields != loggerOptions.LoggingFields || !_additionalHeaders.SetEquals(loggerOptions.AdditionalRequestHeaders))
                {
                    _fileNumber++;
                    if (_fileNumber >= W3CLoggerOptions.MaxFileCount)
                    {
                        _maxFilesReached = true;
                        Log.MaxFilesReached(_logger);
                    }
                    _fields = loggerOptions.LoggingFields;
                    _additionalHeaders = W3CLoggerOptions.FilterRequestHeaders(loggerOptions);
                }

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

        _today = SystemDateTime.Now;

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
        DateTime today = SystemDateTime.Now;

        if (!TryCreateDirectory())
        {
            // return early if we fail to create the directory
            return;
        }

        var fullName = GetFullName(today);
        // Don't write to an incomplete file left around by a previous FileLoggerProcessor
        if (_firstFile)
        {
            _fileNumber = GetFirstFileCount(today);
            fullName = GetFullName(today);
            if (_fileNumber >= W3CLoggerOptions.MaxFileCount)
            {
                _maxFilesReached = true;
                // Return early if log directory is already full
                Log.MaxFilesReached(_logger);
                return;
            }
        }

        _firstFile = false;
        if (_maxFilesReached)
        {
            // Return early if we've already logged that today's file limit has been reached.
            // Need to do this check after the call to GetFullName(), since it resets _maxFilesReached
            // when a new day starts.
            return;
        }
        var fileInfo = new FileInfo(fullName);
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
                if (fileInfo.Exists && fileInfo.Length > _maxFileSize)
                {
                    streamWriter.Dispose();
                    _fileNumber++;
                    if (_fileNumber >= W3CLoggerOptions.MaxFileCount)
                    {
                        streamWriter = null;
                        _maxFilesReached = true;
                        // Return early if log directory is already full
                        Log.MaxFilesReached(_logger);
                        return;
                    }
                    fullName = GetFullName(today);
                    fileInfo = new FileInfo(fullName);
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
        await streamWriter.FlushAsync(cancellationToken);
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

    private int GetFirstFileCount(DateTime date)
    {
        lock (_pathLock)
        {
            var searchString = FormattableString.Invariant($"{_fileName}{date.Year:0000}{date.Month:00}{date.Day:00}.*.txt");
            var files = new DirectoryInfo(_path)
                .GetFiles(searchString);

            return files.Length == 0
                ? 0
                : files
                    .Max(x => int.TryParse(x.Name.Split('.').ElementAtOrDefault(Index.FromEnd(2)), out var parsed)
                        ? parsed + 1
                        : 0);
        }
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

    private static partial class Log
    {

        [LoggerMessage(1, LogLevel.Debug, "Failed to write all messages.", EventName = "WriteMessagesFailed")]
        public static partial void WriteMessagesFailed(ILogger logger, Exception ex);

        [LoggerMessage(2, LogLevel.Debug, "Failed to create directory {Path}.", EventName = "CreateDirectoryFailed")]
        public static partial void CreateDirectoryFailed(ILogger logger, string path, Exception ex);

        [LoggerMessage(3, LogLevel.Warning, "Limit of 10000 files per day has been reached", EventName = "MaxFilesReached")]
        public static partial void MaxFilesReached(ILogger logger);
    }
}
