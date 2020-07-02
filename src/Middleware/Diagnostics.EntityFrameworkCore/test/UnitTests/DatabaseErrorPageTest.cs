// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests.Helpers;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Views;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests
{
    public class DatabaseErrorPageTest
    {
        [Fact]
        public async Task No_database_or_migrations_only_displays_scaffold_first_migration()
        {
            var model = new DatabaseErrorPageModel(
                contextType: typeof(BloggingContext),
                exception: new Exception(),
                databaseExists: false,
                pendingModelChanges: false,
                pendingMigrations: new string[] { });

            var content = await ExecutePage(model);

            AssertHelpers.DisplaysScaffoldFirstMigration(typeof(BloggingContext), content);
            AssertHelpers.NotDisplaysApplyMigrations(typeof(BloggingContext), content);
            AssertHelpers.NotDisplaysScaffoldNextMigraion(typeof(BloggingContext), content);
        }

        [Fact]
        public async Task No_database_with_migrations_only_displays_apply_migrations()
        {
            var model = new DatabaseErrorPageModel(
                contextType: typeof(BloggingContext),
                exception: new Exception(),
                databaseExists: false,
                pendingModelChanges: false,
                pendingMigrations: new[] { "111_MigrationOne" });

            var content = await ExecutePage(model);

            AssertHelpers.NotDisplaysScaffoldFirstMigration(typeof(BloggingContext), content);
            AssertHelpers.DisplaysApplyMigrations(typeof(BloggingContext), content);
            AssertHelpers.NotDisplaysScaffoldNextMigraion(typeof(BloggingContext), content);
        }

        [Fact]
        public async Task Existing_database_with_migrations_only_displays_apply_migrations()
        {
            var model = new DatabaseErrorPageModel(
                contextType: typeof(BloggingContext),
                exception: new Exception(),
                databaseExists: true,
                pendingModelChanges: false,
                pendingMigrations: new[] { "111_MigrationOne" });

            var content = await ExecutePage(model);

            AssertHelpers.NotDisplaysScaffoldFirstMigration(typeof(BloggingContext), content);
            AssertHelpers.DisplaysApplyMigrations(typeof(BloggingContext), content);
            AssertHelpers.NotDisplaysScaffoldNextMigraion(typeof(BloggingContext), content);
        }

        [Fact]
        public async Task Existing_database_with_migrations_and_pending_model_changes_only_displays_apply_migrations()
        {
            var model = new DatabaseErrorPageModel(
                contextType: typeof(BloggingContext),
                exception: new Exception(),
                databaseExists: true,
                pendingModelChanges: true,
                pendingMigrations: new[] { "111_MigrationOne" });

            var content = await ExecutePage(model);

            AssertHelpers.NotDisplaysScaffoldFirstMigration(typeof(BloggingContext), content);
            AssertHelpers.DisplaysApplyMigrations(typeof(BloggingContext), content);
            AssertHelpers.NotDisplaysScaffoldNextMigraion(typeof(BloggingContext), content);
        }

        [Fact]
        public async Task Pending_model_changes_only_displays_scaffold_next_migration()
        {
            var model = new DatabaseErrorPageModel(
                contextType: typeof(BloggingContext),
                exception: new Exception(),
                databaseExists: true,
                pendingModelChanges: true,
                pendingMigrations: new string[] { });

            var content = await ExecutePage(model);

            AssertHelpers.NotDisplaysScaffoldFirstMigration(typeof(BloggingContext), content);
            AssertHelpers.NotDisplaysApplyMigrations(typeof(BloggingContext), content);
            AssertHelpers.DisplaysScaffoldNextMigraion(typeof(BloggingContext), content);
        }

        [Fact]
        public async Task Exception_details_are_displayed()
        {
            var model = new DatabaseErrorPageModel(
                contextType: typeof(BloggingContext),
                exception: new Exception("Something bad happened"),
                databaseExists: false,
                pendingModelChanges: false,
                pendingMigrations: new string[] { });

            var content = await ExecutePage(model);

            Assert.Contains("Something bad happened", content);
        }

        [Fact]
        public async Task Inner_exception_details_are_displayed()
        {
            var model = new DatabaseErrorPageModel(
                contextType: typeof(BloggingContext),
                exception: new Exception("Something bad happened", new Exception("Because something more badder happened")),
                databaseExists: false,
                pendingModelChanges: false,
                pendingMigrations: new string[] { });

            var content = await ExecutePage(model);

            Assert.Contains("Something bad happened", content);
            Assert.Contains("Because something more badder happened", content);
        }

        private static async Task<string> ExecutePage(DatabaseErrorPageModel model)
        {
            var page = new DatabaseErrorPage();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            var stream = new MemoryStream();

            response.Setup(r => r.Body).Returns(stream);
            context.Setup(c => c.Response).Returns(response.Object);

            page.Model = model;

            await page.ExecuteAsync(context.Object);
            var content = Encoding.ASCII.GetString(stream.ToArray());
            return content;
        }

        private class BloggingContext : DbContext
        {

        }
    }
}
