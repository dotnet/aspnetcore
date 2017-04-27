// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Cli.CommandLine
{
    internal class LocalizableStrings
    {
        public const string LastArgumentMultiValueError = "The last argument '{0}' accepts multiple values. No more argument can be added.";

        public const string OptionRequiresSingleValueWhichIsMissing = "Required value for option '{0}' was not provided.";

        public const string UnexpectedValueForOptionError = "Unexpected value '{0}' for option '{1}'";

        public const string UnexpectedArgumentError = "Unrecognized {0} '{1}'";

        public const string ResponseFileNotFoundError = "Response file '{0}' doesn't exist.";

        public const string ShowHelpInfo = "Show help information";

        public const string ShowVersionInfo = "Show version information";

        public const string ShowHintInfo = "Specify --{0} for a list of available options and commands.";

        public const string UsageHeader = "Usage:";

        public const string UsageArgumentsToken = " [arguments]";

        public const string UsageArgumentsHeader = "Arguments:";

        public const string UsageOptionsToken = " [options]";

        public const string UsageOptionsHeader = "Options:";

        public const string UsageCommandToken = " [command]";

        public const string UsageCommandsHeader = "Commands:";

        public const string UsageCommandsDetailHelp = "Use \"{0} [command] --help\" for more information about a command.";

        public const string UsageCommandArgs = " [args]";

        public const string UsageCommandAdditionalArgs = " [[--] <additional arguments>...]]";

        public const string UsageCommandsAdditionalArgsHeader = "Additional Arguments:";

        public const string InvalidTemplateError = "Invalid template pattern '{0}'";

        public const string MSBuildAdditionalArgsHelpText = "Any extra options that should be passed to MSBuild. See 'dotnet msbuild -h' for available options.";
    }
}
