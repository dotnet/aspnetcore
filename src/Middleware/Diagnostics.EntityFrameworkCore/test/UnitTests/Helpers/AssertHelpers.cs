// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests.Helpers
{
    public static class AssertHelpers
    {
        public static void DisplaysScaffoldFirstMigration(Type contextType, string content)
        {
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_NoDbOrMigrationsTitle", contextType.Name), content);
        }

        public static void NotDisplaysScaffoldFirstMigration(Type contextType, string content)
        {
            Assert.DoesNotContain(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_NoDbOrMigrationsTitle", contextType.Name), content);
        }

        public static void DisplaysApplyMigrations(Type contextType, string content)
        {
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_PendingMigrationsTitle", contextType.Name), content);
        }

        public static void NotDisplaysApplyMigrations(Type contextType, string content)
        {
            Assert.DoesNotContain(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_PendingMigrationsTitle", contextType.Name), content);
        }

        public static void DisplaysScaffoldNextMigraion(Type contextType, string content)
        {
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_PendingChangesTitle", contextType.Name), content);
        }

        public static void NotDisplaysScaffoldNextMigraion(Type contextType, string content)
        {
            Assert.DoesNotContain(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_PendingChangesTitle", contextType.Name), content);
        }
    }
}