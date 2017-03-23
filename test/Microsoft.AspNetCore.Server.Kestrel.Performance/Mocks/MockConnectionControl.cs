// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class MockConnectionControl : IConnectionControl
    {
        public void CancelTimeout() { }
        public void End(ProduceEndType endType) { }
        public void Pause() { }
        public void ResetTimeout(long milliseconds, TimeoutAction timeoutAction) { }
        public void Resume() { }
        public void SetTimeout(long milliseconds, TimeoutAction timeoutAction) { }
    }
}
