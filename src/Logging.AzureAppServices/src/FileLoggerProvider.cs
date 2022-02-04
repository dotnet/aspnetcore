// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.AzureAppServices;

/// <summary>
/// A <see cref="BatchingLoggerProvider"/> which writes out to a file.
/// </summary>
[ProviderAlias("AzureAppServicesFile")]
public class FileLoggerProvider : BatchingLoggerProvider
{
    private readonly string _path;
    private readonly string _fileName;
    private readonly int? _maxFileSize;
    private readonly int? _maxRetainedFiles;

    /// <summary>
    /// Creates a new instance of <see cref="FileLoggerProvider"/>.
    /// </summary>
    /// <param name="options">The options to use when creating a provider.</param>
    [SuppressMessage("ApiDesign", "RS0022:Constructor make noninheritable base class inheritable", Justification = "Required for backwards compatibility")]
    public FileLoggerProvider(IOptionsMonitor<AzureFileLoggerOptions> options) : base(options)
    {
        var loggerOptions = options.CurrentValue;
        _path = loggerOptions.LogDirectory;
        _fileName = loggerOptions.FileName;
        _maxFileSize = loggerOptions.FileSizeLimit;
        _maxRetainedFiles = loggerOptions.RetainedFileCountLimit;
    }

    internal override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_path);

        foreach (var group in messages.GroupBy(GetGrouping))
        {
            var fullName = GetFullName(group.Key);
            var fileInfo = new FileInfo(fullName);
            if (_maxFileSize > 0 && fileInfo.Exists && fileInfo.Length > _maxFileSize)
            {
                return;
            }

            using (var streamWriter = File.AppendText(fullName))
            {
                foreach (var item in group)
                {
                    await streamWriter.WriteAsync(item.Message).ConfigureAwait(false);
                }
            }
        }

        RollFiles();
    }

    private string GetFullName((int Year, int Month, int Day) group)
    {
        return Path.Combine(_path, $"{_fileName}{group.Year:0000}{group.Month:00}{group.Day:00}.txt");
    }

    private (int Year, int Month, int Day) GetGrouping(LogMessage message)
    {
        return (message.Timestamp.Year, message.Timestamp.Month, message.Timestamp.Day);
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
}
