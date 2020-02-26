// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public partial class Http3TestBase
    {
        internal class TestHttp3ControlStream : TestHttp3StreamBase
        {
            internal ConnectionContext StreamContext { get; }

            public long StreamId => 0;

            public Dictionary<long, long> SettingsDictionary { get; } = new Dictionary<long, long>();

            public TestHttp3ControlStream(Http3TestBase testBase, ConnectionContext context)
            {
                _testBase = testBase;
                StreamContext = context;
                _application = context.Transport;
            }

            internal async ValueTask<Dictionary<long, long>> ReceiveSettingsFrame()
            {
                var frame = await ReceiveFrameAsync();
                var payload = frame.PayloadSequence;

                while (true)
                {
                    var id = VariableLengthIntegerHelper.GetInteger(payload, out var consumed, out var examined);
                    if (id == -1)
                    {
                        break;
                    }

                    payload = payload.Slice(consumed);

                    var value = VariableLengthIntegerHelper.GetInteger(payload, out consumed, out examined);
                    if (id == -1)
                    {
                        break;
                    }

                    payload = payload.Slice(consumed);
                    SettingsDictionary[id] = value;
                }

                return SettingsDictionary;
            }

            public async ValueTask WriteStreamIdAsync(long id)
            {
                var writableBuffer = _application.Output;

                void WriteSpan(PipeWriter pw)
                {
                    var buffer = pw.GetSpan(sizeHint: 8);
                    var lengthWritten = VariableLengthIntegerHelper.WriteInteger(buffer, id);
                    pw.Advance(lengthWritten);
                }

                WriteSpan(writableBuffer);

                await FlushAsync(writableBuffer);
            }

            internal void VerifyGoAway(long expectedLastStreamId)
            {
                // Make sure stream has ended first.
                //Assert.True(_hasReceivedGoAway);
            }

            internal Task WriteSettings(IList<Http3PeerSetting> settings)
            {
                var frame = new Http3RawFrame();
                frame.PrepareSettings();

                frame.Length = Http3FrameWriter.GetSettingsLength(settings);
                Http3FrameWriter.WriteHeader(frame, _application.Output);
                var settingsBuffer = _application.Output.GetSpan((int)frame.Length);
                // TODO may want to modify behavior of input frames to mock different client behavior (client can send anything).
                Http3FrameWriter.WriteSettings(settings, settingsBuffer);
                _application.Output.Advance((int)frame.Length);

                return FlushAsync(_application.Output);
            }
        }
    }
}
