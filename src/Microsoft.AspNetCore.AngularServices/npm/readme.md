If you just want to use this package, then you *don't have to build it*. Instead, just grab the prebuilt package from NPM:

    npm install angular2-aspnet

The rest of this file is notes for anyone contributing to this package itself.

## How to build

Run the following:

    npm install
    npm run prepublish

Requirements:

 * Node, NPM
 * `tsc` installed globally (via `npm install -g typescript`)

## Project structure

This package is intended to be consumable both on the server in Node.js, and on the client. Also, it's written in TypeScript,
which neither of those environments knows natively, but the TypeScript type definitions need to get delivered with the package
so that developers get a good IDE experience when consuming it.

The build process is therefore:

1. Compile the TypeScript to produce the development-time (.d.ts) and server-side (.js) artifacts

   `tsc` reads `tsconfig.json` and is instructed to compile all the `.ts` files in `src/`. It produces a corresponding
   structure of `.js` and `.d.ts` files in `dist/`.

   When a developer consumes the resulting package (via `npm install angular2-aspnet`),

    - No additional copy of `angular2` will be installed, because this package's dependency on it is declared as a
      `peerDependency`. This means it will work with whatever (compatible) version of `angular2` is already installed.
    - At runtime inside Node.js, the `main` configuration in `package.json` means the developer can use a standard
      `import` statement to consume this package (i.e., `import * from 'angular2-aspnet';` in either JS or TS files).
    - At development time inside an IDE such as Visual Studio Code, the `typings` configuration in `package.json` means
      the IDE will use the corresponding `.d.ts` file as type metadata for the variable imported that way.

2. Use the SystemJS builder to produce the client-side artifacts

   `build.js` uses the SystemJS Builder API to combine files in `dist/` into `.js` files ready for use in client-side
   SystemJS environments, and puts them in `bundles/`. The bundle files contain `System.register` calls so that any
   other part of your client-side code that tries to import `angular2-aspnet` via SystemJS will get that module at runtime.

   To make it work in an application:
    - Set up some build step that copies your chosen bundle file from `bundles/` to some location where it will
      be served to the client
    - Below your `<script>` tag that loads SystemJS itself, and above the `<script>` tag that makes the first call to
      `System.import`, have a `<script>` tag that loads the desired `angular2-aspnet.js` bundle file

   For an example, see https://github.com/aspnet/NodeServices/tree/master/samples/angular/MusicStore

   Of course, you can also bundle the `angular2-aspnet.js` file into a larger SystemJS bundle if you want to combine
   it with the rest of the code in your application.

Currently, this build system does *not* attempt to send sourcemaps of the original TypeScript to the client. This
could be added if a strong need emerges.
