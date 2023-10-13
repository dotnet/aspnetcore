# Identity

ASP.NET Core Identity is the membership system for building ASP.NET Core web applications, including membership, login, and user data. ASP.NET Core Identity allows you to add login features to your application and makes it easy to customize data about the logged in user. You can find additional information in the [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core/security/authentication/identity).

## Description

The following contains a description of each sub-directory in the `Identity` directory.

* `Core`: Contains the main abstractions and types for providing support for Identity in ASP.NET Core applications.
* `EntityFrameworkCore`: Contains implementations for Identity stores based on EntityFrameworkCore.
* `Extensions.Core`: Contains the abstractions and types for general Identity concerns.
* `Extensions.Stores`: Contains abstractions and types for Identity storage providers.
* `samples`: Contains a collection of sample apps.
* `Specification.Tests`: Contains a test suite for ASP.NET Core Identity store implementations.
* `test`: Contains the unit and functional tests for Microsoft.Extensions.Identity and Microsoft.AspNetCore.Identity components.
* `testassets`: Contains a webapp used for functional testing.
* `UI`: Contains compiled Razor UI components for use in ASP.NET Core Identity.

## Development Setup

### Build

To build this specific project from source, follow the instructions [on building the project](../../docs/BuildFromSource.md#step-3-build-the-repo).

### Test

To run the tests for this project, [run the tests on the command line](../../docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).

## ASP.NET Identity for ASP.NET MVC 5

The previous versions of Identity for MVC5 and lower, previously available on CodePlex, are available at <https://github.com/aspnet/AspNetIdentity>

## Community Maintained Store Providers

**IMPORTANT:** Extensions are built by a variety of sources and not maintained as part of the ASP.NET Identity project. When considering a third party provider, be sure to evaluate quality, licensing, compatibility, support, etc. to ensure they meet your requirements.

* [ASP.NET Identity Azure Table Storage Provider](https://dlmelendez.github.io/identityazuretable/)
* ASP.NET Identity Cosmos DB Providers:
  * [By Dave Melendez](https://github.com/dlmelendez/identitycosmosdb)
  * [By Eric Kauffman](https://github.com/CosmosSoftware/AspNetCore.Identity.CosmosDb)
  * [By Piero De Tomi](https://github.com/pierodetomi/efcore-identity-cosmos)
* ASP.NET Identity MongoDB Providers:
  * [By Tugberk Ugurlu](https://github.com/tugberkugurlu/AspNetCore.Identity.MongoDB)
  * [By Alexandre Spieser](https://github.com/alexandre-spieser/AspNetCore.Identity.MongoDbCore)
  * [By Deveel](https://github.com/deveel/deveel.identity.mongodb)
* [ASP.NET Identity LinqToDB Provider](https://github.com/ili/LinqToDB.Identity)
* ASP.NET Identity DynamoDB Providers:
  * [By Vasyl Solovei](https://github.com/miltador/AspNetCore.Identity.DynamoDB)
  * [By Anna Aitchison](https://github.com/Ara225/ara225.DynamoDBUserStore)
* ASP.NET Identity RavenDB Providers:
  * [By Judah Gabriel Himango](https://github.com/JudahGabriel/RavenDB.Identity)
  * [By Iskandar Rafiev](https://github.com/maqduni/AspNetCore.Identity.RavenDB)
* [ASP.NET Identity Cassandra Provider](https://github.com/lkubis/AspNetCore.Identity.Cassandra)
* [ASP.NET Identity Firebase Provider](https://github.com/aguacongas/Identity.Firebase)
* [ASP.NET Identity Redis Provider](https://github.com/aguacongas/Identity.Redis)
* [ASP.NET Identity DocumentDB](https://github.com/codekoenig/AspNetCore.Identity.DocumentDb)
* [ASP.NET Identity Amazon Cognito Provider](https://github.com/aws/aws-aspnet-cognito-identity-provider)
* [ASP.NET Identity Marten Provider](https://github.com/yetanotherchris/Marten.AspNetIdentity)
