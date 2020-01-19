using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace BlazorWasm_CSharp.Shared.Authorization
{
    public class UserInfo
    {
        public static readonly UserInfo Anonymous = new UserInfo();

        public bool IsAuthenticated { get; set; }

        public string NameClaimType { get; set; }

        public string RoleClaimType { get; set; }

        public ICollection<ClaimValue> Claims { get; set; }
    }
}
