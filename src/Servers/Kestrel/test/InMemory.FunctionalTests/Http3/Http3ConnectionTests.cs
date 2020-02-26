using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http3ConnectionTests : Http3TestBase
    {
        [Fact]
        public async Task VerifySettingsAreReceived()
        {
            await InitializeConnectionAsync(_noopApplication);
            await CreateOutboundControlStream(ControlStreamId);
            await CreateOutboundControlStream(EncoderStreamId);
            await CreateOutboundControlStream(DecoderStreamId);
            await WaitForInboundControlStreamCreated();

            var settings = await ReadSettings();
            Assert.Equal(new KestrelServerLimits().MaxRequestHeadersTotalSize, settings[(long)Http3SettingType.MaxHeaderListSize]);
        }

        [Fact]
        public async Task VerifyDefaultSettingsAreSent()
        {
            var settings = new Http3PeerSettings();
            settings.MaxHeaderListSize = 10;

            await InitializeConnectionAsync(_noopApplication);
            await CreateOutboundControlStream(ControlStreamId);

            await WriteSettings(settings.GetNonProtocolDefaults());

            await CreateOutboundControlStream(EncoderStreamId);
            await CreateOutboundControlStream(DecoderStreamId);
            await WaitForInboundControlStreamCreated();
        }

        // Idle timeout from transport
        //
        // send settings, verify they are updated?

        // server sending GOAWAY

    }
}
