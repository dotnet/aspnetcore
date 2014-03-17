
namespace Microsoft.AspNet.Identity.Test
{
    public class TestUser : IUser<string>
    {
        public string Id { get; private set; }
        public string UserName { get; set; }
    }
}
