// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Identity.DefaultUI.WebSite.Data.Migrations;

public partial class UpdateIdentitySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UserNameIndex",
            table: "AspNetUsers");

        migrationBuilder.DropIndex(
            name: "IX_AspNetUserRoles_UserId",
            table: "AspNetUserRoles");

        migrationBuilder.DropIndex(
            name: "RoleNameIndex",
            table: "AspNetRoles");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "AspNetUsers",
            column: "NormalizedUserName",
            unique: true,
            filter: "[NormalizedUserName] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "AspNetRoles",
            column: "NormalizedName",
            unique: true,
            filter: "[NormalizedName] IS NOT NULL");

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUserTokens_AspNetUsers_UserId",
            table: "AspNetUserTokens",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUserTokens_AspNetUsers_UserId",
            table: "AspNetUserTokens");

        migrationBuilder.DropIndex(
            name: "UserNameIndex",
            table: "AspNetUsers");

        migrationBuilder.DropIndex(
            name: "RoleNameIndex",
            table: "AspNetRoles");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "AspNetUsers",
            column: "NormalizedUserName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserRoles_UserId",
            table: "AspNetUserRoles",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "AspNetRoles",
            column: "NormalizedName");
    }
}
