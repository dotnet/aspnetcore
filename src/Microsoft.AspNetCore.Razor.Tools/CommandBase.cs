// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.TagHelperTool
{
    internal abstract class CommandBase : CommandLineApplication
    {
        protected CommandBase(Application parent, string name)
            : base(throwOnUnexpectedArg: true)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            base.Parent = parent;
            Name = name;

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
                return 1;
            }

            return await ExecuteCoreAsync();
        }
    }
}
