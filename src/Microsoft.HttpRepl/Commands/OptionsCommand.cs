namespace Microsoft.HttpRepl.Commands
{
    public class OptionsCommand : BaseHttpCommand
    {
        protected override string Verb => "options";

        protected override bool RequiresBody => false;
    }
}
