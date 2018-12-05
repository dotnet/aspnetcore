using Microsoft.AspNetCore.Identity;

namespace IdentitySample.DefaultUI.Data
{
    public class ApplicationUser : IdentityUser
    {
        [ProtectedPersonalData]
        public string Name { get; set; }
        [PersonalData]
        public int Age { get; set; }
    }
}
