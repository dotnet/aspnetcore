# Contributing to the jQuery Validation Plugin

## Reporting an Issue

1. Make sure the problem you're addressing is reproducible.
2. Use http://jsbin.com or http://jsfiddle.net to provide a test page.
3. Indicate what browsers the issue can be reproduced in. **Note: IE Compatibilty mode issues will not be addressed. Make sure you test in a real browser!**
4. What version of the plug-in is the issue reproducible in. Is it reproducible after updating to the latest version.

Documentation issues are also tracked at the [jQuery Validation](https://github.com/jzaefferer/jquery-validation/issues) issue tracker.
Pull Requests to improve the docs are welcome at the [jQuery Validation docs](https://github.com/jzaefferer/validation-content) repository, though.

**IMPORTANT NOTE ABOUT EMAIL VALIDATION**. As of version 1.12.0 this plugin is using the same regular expression that the [HTML5 specification suggests for browsers to use](https://html.spec.whatwg.org/multipage/forms.html#valid-e-mail-address). We will follow their lead and use the same check. If you think the specification is wrong, please report the issue to them. If you have different requirements, consider [using a custom method](http://jqueryvalidation.org/jQuery.validator.addMethod/).

## Contributing code

Thanks for contributing! Here's a few guidelines to help your contribution get landed.

1. Make sure the problem you're addressing is reproducible. Use jsbin.com or jsfiddle.net to provide a test page.
2. Follow the [jQuery style guide](http://contribute.jquery.com/style-guides/js)
3. Add or update unit tests along with your patch. Run the unit tests in at least one browser (see below).
4. Run `grunt` (see below) to check for linting and a few other issues.
5. Describe the change in your commit message and reference the ticket, like this: "Demos: Fixed delegate bug for dynamic-totals demo. Fixes #51". If you're adding a new localization file, use something like this: "Localization: Added croatian (HR) localization"

## Build setup

1. Install [NodeJS](http://nodejs.org).
2. Install the Grunt CLI To install by running `npm install -g grunt-cli`. More details are available on their website http://gruntjs.com/getting-started.
3. Install the NPM dependencies by running `npm install`.
4. The build can now be called by running `grunt`.

## Creating a new Additional Method

If you've wrote custom methods that you'd like to contribute to additional-methods.js:

1. Create a branch
2. Add the method as a new file in `src/additional`
3. (Optional) Add translations to `src/localization`
4. Send a pull request to the master branch.

## Unit Tests

To run unit tests, just open `test/index.html` within your browser. Make sure you ran `npm install` before so all required dependencies are available.
Start with one browser while developing the fix, then run against others before committing. Usually latest Chrome, Firefox, Safari and Opera and a few IEs.

## Documentation

Please report documentation issues at the [jQuery Validation](https://github.com/jzaefferer/jquery-validation/issues) issue tracker.
In case your pull request implements or changes public API it would be a plus you would provide a pull request against the [jQuery Validation docs](https://github.com/jzaefferer/validation-content) repository.

## Linting

To run JSHint and other tools, use `grunt`.
