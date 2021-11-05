// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class Base { }

    public class Derived : Base { }

    public class TestController
    {
        public IActionResult Get(int id) => null;

        public IActionResult Search(string searchTerm, bool sortDescending, int page) => null;

        public IActionResult SearchEmpty() => null;
    }

    public static class TestConvention
    {
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void Get(int id) { }

        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        public static void GetNoArgs() { }

        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        public static void GetTwoArgs(int id, string name) { }

        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void Post(Derived model) { }

        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void GetParameterNotMatching([ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.AssignableFrom)] Derived model) { }

        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        public static void Search(
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Exact)]
                string searchTerm,
            params object[] others)
        { }

        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        public static void SearchWithParams(params object[] others) { }

        public static void MethodWithoutMatchBehavior() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MethodWithRandomAttributes() { }

        public static void MethodParameterWithRandomAttributes([FromRoute] int value) { }

        public static void MethodWithAnyTypeMatchBehaviorParameter([ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)] int value) { }
    }
}
