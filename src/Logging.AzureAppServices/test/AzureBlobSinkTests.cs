// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test;

public class AzureBlobSinkTests
{
    DateTimeOffset _timestampOne = new DateTimeOffset(2016, 05, 04, 03, 02, 01, TimeSpan.Zero);

    [Fact]
    public async Task WritesMessagesInBatches()
    {
        var blob = new Mock<ICloudAppendBlob>();
        var buffers = new List<byte[]>();
        blob.Setup(b => b.AppendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .Callback((ArraySegment<byte> s, CancellationToken ct) => buffers.Add(ToArray(s)))
            .Returns(Task.CompletedTask);

        var sink = new TestBlobSink(name => blob.Object);
        var logger = (BatchingLogger)sink.CreateLogger("Cat");

        await sink.IntervalControl.Pause;

        for (int i = 0; i < 5; i++)
        {
            logger.Log(_timestampOne, LogLevel.Information, 0, "Text " + i, null, (state, ex) => state);
        }

        sink.IntervalControl.Resume();
        await sink.IntervalControl.Pause;

        Assert.Single(buffers);
        Assert.Equal(
            "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Text 0" + Environment.NewLine +
            "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Text 1" + Environment.NewLine +
            "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Text 2" + Environment.NewLine +
            "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Text 3" + Environment.NewLine +
            "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Text 4" + Environment.NewLine,
            Encoding.UTF8.GetString(buffers[0]));
    }

    [Fact]
    public async Task GroupsByHour()
    {
        var blob = new Mock<ICloudAppendBlob>();
        var buffers = new List<byte[]>();
        var names = new List<string>();

        blob.Setup(b => b.AppendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .Callback((ArraySegment<byte> s, CancellationToken ct) => buffers.Add(ToArray(s)))
            .Returns(Task.CompletedTask);

        var sink = new TestBlobSink(name =>
        {
            names.Add(name);
            return blob.Object;
        });
        var logger = (BatchingLogger)sink.CreateLogger("Cat");

        await sink.IntervalControl.Pause;

        var startDate = _timestampOne;
        for (int i = 0; i < 3; i++)
        {
            logger.Log(startDate, LogLevel.Information, 0, "Text " + i, null, (state, ex) => state);

            startDate = startDate.AddHours(1);
        }

        sink.IntervalControl.Resume();
        await sink.IntervalControl.Pause;

        Assert.Equal(3, buffers.Count);

        Assert.Equal("appname/2016/05/04/03/42_filename", names[0]);
        Assert.Equal("appname/2016/05/04/04/42_filename", names[1]);
        Assert.Equal("appname/2016/05/04/05/42_filename", names[2]);
    }

    private byte[] ToArray(ArraySegment<byte> inputStream)
    {
        return inputStream.Array
            .Skip(inputStream.Offset)
            .Take(inputStream.Count)
            .ToArray();
    }
}
