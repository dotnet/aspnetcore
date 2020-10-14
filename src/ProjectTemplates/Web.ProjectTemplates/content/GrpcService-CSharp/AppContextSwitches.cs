using System;

namespace GrpcService_CSharp
{
    public static class AppContextSwitches
    {
        public static void Apply()
        {
            AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", true);
        }
    }
}
