namespace Microsoft.HttpRepl.Commands
{
    public class PostCommand : BaseHttpCommand
    {
        protected override string Verb => "post";

        protected override bool RequiresBody => true;
    }
}
