
namespace Microsoft.AspNet.Identity.Test
{
    public class TestRole : IRole<string>
    {
        public string Id { get; private set; }
        public string Name { get; set; }
    }
}
