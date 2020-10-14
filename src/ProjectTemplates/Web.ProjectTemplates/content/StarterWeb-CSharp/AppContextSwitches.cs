using System;

namespace Company.WebApplication1
{
    public static class AppContextSwitches
    {
        public static void Apply()
        {
            AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", true);
        }
    }
}
