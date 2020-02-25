using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http3ConnectionTests : Http3TestBase
    {
        [Fact]
        public async Task VerifySettingsAreSentAndReceived()
        {
            await InitializeConnectionAsync(_noopApplication);
            await CreateOutboundControlStream(ControlStreamId);
            await CreateOutboundControlStream(EncoderStreamId);
            await CreateOutboundControlStream(DecoderStreamId);
            await WaitForInboundControlStreamCreated();
            await ReadSettings();
        }

        // Idle timeout from transport
        //
        // send settings, verify they are updated?

        // server sending GOAWAY

    }
}
