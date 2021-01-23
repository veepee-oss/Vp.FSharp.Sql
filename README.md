# Vp.FSharp.Sql

The core library that enables to work with any ADO provider _consistently_.

## Slagging Hype

We aim at following highly controversial practices to the best of our ability!

Status | Package                
------ | ----------------------
OK     | [![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-green.svg)](https://conventionalcommits.org)
OK     | [![semver](https://img.shields.io/badge/semver-2.0.0-green)](https://semver.org/spec/v2.0.0.html)
TBD    | [![keep a changelog](https://img.shields.io/badge/keep%20a%20changelog-1.0.0-red)](https://keepachangelog.com/en/1.0.0)
TBD    | [![Semantic Release](https://img.shields.io/badge/Semantic%20Release-17.1.1-red)](https://semantic-release.gitbook.io/semantic-release)

[Conventional Commits]: https://conventionalcommits.org
[semver]: https://img.shields.io/badge/semver-2.0.0-blue
[Semantic Release]: https://semantic-release.gitbook.io/semantic-release
[keep a changelog]: https://keepachangelog.com/en/1.0.0

## NuGet Package

 Name            | Version  | Command |
---------------- | -------- | ------- |
 `Vp.FSharp.Sql` | [![NuGet Status](http://img.shields.io/nuget/v/Vp.FSharp.Sql.svg)](https://www.nuget.org/packages/Vp.FSharp.Sql) | `Install-Package Vp.FSharp.Sql`

# Why did you folks create this lib?

##  Motivations

- We all agree that Dapper is a great library
- We also wanted something even more bare-bone and more functional / idiomatic-F#
- Inspired by [Zaid's Npgsql.FSharp](https://github.com/Zaid-Ajaj/Npgsql.FSharp) but with
    - Multi DB-providers support
    - Explicit operations flow
    - Transaction Helpers
    - Dapper connection workflow
    - (Very) limited support for basic events (ie. logging)
    - Opinionated (ie. async only, no result as return types)

# How to use this library?

This library mostly aims at being used some sort of building block with ADO.NET providers to provide a strongly-typed, 
you can check out the libraries below, each leveraging a specific ADO.NET provider:

Name                                          | Version  | Command |
--------------------------------------------- | -------- | ------- |
[`Vp.FSharp.Sql.Sqlite`][sqlite-repo]         | [![NuGet Status](http://img.shields.io/nuget/v/Vp.FSharp.Sql.Sqlite.svg)](https://www.nuget.org/packages/Vp.FSharp.Sql.Sqlite) | `Install-Package Vp.FSharp.Sql.Sqlite`
[`Vp.FSharp.Sql.SqlServer`][sqlserver-repo]   | [![NuGet Status](http://img.shields.io/nuget/v/Vp.FSharp.Sql.SqlServer.svg)](https://www.nuget.org/packages/Vp.FSharp.Sql.SqlServer) | `Install-Package Vp.FSharp.Sql.SqlServer`
[`Vp.FSharp.Sql.PostgreSql`][postgresql-repo] | [![NuGet Status](http://img.shields.io/nuget/v/Vp.FSharp.Sql.PostgreSql.svg)](https://www.nuget.org/packages/Vp.FSharp.Sql.Sqlite) | `Install-Package Vp.FSharp.Sql.PostgreSql`

In a Nutshell you can create your own provider by:
- Using the relevant generic providers `SqlDependencies` 
  (this is used due to the lack of member support for the SRTP, 
  especially regarding shadowing members which is quite common across all ADO.NET providers)
- To be continued...

# How to Contribute
Bug reports, feature requests, and pull requests are very welcome! Please read the [Contribution Guidelines](./CONTRIBUTION.md) to get started.

# Licensing
The project is licensed under MIT. For more information on the license see the [license file](./LICENSE).

[sqlite-repo]: https://github.com/veepee-oss/Vp.FSharp.Sql.Sqlite
[sqlserver-repo]: https://github.com/veepee-oss/Vp.FSharp.Sql.SqlServer
[postgresql-repo]: https://github.com/veepee-oss/Vp.FSharp.Sql.PostgreSql
