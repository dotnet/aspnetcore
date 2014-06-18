// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace InlineConstraints
{
    public class DefaultCommandLineArgumentBuilder : ICommandLineArgumentBuilder
    {
        private readonly List<string> _args = new List<string>();

        public void AddArgument(string arg)
        {
            _args.Add(arg);
        }

        public IEnumerable<string> Build()
        {
            return _args;
        }
    }
}