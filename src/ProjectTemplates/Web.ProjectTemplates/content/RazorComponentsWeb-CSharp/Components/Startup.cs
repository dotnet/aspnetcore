using Microsoft.AspNetCore.Components.Builder;

namespace RazorComponentsWeb_CSharp.Components
{
    public class Startup
    {
        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
