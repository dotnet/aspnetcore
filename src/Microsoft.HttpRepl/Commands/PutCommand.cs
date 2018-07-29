namespace Microsoft.HttpRepl.Commands
{
    public class PutCommand : BaseHttpCommand
    {
        protected override string Verb => "put";

        protected override bool RequiresBody => true;
    }
}
