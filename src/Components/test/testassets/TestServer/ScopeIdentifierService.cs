using System;

namespace TestServer
{
    internal class ScopeIdentifierService
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }
}
