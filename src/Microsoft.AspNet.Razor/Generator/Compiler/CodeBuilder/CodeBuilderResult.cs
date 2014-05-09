// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeBuilderResult
    {
        public CodeBuilderResult(string code, IList<LineMapping> designTimeLineMappings)
        {
            Code = code;
            DesignTimeLineMappings = designTimeLineMappings;
        }

        public string Code { get; private set; }
        public IList<LineMapping> DesignTimeLineMappings { get; private set; }
    }
}
