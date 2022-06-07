using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EmptyBlazorServerWeb_CSharp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages();

        builder.Services.AddServerSideBlazor();
       
        var app = builder.Build();

        app.UseStaticFiles();

        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}
