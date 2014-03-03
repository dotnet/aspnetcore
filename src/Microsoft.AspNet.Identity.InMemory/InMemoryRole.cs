using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;

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