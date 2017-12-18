Universe
========

Build infrastructure used to produce the whole ASP.NET Core stack.

## Getting started

```
git clone --recursive https://github.com/aspnet/Universe.git
cd Universe
./build.cmd
```

## Useful properties and targets
Property                           | Purpose                                                                        | Example
-----------------------------------|--------------------------------------------------------------------------------|--------
`/p:SkipTests`/`/p:CompileOnly`    | Only build repos, don't run the tests.                                         | `/p:SkipTests=true`
`/p:TestOnly`                      | Don't package or verify things.                                                | `/p:TestOnly=true`
`ENV:KOREBUILD_REPOSITORY_INCLUDE` | A list of the repositories to include in build (instead of all of them).       | `ENV:KOREBUILD_REPOSITORY_INCLUDE=Antiforgery;CORS`
`ENV:KOREBUILD_REPOSITORY_EXCLUDE` | A list of the repositories to exclude from build (all the rest will be built). | `ENV:KOREBUILD_REPOSITORY_EXCLUDE=EntityFramework`

## More info

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.
