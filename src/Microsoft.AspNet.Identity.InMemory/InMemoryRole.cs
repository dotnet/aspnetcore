using System;

namespace Microsoft.AspNet.Identity.InMemory
{
    public class InMemoryRole : IRole<string>
    {
        public InMemoryRole(string roleName)
        {
            Id = Guid.NewGuid().ToString();
            Name = roleName;
        }

        public virtual string Id { get; set; }
        public virtual string Name { get; set; }
    }
}