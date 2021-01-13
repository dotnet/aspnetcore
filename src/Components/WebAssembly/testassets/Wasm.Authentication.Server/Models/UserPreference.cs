using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Wasm.Authentication.Server.Models
{
    public class UserPreference
    {
        public string Id { get; set; }

        public string ApplicationUserId { get; set; }

        public string Color { get; set; }
    }
}
