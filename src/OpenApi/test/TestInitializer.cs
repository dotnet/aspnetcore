using System.Runtime.CompilerServices;

public static class TestInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Verifier.UseProjectRelativeDirectory("snapshots");
        VerifierSettings.AutoVerify();
    }
}
