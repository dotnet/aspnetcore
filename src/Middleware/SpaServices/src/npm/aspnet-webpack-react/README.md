# Not for general use

This NPM package is an internal implementation detail of the `Microsoft.AspNetCore.SpaServices` NuGet package.

You should not use this package directly in your own applications, because it is not supported, and there are no
guarantees about how its APIs will change in the future.

## History

* Version 1.x amends the Webpack config to insert `react-transform` and `react-transform-hmr` entries on `babel-loader`.
* Version 2.x drops support for the Babel plugin, and instead amends the Webpack config to insert `react-hot-loader/webpack` and `react-hot-loader/patch` entries. This means it works with React Hot Loader v3.
