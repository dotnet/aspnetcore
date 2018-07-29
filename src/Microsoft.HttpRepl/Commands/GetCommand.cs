namespace Microsoft.HttpRepl.Commands
{
    public class GetCommand : BaseHttpCommand
    {
        protected override string Verb => "get";

        protected override bool RequiresBody => false;
    }
}
