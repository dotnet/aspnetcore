using System.Diagnostics;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    internal class EditCommand : ICommand
    {
        public void Execute(CommandContext context)
        {
            Process.Start(context.SecretStore.SecretsFilePath);
        }

        internal static void Configure(CommandLineApplication command)
        {
            command.Description = "Opens secrets file in default editor";
            command.HelpOption();
        }
    }
}
