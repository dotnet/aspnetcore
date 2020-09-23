namespace blazor31
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Just make sure we have a reference to the MvcShim
            var t = typeof(Microsoft.AspNetCore.Components.ComponentBase);
            System.Console.WriteLine(t.FullName);
        }
    }
}
