// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Identity.OpenIdConnect.WebSite.Identity.Data.Migrations
{
    public partial class CreateIdentitySchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    PasswordHash = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    SecurityStamp = table.Column<string>(nullable: true),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    UserName = table.Column<string>(maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true),
                    RoleId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    RoleId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetApplications",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ClientId = table.Column<string>(maxLength: 256, nullable: false),
                    ClientSecretHash = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Name = table.Column<string>(maxLength: 256, nullable: false),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetApplications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetApplicationClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(maxLength: 256, nullable: false),
                    ClaimValue = table.Column<string>(maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetApplicationClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetApplicationClaims_AspNetApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "AspNetApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRedirectUris",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ApplicationId = table.Column<string>(nullable: false),
                    IsLogout = table.Column<bool>(nullable: false),
                    Value = table.Column<string>(maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRedirectUris", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRedirectUris_AspNetApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "AspNetApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetScopes",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ApplicationId = table.Column<string>(nullable: false),
                    Value = table.Column<string>(maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetScopes_AspNetApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "AspNetApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "ClientIdIndex",
                table: "AspNetApplications",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "NameIndex",
                table: "AspNetApplications",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetApplications_UserId",
                table: "AspNetApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetApplicationClaims_ApplicationId",
                table: "AspNetApplicationClaims",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRedirectUris_ApplicationId",
                table: "AspNetRedirectUris",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetScopes_ApplicationId",
                table: "AspNetScopes",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            // Seed client application
            var clientAppId = "4122031F-D3A2-4C1A-B25E-2A55B2A32FAC";
            var clientId = "56A33E6A-ADFE-47EA-BBFE-40F4AE4C55BA";
            migrationBuilder.Sql($@"INSERT INTO AspNetApplications (Id,ClientId,Name)
VALUES (N'{clientAppId}',N'{clientId}',N'Identity.OpenIdConnect.WebSite')");
            //migrationBuilder.InsertData(
            //    table: "AspNetApplications",
            //    columns: new[] { "Id", "ClientId", "Name" },
            //    values: new object[,]
            //    {
            //        { clientAppId, clientId, "Identity.OpenIdConnect.WebSite" }
            //    });

            var clientOpenIdScopeId = "7F4F91FE-87F5-41DC-B111-3DC5FC186E35";
            migrationBuilder.Sql($@"INSERT INTO AspNetScopes (Id,ApplicationId,Value)
VALUES (N'{clientOpenIdScopeId}',N'{clientAppId}',N'{ApplicationScope.OpenId.Scope}')");
            //migrationBuilder.InsertData(
            //    table: "AspNetScopes",
            //    columns: new[] { "Id", "ApplicationId", "Value" },
            //    values: new object[,]
            //    {
            //        { clientOpenIdScopeId, clientAppId, ApplicationScope.OpenId.Scope },
            //    });

            var clientRedirectUriId = "849B8050-0DEC-4A96-B234-8A08695A1526";
            var clientLogoutRedirectUriId = "9F24EA98-4375-4CE2-A37C-95832F19D75D";
            migrationBuilder.Sql($@"INSERT INTO AspNetRedirectUris (Id, ApplicationId, IsLogout, Value)
VALUES (N'{clientRedirectUriId}',N'{clientAppId}','false',N'urn:self:aspnet:identity:integrated')");
            migrationBuilder.Sql($@"INSERT INTO AspNetRedirectUris (Id, ApplicationId, IsLogout, Value)
VALUES (N'{clientLogoutRedirectUriId}',N'{clientAppId}','true',N'urn:self:aspnet:identity:integrated')");
            //migrationBuilder.InsertData(
            //    table: "AspNetRedirectUris",
            //    columns: new[] { "Id", "ApplicationId", "IsLogout", "Value" },
            //    values: new object[,]
            //    {
            //        { clientRedirectUriId, clientAppId, false, "urn:self:aspnet:identity:integrated"},
            //        { clientLogoutRedirectUriId, clientAppId, true, "urn:self:aspnet:identity:integrated" }
            //    });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AspNetApplicationClaims");

            migrationBuilder.DropTable(
                name: "AspNetRedirectUris");

            migrationBuilder.DropTable(
                name: "AspNetScopes");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetApplications");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
