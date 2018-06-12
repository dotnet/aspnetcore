# Debugging/Running JavaScript Unit Tests

We use [Jest](https://facebook.github.io/jest/) as our JavaScript testing framework. We also use [ts-jest](https://github.com/kulshekhar/ts-jest) which builds TypeScript automatically.

Prerequisites: NodeJS, have run `./build.cmd /t:Restore` at least once since cleaning.

All commands must be run from this directory (the `clients/ts` directory).

## Building the library before running tests

You need to build the libraries any time you make a change, before you can run the tests. Do this by running the following command from the `clients/ts` folder:

```
> npm run build
```

## Running all tests

```
> npm test
```

## Running all tests in a specific file

```
> npm test -- FileName
```

`FileName` can be a substring of the path, it will run all test files containing that **substring** in the path.

For example (use `/` for paths even on Windows, since Node is interpreting them):

* `npm test -- signalr/tests` will run all tests in `clients\ts\signalr\tests`
* `npm test -- signalr-protocol-msgpack/tests` will run all tests in `clients\ts\signalr-protocol-msgpack\tests`
* `npm test -- signalr/tests/` will run all tests in `clients\ts\signalr\tests`
* `npm test -- signalr/tests/JsonHubProtocol` will run all tests in `signalr/tests/JsonHubProtocol.test.ts`
* `npm test -- JsonHubProtocol` will **also** run all tests in `signalr/tests/JsonHubProtocol.test.ts` because it's the only test file matching that pattern

## Running a single test

The simplest way to run a single test is to use `.only`. Marking a test with `.only` will ensure that **only** that test is run when running tests from that file. If you are running multiple files (i.e. `npm test` with no arguments, it will still run all the tests in the other files).

To use `.only`, just add `.only` to the end of the call to `it`:

```typescript
describe("A suite of tests", () => {
    describe("A sub-suite of tests", () => {
        it.only("will run", () => {

        });

        it("will not run", () => {

        });
    });

    describe("Another sub-suite of tests", () => {
        it("will not run either", () => {

        });
    });
});
```

Just make sure you remove `.only` when you finish running that test!

You can also use the `-t` parameter to jest. That parameter takes a substring pattern to match against all tests to see if they should run. To improve the speed of the run, you should pair this up with the argument that takes a file path to filter on. For example, given these tests

```
describe("AbortSignal", () => {
    describe("aborted", () => {
        it("is false on initialization", () => {
            // ...
        });

        it("is true when aborted", () => {
            // ...
        });
    });

    describe("onabort", () => {
        it("is called when abort is called", () => {
            // ...
        });
    });
});
```

These commands will each run the following sets of tests:

* `npm test -- AbortSignal -t "AbortSignal aborted"` will run `AbortSignal aborted is false on initialization` and `AbortSignal aborted is true when aborted`.
* `npm test -- AbortSignal -t "is called when abort is called"` will run `AbortSignal onabort is called when abort is called`.

## Debugging All Tests

You can launch all tests under the debugger in Visual Studio Code by clicking on the "Debug" tab on the left side, selecting "Jest - All" in the dropdown at the top and clicking the play button, or pressing F5.

## Debugging All Tests in a single file

You can launch all tests **in the currently open file** under the debugger in Visual Studio Code by clicking on the "Debug" tab on the left side, selecting "Jest - Current File" in the dropdown at the top and clicking the play button, or pressing F5.

**NOTE**: Pair this with `.only` to easily debug a single test!
