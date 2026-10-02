dotnet-dev-certs
================

`dotnet-dev-certs` is a command line tool to generate certificates used in ASP.NET Core during development.

### How To Use

Run `dotnet dev-certs --help` for more information about usage.

### Linux NSS database overrides

On Linux, `DOTNET_DEV_CERTS_NSSDB_PATHS` can specify a colon-delimited list of NSS database directories. Prefix an entry with `firefox=` or `chromium=` to select the browser-specific certificate trust behavior:

```bash
export DOTNET_DEV_CERTS_NSSDB_PATHS="firefox=/path/to/firefox-profile:chromium=/path/to/chromium-nssdb"
```

The `firefox=` prefix can also configure certificate trust for Firefox-derived browsers that store their NSS database in a custom location.

Unprefixed paths remain supported. Known Firefox profile paths are detected automatically, and other paths use Chromium behavior.
