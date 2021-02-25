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
            Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_NoDbOrMigrationsTitle"), content);
            Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_NoDbOrMigrationsInfo"), content);
        }

        public static void NotDisplaysScaffoldFirstMigration(Type contextType, string content)
        {
            Assert.DoesNotContain(StringsHelpers.GetResourceString("DatabaseErrorPage_NoDbOrMigrationsTitle"), content);
            Assert.DoesNotContain(StringsHelpers.GetResourceString("DatabaseErrorPage_NoDbOrMigrationsInfo"), content);
        }

        public static void DisplaysApplyMigrations(Type contextType, string content)
        {
            Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_PendingMigrationsTitle"), content);
            Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_PendingMigrationsInfo"), content);
        }

        public static void NotDisplaysApplyMigrations(Type contextType, string content)
        {
            Assert.DoesNotContain(StringsHelpers.GetResourceString("DatabaseErrorPage_PendingMigrationsTitle"), content);
            Assert.DoesNotContain(StringsHelpers.GetResourceString("DatabaseErrorPage_PendingMigrationsInfo"), content);
        }

        public static void DisplaysScaffoldNextMigraion(Type contextType, string content)
        {
            Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_PendingChangesTitle"), content);
            Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_PendingChangesInfo"), content);
        }

        public static void NotDisplaysScaffoldNextMigraion(Type contextType, string content)
        {
            Assert.DoesNotContain(StringsHelpers.GetResourceString("DatabaseErrorPage_PendingChangesTitle"), content);
            Assert.DoesNotContain(StringsHelpers.GetResourceString("DatabaseErrorPage_PendingChangesInfo"), content);
        }
    }
}
