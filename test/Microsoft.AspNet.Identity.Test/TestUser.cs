using System;

namespace Microsoft.AspNet.Identity.Test
{
    public class TestUser : TestUser<string>
    {
        public TestUser()
        {
            Id = Guid.NewGuid().ToString();
        }
    }

    public class TestUser<TKey> : IUser<TKey>
    {
        public TKey Id { get; set; }
        public string UserName { get; set; }
    }
}