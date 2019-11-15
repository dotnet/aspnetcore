using TestSite;

internal class StartupHook
{
    public static void Initialize()
    {
        Startup.StartupHookCalled = true;
    }
}
