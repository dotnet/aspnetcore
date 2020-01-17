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
            IsAuthenticated = identity.AuthenticationType != null;
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
                var nameClaims = claimsPrincipal.FindAll(NameClaimType);
                foreach (var claim in nameClaims)
                {
                    claims.Add(new ClaimValue(NameClaimType, claim.Value));
                }
                
                // Uncomment this code if you want to send additional claims to the client.
                //foreach (var claim in claimsPrincipal.Claims.Except(nameClaims))
                //{
                //    claims.Add(new ClaimValue(claim.Type, claim.Value));
                //}

                Claims = claims;
            }
        }

        public bool IsAuthenticated { get; set; }

        public string NameClaimType { get; set; }

        public string RoleClaimType { get; set; }

        public ICollection<ClaimValue> Claims { get; set; }
    }
}
