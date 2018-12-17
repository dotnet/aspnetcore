// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal abstract class CommandBase : CommandLineApplication
    {
        public const int ExitCodeSuccess = 0;
        public const int ExitCodeFailure = 1;
        public const int ExitCodeFailureRazorError = 2;

        protected CommandBase(Application parent, string name)
            : base(throwOnUnexpectedArg: true)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            base.Parent = parent;
            Name = name;
            Out = parent.Out ?? Out;
            Error = parent.Error ?? Error;

            Help = HelpOption("-?|-h|--help");
            OnExecute((Func<Task<int>>)ExecuteAsync);
        }

        protected new Application Parent => (Application)base.Parent;

        protected CancellationToken Cancelled => Parent?.CancellationToken ?? default;

        protected CommandOption Help { get; }

        protected virtual bool ValidateArguments()
        {
            return true;
        }

        protected abstract Task<int> ExecuteCoreAsync();

        private async Task<int> ExecuteAsync()
        {
            if (!ValidateArguments())
            {
                ShowHelp();
                return ExitCodeFailureRazorError;
            }

            return await ExecuteCoreAsync();
        }
    }
}
