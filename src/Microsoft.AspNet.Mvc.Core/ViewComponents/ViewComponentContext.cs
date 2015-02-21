// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class ViewComponentContext
    {
        public ViewComponentContext([NotNull] TypeInfo componentType, [NotNull] ViewContext viewContext,
            [NotNull] TextWriter writer)
        {
            ComponentType = componentType;
            ViewContext = viewContext;
            Writer = writer;
        }

        public TypeInfo ComponentType { get; private set; }

        public ViewContext ViewContext { get; private set; }

        public TextWriter Writer { get; private set; }
    }
}
