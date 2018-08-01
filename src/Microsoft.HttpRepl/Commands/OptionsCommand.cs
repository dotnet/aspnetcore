// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.HttpRepl.Commands
{
    public class OptionsCommand : BaseHttpCommand
    {
        protected override string Verb => "options";

        protected override bool RequiresBody => false;
    }
}
