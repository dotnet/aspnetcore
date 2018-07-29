namespace Microsoft.HttpRepl.Commands
{
    public class HeadCommand : BaseHttpCommand
    {
        protected override string Verb => "head";

        protected override bool RequiresBody => false;
    }
}
