using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wasm.Authentication.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        public UserPreference UserPreference { get; set; }
    }
}
