using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace BlazorWasm_CSharp.Shared.Models
{
    public class UserInfo
    {
        public static readonly UserInfo Anonymous = new UserInfo();

        public UserInfo() { }

        public UserInfo(ClaimsPrincipal claimsPrincipal)
        {
            var identity = claimsPrincipal.Identity;
            AuthenticationType = identity.AuthenticationType;
            if (identity is ClaimsIdentity claimsIdentity)
            {
                NameClaimType = claimsIdentity.NameClaimType;
                RoleClaimType = claimsIdentity.RoleClaimType;
            }
            else
            {
                NameClaimType = "name";
                RoleClaimType = "roles";
            }

            if (claimsPrincipal.Claims.Any())
            {
                var claims = new List<ClaimValue>();
                foreach (var claim in claimsPrincipal.Claims)
                {
                    claims.Add(new ClaimValue(claim.Type, claim.Value));
                }

                Claims = claims;
            }
        }

        public string AuthenticationType { get; set; }

        public string NameClaimType { get; set; }

        public string RoleClaimType { get; set; }

        public ICollection<ClaimValue> Claims { get; set; }
    }
}
