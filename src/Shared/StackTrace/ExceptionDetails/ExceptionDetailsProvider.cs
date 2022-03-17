// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.Extensions.StackTrace.Sources;

internal class ExceptionDetailsProvider
{
    private readonly IFileProvider _fileProvider;
    private readonly ILogger? _logger;
    private readonly int _sourceCodeLineCount;

    public ExceptionDetailsProvider(IFileProvider fileProvider, ILogger? logger, int sourceCodeLineCount)
    {
        _fileProvider = fileProvider;
        _logger = logger;
        _sourceCodeLineCount = sourceCodeLineCount;
    }

    public IEnumerable<ExceptionDetails> GetDetails(Exception exception)
    {
        var exceptions = FlattenAndReverseExceptionTree(exception);

        foreach (var ex in exceptions)
        {
            yield return new ExceptionDetails(ex, GetStackFrames(ex));
        }
    }

    private IEnumerable<StackFrameSourceCodeInfo> GetStackFrames(Exception original)
    {
        var stackFrames = StackTraceHelper.GetFrames(original, out var exception)
            .Select(frame => GetStackFrameSourceCodeInfo(
                frame.MethodDisplayInfo?.ToString(),
                frame.FilePath,
                frame.LineNumber));

        if (exception != null)
        {
            _logger?.FailedToReadStackTraceInfo(exception);
        }

        return stackFrames;
    }

    private static IEnumerable<Exception> FlattenAndReverseExceptionTree(Exception? ex)
    {
        // ReflectionTypeLoadException is special because the details are in
        // the LoaderExceptions property
        var typeLoadException = ex as ReflectionTypeLoadException;
        if (typeLoadException != null)
        {
            var typeLoadExceptions = new List<Exception>();
            foreach (var loadException in typeLoadException.LoaderExceptions)
            {
                typeLoadExceptions.AddRange(FlattenAndReverseExceptionTree(loadException));
            }

            typeLoadExceptions.Add(typeLoadException);
            return typeLoadExceptions;
        }

        var list = new List<Exception>();
        if (ex is AggregateException aggregateException)
        {
            list.Add(ex);
            foreach (var innerException in aggregateException.Flatten().InnerExceptions)
            {
                list.Add(innerException);
            }
        }
        else
        {
            while (ex != null)
            {
                list.Add(ex);
                ex = ex.InnerException;
            }
            list.Reverse();
        }

        return list;
    }

    // make it internal to enable unit testing
    internal StackFrameSourceCodeInfo GetStackFrameSourceCodeInfo(string? method, string? filePath, int lineNumber)
    {
        var stackFrame = new StackFrameSourceCodeInfo
        {
            Function = method,
            File = filePath,
            Line = lineNumber
        };

        if (string.IsNullOrEmpty(stackFrame.File))
        {
            return stackFrame;
        }

        IEnumerable<string>? lines = null;
        if (File.Exists(stackFrame.File))
        {
            lines = File.ReadLines(stackFrame.File);
        }
        else
        {
            // Handle relative paths and embedded files
            var fileInfo = _fileProvider.GetFileInfo(stackFrame.File);
            if (fileInfo.Exists)
            {
                // ReadLines doesn't accept a stream. Use ReadLines as its more efficient
                // relative to reading lines via stream reader
                if (!string.IsNullOrEmpty(fileInfo.PhysicalPath))
                {
                    lines = File.ReadLines(fileInfo.PhysicalPath);
                }
                else
                {
                    lines = ReadLines(fileInfo);
                }
            }
        }

        if (lines != null)
        {
            ReadFrameContent(stackFrame, lines, stackFrame.Line, stackFrame.Line);
        }

        return stackFrame;
    }

    // make it internal to enable unit testing
    internal void ReadFrameContent(
        StackFrameSourceCodeInfo frame,
        IEnumerable<string> allLines,
        int errorStartLineNumberInFile,
        int errorEndLineNumberInFile)
    {
        // Get the line boundaries in the file to be read and read all these lines at once into an array.
        var preErrorLineNumberInFile = Math.Max(errorStartLineNumberInFile - _sourceCodeLineCount, 1);
        var postErrorLineNumberInFile = errorEndLineNumberInFile + _sourceCodeLineCount;
        var codeBlock = allLines
            .Skip(preErrorLineNumberInFile - 1)
            .Take(postErrorLineNumberInFile - preErrorLineNumberInFile + 1)
            .ToArray();

        var numOfErrorLines = (errorEndLineNumberInFile - errorStartLineNumberInFile) + 1;
        var errorStartLineNumberInArray = errorStartLineNumberInFile - preErrorLineNumberInFile;

        frame.PreContextLine = preErrorLineNumberInFile;
        frame.PreContextCode = codeBlock.Take(errorStartLineNumberInArray).ToArray();
        frame.ContextCode = codeBlock
            .Skip(errorStartLineNumberInArray)
            .Take(numOfErrorLines)
            .ToArray();
        frame.PostContextCode = codeBlock
            .Skip(errorStartLineNumberInArray + numOfErrorLines)
            .ToArray();
    }

    private static IEnumerable<string> ReadLines(IFileInfo fileInfo)
    {
        using (var reader = new StreamReader(fileInfo.CreateReadStream()))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}
