// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class InjectChunk : Chunk
    {
        /// <summary>
        /// Represents the chunk for an @inject statement.
        /// </summary>
        /// <param name="typeName">The type of object that would be injected</param>
        /// <param name="propertyName">The member name the field is exposed to the page as.</param>
        public InjectChunk(string typeName,
                           string propertyName)
        {
            TypeName = typeName;
            MemberName = propertyName;
        }

        public string TypeName { get; private set; }

        public string MemberName { get; private set; }
    }
}