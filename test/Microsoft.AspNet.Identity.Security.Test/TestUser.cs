namespace Microsoft.AspNet.Identity.Security.Test
{
    public class TestUser : TestUser<string>
    {
    }

    public class TestUser<TKey> : IUser<TKey>
    {
        public TKey Id { get; private set; }
        public string UserName { get; set; }
    }
}