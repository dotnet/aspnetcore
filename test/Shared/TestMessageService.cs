using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity.Test
{
    public class TestMessageService : IIdentityMessageService
    {
        public IdentityMessage Message { get; set; }

        public Task SendAsync(IdentityMessage message, CancellationToken cancellationToken = default(CancellationToken))
        {
            Message = message;
            return Task.FromResult(0);
        }
    }

}