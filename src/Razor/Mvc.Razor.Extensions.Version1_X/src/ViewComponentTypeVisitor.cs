// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X
{
    internal class ViewComponentTypeVisitor : SymbolVisitor
    {
        private static readonly Version SupportedVCTHMvcVersion = new Version(1, 1);

        private readonly INamedTypeSymbol _viewComponentAttribute;
        private readonly INamedTypeSymbol _nonViewComponentAttribute;
        private readonly List<INamedTypeSymbol> _results;

        public static ViewComponentTypeVisitor Create(Compilation compilation, List<INamedTypeSymbol> results)
        {
            var vcAttribute = compilation.GetTypeByMetadataName(ViewComponentTypes.ViewComponentAttribute);
            var nonVCAttribute = compilation.GetTypeByMetadataName(ViewComponentTypes.NonViewComponentAttribute);
            return new ViewComponentTypeVisitor(vcAttribute, nonVCAttribute, results);
        }

        public ViewComponentTypeVisitor(
            INamedTypeSymbol viewComponentAttribute,
            INamedTypeSymbol nonViewComponentAttribute,
            List<INamedTypeSymbol> results)
        {
            _viewComponentAttribute = viewComponentAttribute;
            _nonViewComponentAttribute = nonViewComponentAttribute;
            _results = results;

            Enabled = _viewComponentAttribute != null;
        }

        public bool Enabled { get; set; }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            if (IsViewComponent(symbol))
            {
                _results.Add(symbol);
            }

            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                return;
            }

            foreach (var member in symbol.GetTypeMembers())
            {
                Visit(member);
            }
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                Visit(member);
            }
        }

        internal bool IsViewComponent(INamedTypeSymbol symbol)
        {
            if (!Enabled)
            {
                return false;
            }

            if (symbol.DeclaredAccessibility != Accessibility.Public ||
                symbol.IsAbstract ||
                symbol.IsGenericType ||
                AttributeIsDefined(symbol, _nonViewComponentAttribute))
            {
                return false;
            }

            return symbol.Name.EndsWith(ViewComponentTypes.ViewComponentSuffix) ||
                AttributeIsDefined(symbol, _viewComponentAttribute);
        }

        private static bool AttributeIsDefined(INamedTypeSymbol type, INamedTypeSymbol queryAttribute)
        {
            if (type == null || queryAttribute == null)
            {
                return false;
            }

            var attribute = type.GetAttributes().Where(a => a.AttributeClass == queryAttribute).FirstOrDefault();

            if (attribute != null)
            {
                return true;
            }

            return AttributeIsDefined(type.BaseType, queryAttribute);
        }
    }
}
