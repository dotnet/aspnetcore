namespace Microsoft.HttpRepl.Commands
{
    public class PatchCommand : BaseHttpCommand
    {
        protected override string Verb => "patch";

        protected override bool RequiresBody => true;
    }
}
