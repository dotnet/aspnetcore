
namespace AppWithP2PReference
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Just make sure we have a reference to the MvcShim
            var t = typeof(Microsoft.AspNetCore.Mvc.IActionResult);
            System.Console.WriteLine(t.FullName);
        }
    }
}
