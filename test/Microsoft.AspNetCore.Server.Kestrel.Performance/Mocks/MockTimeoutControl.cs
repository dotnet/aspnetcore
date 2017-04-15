using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance.Mocks
{
    public class MockTimeoutControl : ITimeoutControl
    {
        public void CancelTimeout()
        {
        }

        public void ResetTimeout(long ticks, TimeoutAction timeoutAction)
        {
        }

        public void SetTimeout(long ticks, TimeoutAction timeoutAction)
        {
        }
    }
}
