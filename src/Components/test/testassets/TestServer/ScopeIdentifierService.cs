using System;

namespace TestServer
{
    public class ScopeIdentifierService
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }
}
