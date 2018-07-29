namespace Microsoft.HttpRepl.Commands
{
    public class DeleteCommand : BaseHttpCommand
    {
        protected override string Verb => "delete";

        protected override bool RequiresBody => true;
    }
}
