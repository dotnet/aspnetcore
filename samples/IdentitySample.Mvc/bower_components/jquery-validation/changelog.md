1.14.0 / 2015-06-30
==================

## Core
  * Remove unused removeAttrs method
  * Replace regex for url method
  * Remove bad url param in $.ajax, overwritten by $.extend
  * Properly handle nested cancel submit button
  * Fix indent
  * Refactor attributeRules and dataRules to share noramlizer
  * dataRules method to convert value to number for number inputs
  * Update url method to allow for protocol-relative URLs
  * Remove deprecated $.format placeholder
  * Use jQuery 1.7+ on/off, add destroy method
  * IE8 compatibility changed .indexOf to $.inArray
  * Cast NaN value attributes to undefined for Opera Mini
  * Stop trimming value inside required method
  * Use :disabled selector to match disabled elements
  * Exclude some keyboard keys to prevent revalidating the field
  * Do not search the whole DOM for radio/checkbox elements
  * Throw better errors for bad rule methods
  * Fixed number validation error
  * Fix reference to whatwg spec
  * Focus invalid element when validating a custom set of inputs
  * Reset element styles when using custom highlight methods
  * Escape dollar sign in error id
  * Revert "Ignore readonly as well as disabled fields."
  * Update link in comment for Luhn algorithm

## Additionals
  * Update dateITA to address timezone issue
  * Fix extension method to only method period
  * Fix accept method to match period only
  * Update time method to allow single digit hour
  * Drop bad test for notEqualTo method
  * Add notEqualTo method
  * Use correct jQuery reference via `$`
  * Remove useless regex check in iban method
  * Brazilian CPF number

## Localization
  * Update messages_tr.js
  * Update messages_sr_lat.js
  * Adding Perú Spanish (ES PE)
  * Adding Georgian (ქართული, ge)
  * Fixed typo in catalan translation
  * Improve Finnish (fi) translation
  * Add armenian (hy_AM) locale
  * Extend italian (it) translation with currency method
  * Add bn_BD locale
  * Update zh locale
  * Remove full stop at the end of italian messages

1.13.1 / 2014-10-14
==================

## Core
  * Allow 0 as value for autoCreateRanges
  * Apply ignore setting to all validationTargetFor elements
  * Don't trim value in min/max/rangelength methods
  * Escape id/name before using it as a selector in errorsFor
  * Explicit default for focusCleanup option
  * Fix incorrect regexp for describedby matcher
  * Ignore readonly as well as disabled fields
  * Improve id escaping, store escaped id in describedby
  * Use return value of submitHandler to allow or prevent form submit

## Additionals
  * Add postalcodeBR method
  * Fix pattern method when parameter is a string


1.13.0 / 2014-07-01
==================

## All
* Add plugin UMD wrapper

## Core
* Respect non-error aria-describedby and empty hidden errors
* Improve dateISO RegExp
* Added radio/checkbox to delegate click-event
* Use aria-describedby for non-label elements
* Register focusin, focusout and keyup also on radio/checkbox
* Fix normalization for rangelength attribute value
* Update elementValue method to deal with type="number" fields
* Use charAt instead of array notation on strings, to support IE8(?)

## Localization
* Fix sk translation of rangelength method
* Add Finnish methods
* Fixed GL number validation message
* Fixed ES number method validation message
* Added galician (GL)
* Fixed French messages for min and max methods

## Additionals
* Add statesUS method
* Fix dateITA method to deal with DST bug
* Add persian date method
* Add postalCodeCA method
* Add postalcodeIT method

1.12.0 / 2014-04-01
==================

* Add ARIA testing ([3d5658e](https://github.com/jzaefferer/jquery-validation/commit/3d5658e9e4825fab27198c256beed86f0bd12577))
* Add es-AR localization messages. ([7b30beb](https://github.com/jzaefferer/jquery-validation/commit/7b30beb8ebd218c38a55d26a63d529e16035c7a2))
* Add missing dots to 'es' and 'es_AR' messages. ([a2a653c](https://github.com/jzaefferer/jquery-validation/commit/a2a653cb68926ca034b4b09d742d275db934d040))
* Added Indonesian (ID) localization ([1d348bd](https://github.com/jzaefferer/jquery-validation/commit/1d348bdcb65807c71da8d0bfc13a97663631cd3a))
* Added NIF, NIE and CIF Spanish documents numbers validation ([#830](https://github.com/jzaefferer/jquery-validation/issues/830), [317c20f](https://github.com/jzaefferer/jquery-validation/commit/317c20fa9bb772770bb9b70d46c7081d7cfc6545))
* Added the current form to the context of the remote ajax request ([0a18ae6](https://github.com/jzaefferer/jquery-validation/commit/0a18ae65b9b6d877e3d15650a5c2617a9d2b11d5))
* Additionals: Update IBAN method, trim trailing whitespaces ([#970](https://github.com/jzaefferer/jquery-validation/issues/970), [347b04a](https://github.com/jzaefferer/jquery-validation/commit/347b04a7d4e798227405246a5de3fc57451d52e1))
* BIC method: Improve RegEx, {1} is always redundant. Closes gh-744 ([5cad6b4](https://github.com/jzaefferer/jquery-validation/commit/5cad6b493575e8a9a82470d17e0900c881130873))
* Bower: Add Bower.json for package registration ([e86ccb0](https://github.com/jzaefferer/jquery-validation/commit/e86ccb06e301613172d472cf15dd4011ff71b398))
* Changes dollar references to 'jQuery', for compability with jQuery.noConflict. Closes gh-754 ([2049afe](https://github.com/jzaefferer/jquery-validation/commit/2049afe46c1be7b3b89b1d9f0690f5bebf4fbf68))
* Core: Add "method" field to error list entry ([89a15c7](https://github.com/jzaefferer/jquery-validation/commit/89a15c7a4b17fa2caaf4ff817f09b04c094c3884))
* Core: Added support for generic messages via data-msg attribute ([5bebaa5](https://github.com/jzaefferer/jquery-validation/commit/5bebaa5c55c73f457c0e0181ec4e3b0c409e2a9d))
* Core: Allow attributes to have a value of zero (eg min='0') ([#854](https://github.com/jzaefferer/jquery-validation/issues/854), [9dc0d1d](https://github.com/jzaefferer/jquery-validation/commit/9dc0d1dd946b2c6178991fb16df0223c76162579))
* Core: Disable deprecated $.format ([#755](https://github.com/jzaefferer/jquery-validation/issues/755), [bf3b350](https://github.com/jzaefferer/jquery-validation/commit/bf3b3509140ea8ab5d83d3ec58fd9f1d7822efc5))
* Core: Fix support for multiple error classes ([c1f0baf](https://github.com/jzaefferer/jquery-validation/commit/c1f0baf36c21ca175bbc05fb9345e5b44b094821))
* Core: Ignore events on ignored elements ([#700](https://github.com/jzaefferer/jquery-validation/issues/700), [a864211](https://github.com/jzaefferer/jquery-validation/commit/a86421131ea69786ee9e0d23a68a54a7658ccdbf))
* Core: Improve elementValue method ([6c041ed](https://github.com/jzaefferer/jquery-validation/commit/6c041edd21af1425d12d06cdd1e6e32a78263e82))
* Core: Make element() handle ignored elements properly. ([3f464a8](https://github.com/jzaefferer/jquery-validation/commit/3f464a8da49dbb0e4881ada04165668e4a63cecb))
* Core: Switch dataRules parsing to W3C HTML5 spec style ([460fd22](https://github.com/jzaefferer/jquery-validation/commit/460fd22b6c84a74c825ce1fa860c0a9da20b56bb))
* Core: Trigger success on optional but have other successful validators ([#851](https://github.com/jzaefferer/jquery-validation/issues/851), [f93e1de](https://github.com/jzaefferer/jquery-validation/commit/f93e1deb48ec8b3a8a54e946a37db2de42d3aa2a))
* Core: Use plain element instead of un-wrapping the element again ([03cd4c9](https://github.com/jzaefferer/jquery-validation/commit/03cd4c93069674db5415a0bf174a5870da47e5d2))
* Core: make sure remote is executed last ([#711](https://github.com/jzaefferer/jquery-validation/issues/711), [ad91b6f](https://github.com/jzaefferer/jquery-validation/commit/ad91b6f388b7fdfb03b74e78554cbab9fd8fca6f))
* Demo: Use correct option in multipart demo. ([#1025](https://github.com/jzaefferer/jquery-validation/issues/1025), [070edc7](https://github.com/jzaefferer/jquery-validation/commit/070edc7be4de564cb74cfa9ee4e3f40b6b70b76f))
* Fix $/jQuery usage in additional methods. Fixes #839 ([#839](https://github.com/jzaefferer/jquery-validation/issues/839), [59bc899](https://github.com/jzaefferer/jquery-validation/commit/59bc899e4586255a4251903712e813c21d25b3e1))
* Improve Chinese translations ([1a0bfe3](https://github.com/jzaefferer/jquery-validation/commit/1a0bfe32b16f8912ddb57388882aa880fab04ffe))
* Initial ARIA-Required implementation ([bf3cfb2](https://github.com/jzaefferer/jquery-validation/commit/bf3cfb234ede2891d3f7e19df02894797dd7ba5e))
* Localization: change accept values to extension. Fixes #771, closes gh-793. ([#771](https://github.com/jzaefferer/jquery-validation/issues/771), [12edec6](https://github.com/jzaefferer/jquery-validation/commit/12edec66eb30dc7e86756222d455d49b34016f65))
* Messages: Add icelandic localization ([dc88575](https://github.com/jzaefferer/jquery-validation/commit/dc885753c8872044b0eaa1713cecd94c19d4c73d))
* Messages: Add missing dots to 'bg', 'fr' and 'sr' messages. ([adbc636](https://github.com/jzaefferer/jquery-validation/commit/adbc6361c377bf6b74c35df9782479b1115fbad7))
* Messages: Create messages_sr_lat.js ([f2f9007](https://github.com/jzaefferer/jquery-validation/commit/f2f90076518014d98495c2a9afb9a35d45d184e6))
* Messages: Create messages_tj.js ([de830b3](https://github.com/jzaefferer/jquery-validation/commit/de830b3fd8689a7384656c17565ee92c2878d8a5))
* Messages: Fix sr_lat translation, add missing space ([880ba1c](https://github.com/jzaefferer/jquery-validation/commit/880ba1ca545903a41d8c5332fc4038a7e9a580bd))
* Messages: Update messages_sr.js, fix missing space ([10313f4](https://github.com/jzaefferer/jquery-validation/commit/10313f418c18ea75f385248468c2d3600f136cfb))
* Methods: Add additional method for currency ([1a981b4](https://github.com/jzaefferer/jquery-validation/commit/1a981b440346620964c87ebdd0fa03246348390e))
* Methods: Adding Smart Quotes to stripHTML's punctuation removal ([aa0d624](https://github.com/jzaefferer/jquery-validation/commit/aa0d6241c3ea04663edc1e45ed6e6134630bdd2f))
* Methods: Fix dateITA method, avoiding summertime errors ([279b932](https://github.com/jzaefferer/jquery-validation/commit/279b932c1267b7238e6652880b7846ba3bbd2084))
* Methods: Localized methods for chilean culture (es-CL) ([cf36b93](https://github.com/jzaefferer/jquery-validation/commit/cf36b933499e435196d951401221d533a4811810))
* Methods: Update email to use HTML5 regex, remove email2 method ([#828](https://github.com/jzaefferer/jquery-validation/issues/828), [dd162ae](https://github.com/jzaefferer/jquery-validation/commit/dd162ae360639f73edd2dcf7a256710b2f5a4e64))
* Pattern method: Remove delimiters, since HTML5 implementations don't include those either. ([37992c1](https://github.com/jzaefferer/jquery-validation/commit/37992c1c9e2e0be8b315ccccc2acb74863439d3e))
* Restricting credit card validator to include length check. Closes gh-772 ([f5f47c5](https://github.com/jzaefferer/jquery-validation/commit/f5f47c5c661da5b0c0c6d59d169e82230928a804))
* Update messages_ko.js - closes gh-715 ([5da3085](https://github.com/jzaefferer/jquery-validation/commit/5da3085ff02e0e6ecc955a8bfc3bb9a8d220581b))
* Update messages_pt_BR.js. Closes gh-782 ([4bf813b](https://github.com/jzaefferer/jquery-validation/commit/4bf813b751ce34fac3c04eaa2e80f75da3461124))
* Update phonesUK and mobileUK to accept new prefixes. Closes gh-750 ([d447b41](https://github.com/jzaefferer/jquery-validation/commit/d447b41b830dee984be21d8281ec7b87a852001d))
* Verify nine-digit zip codes. Closes gh-726 ([165005d](https://github.com/jzaefferer/jquery-validation/commit/165005d4b5780e22d13d13189d107940c622a76f))
* phoneUS: Add N11 exclusions. Closes gh-861 ([519bbc6](https://github.com/jzaefferer/jquery-validation/commit/519bbc656bcb26e8aae5166d7b2e000014e0d12a))
* resetForm should clear any aria-invalid values ([4f8a631](https://github.com/jzaefferer/jquery-validation/commit/4f8a631cbe84f496ec66260ada52db2aa0bb3733))
* valid(): Check all elements. Fixes #791 - valid() validates only the first (invalid) element ([#791](https://github.com/jzaefferer/jquery-validation/issues/791), [6f26803](https://github.com/jzaefferer/jquery-validation/commit/6f268031afaf4e155424ee74dd11f6c47fbb8553))

1.11.1 / 2013-03-22
==================

  * Revert to also converting parameters of range method to numbers. Closes gh-702
  * Replace most usage of PHP with mockjax handlers. Do some demo cleanup as well, update to newer masked-input plugin. Keep captcha demo in PHP. Fixes #662
  * Remove inline code highlighting from milk demo. View source works fine.
  * Fix dynamic-totals demo by trimming whitespace from template content before passing to jQuery constructor
  * Fix min/max validation. Closes gh-666. Fixes #648
  * Fixed 'messages' coming up as a rule and causing an exception after being updated through rules("add"). Closes gh-670, fixes #624
  * Add Korean (ko) localization. Closes gh-671
  * Improved the UK postcode method to filter out more invalid postcodes. Closes #682
  * Update messages_sv.js. Closes #683
  * Change grunt link to the project website. Closes #684
  * Move remote method down the list to run last, after all other methods applied to a field. Fixes #679
  * Update plugin.json description, should include the word 'validate'
  * Fix typos
  * Fix jQuery loader to use path of itself. Fixes nested demos.
  * Update grunt-contrib-qunit to make use of PhantomJS 1.8, when installed through node module 'phantomjs'
  * Make valid() return a boolean instead of 0 or 1. Fixes #109 - valid() does not return boolean value

1.11.0 / 2013-02-04
==================

  * Remove clearing as numbers of `min`, `max` and `range` rules. Fixes #455. Closes gh-528.
  * Update pre-existing labels - fixes #430 closes gh-436
  * Fix $.validator.format to avoid group interpolation, where at least IE8/9 replaces -bash with the match. Fixes #614
  * Fix mimetype regex
  * Add plugin manifest and update headers to just MIT license, drop unnecessary dual-licensing (like jQuery).
  * Hebrew messages: Removed dots at end of sentences - Fixes gh-568
  * French translation for require_from_group validation. Fixes gh-573.
  * Allow groups to be an array or a string - Fixes #479
  * Removed spaces with multiple MIME types
  * Fix some date validations, JS syntax errors.
  * Remove support for metadata plugin, replace with data-rule- and data-msg- (added in 907467e8) properties.
  * Added sftp as a valid url-pattern
  * Add Malay (my) localization
  * Update localization/messages_hu.js
  * Remove focusin/focusout polyfill. Fixes #542 - Inclusion of jquery.validate interfers with focusin and focusout events in IE9
  * Localization: Fixed typo in finnish translation
  * Fix RTM demo to show invalid icon when going from valid back to invalid
  * Fixed premature return in remote function which prevented ajax call from being made in case an input was entered too quickly. Ensures remote validation always validates the newest value.
  * Undo fix for #244. Fixes #521 - E-mail validation fires immediately when text is in the field.

1.10.0 / 2012-09-07
===================

  * Corrected French strings for nowhitespace, phoneUS, phoneUK and mobileUK based upon community feedback.
  * rename files for language_REGION according to the standard ISO_3166-1 (http://en.wikipedia.org/wiki/ISO_3166-1), for Taiwan tha language is Chinese (zh) and the region is Taiwan (TW)
  * Optimise RegEx patterns, especially for UK phone numbers.
  * Add Language Name for each file, rename the language code according to the standard ISO 639 for Estonian, Georgian, Ukrainian and Chinese (http://en.wikipedia.org/wiki/List_of_ISO_639-1_codes)
  * Added croatian (HR) localization
  * Existing French translations were edited and French translations for the additional methods were added.
  * Merged in changes for specifying custom error messages in data attributes
  * Updated UK Mobile phone number regex for new numbers. Fixes #154
  * Add element to success call with test. Fixes #60
  * Fixed regex for time additional method. Fixes #131
  * resetForm now clears old previousValue on form elements. Fixes #312
  * Added checkbox test to require_from_group and changed require_from_group to use elementValue. Fixes #359
  * Fixed dataFilter response issues in jQuery 1.5.2+. Fixes #405
  * Added jQuery Mobile demo. Fixes #249
  * Deoptimize findByName for correctness. Fixes #82 - $.validator.prototype.findByName breaks in IE7
  * Added US zip code support and test. Fixes #90
  * Changed lastElement to lastActive in keyup, skip validation on tab or empty element. Fixes #244
  * Removed number stripping from stripHtml. Fixes #2
  * Fixed invalid count on invalid to valid remote validation. Fixes #286
  * Add link to file_input to demo index
  * Moved old accept method to extension additional-method, added new accept method to handle standard browser mimetype filtering. Fixes #287 and supersedes #369
  * Disables blur event when onfocusout is set to false. Test added.
  * Fixed value issue for radio buttons and checkboxes. Fixes #363
  * Added test for rangeWords and fixed regex and bounds in method. Fixes #308
  * Fixed TinyMCE Demo and added link on demo page. Fixes #382
  * Changed localization message for min/max. Fixes #273
  * Added pseudo selector for text input types to fix issue with default empty type attribute. Added tests and some test markup. Fixes #217
  * Fixed delegate bug for dynamic-totals demo. Fixes #51
  * Fix incorrect message for alphanumeric validator
  * Removed incorrect false check on required attribute
  * required attribute fix for non-html5 browsers. Fixes #301
  * Added methods "require_from_group" and "skip_or_fill_minimum"
  * Use correct iso code for swedish
  * Updated demo HTML files to use HTML5 doctype
  * Fixed regex issue for decimals without leading zeroes. Added new methods test. Fixes #41
  * Introduce a elementValue method that normalizes only string values (don't touch array value of multi-select). Fixes #116
  * Support for dynamically added submit buttons, and updated test case. Uses validateDelegate. Code from PR #9
  * Fix bad double quote in test fixtures
  * Fix maxWords method to include the upper bound, not exclude it. Fixes #284
  * Fixed grammar error in german range validator message. Fixes #315
  * Fixed handling of multiple class names for errorClass option. Test by Max Lynch. Fixes #280
  * Fix jQuery.format usage, should be $.validator.format. Fixes #329
  * Methods for 'all' UK phone numbers + UK postcodes
  * Pattern method: Convert string param to RegExp. Fixes issue #223
  * grammar error in german localization file
  * Added Estonian localization for messages
  * Improve tooltip handling on themerollered demo
  * Add type="text" to input fields without type attribute to please qSA
  * Update themerollered demo to use tooltip to show errors as overlay.
  * Update themerollered demo to use latest jQuery UI (along with newer jQuery version). Move code around to speed up page load.
  * Fixed min error message broken in Japanese.
  * Update form plugin to latest version. Enhance the ajaxSubmit demo.
  * Drop dateDE and numberDE methods from classRuleSettings, leftover from moving those to localized methods
  * Passing submit event to submitHandler callback
  * Fixed #219 - Fix valid() on elements with dependency-callback or dependency-expression.
  * Improve build to remove dist dir to ensure only the current release gets zipped up

1.9.0
---
* Added Basque (EU) localization
* Added Slovenian (SL) localization
* Fixed issue #127 - Finnish translations has one : instead of ;
* Fixed Russian localization, minor syntax issue
* Added in support for HTML5 input types, fixes #97
* Improved HTML5 support by setting novalidate attribute on the form, and reading the type attribute.
* Fixed showLabel() removing all classes from error element. Remove only settings.validClass. Fixes #151.
* Added 'pattern' to additional-methods to validate against arbitrary regular expressions.
* Improved email method to not allow the dot at the end (valid by RFC, but unwanted here). Fixes #143
* Fixed swedish and norwegian translations, min/max messages got switched. Fixes #181
* Fixed #184 - resetForm: should unset lastElement
* Fixed #71 - improve existing time method and add time12h method for 12h am/pm time format
* Fixed #177 - Fix validation of a single radio or checkbox input
* Fixed #189 - :hidden elements are now ignored by default
* Fixed #194 - Required as attribute fails if jQuery>=1.6 - Use .prop instead of .attr
* Fixed #47, #39, #32 - Allowed credit card numbers to contain spaces as well as dashes (spaces are commonly input by users).

1.8.1
---
* Added Thai (TH) localization, fixes #85
* Added Vietnamese (VI) localization, thanks Ngoc
* Fixed issue #78. Error/Valid styling applies to all radio buttons of same group for required validation.
* Don't use form.elements as that isn't supported in jQuery 1.6 anymore. Its buggy as hell anyway (IE6-8: form.elements === form).

1.8.0
---
* Improved NL localization (http://plugins.jquery.com/node/14120)
* Added Georgian (GE) localization, thanks Avtandil Kikabidze
* Added Serbian (SR) localization, thanks Aleksandar Milovac
* Added ipv4 and ipv6 to additional methods, thanks Natal Ngétal
* Added Japanese (JA) localization, thanks Bryan Meyerovich
* Added Catalan (CA) localization, thanks Xavier de Pedro
* Fixed missing var statements within for-in loops
* Fix for remote validation, where a formatted message got messed up (https://github.com/jzaefferer/jquery-validation/issues/11)
* Bugfixes for compatibility with jQuery 1.5.1, while maintaining backwards-compatibility

1.7
---
* Added Lithuanian (LT) localization
* Added Greek (EL) localization (http://plugins.jquery.com/node/12319)
* Added Latvian (LV) localization (http://plugins.jquery.com/node/12349)
* Added Hebrew (HE) localization (http://plugins.jquery.com/node/12039)
* Fixed Spanish (ES) localization (http://plugins.jquery.com/node/12696)
* Added jQuery UI themerolled demo
* Removed cmxform.js
* Fixed four missing semicolons (http://plugins.jquery.com/node/12639)
* Renamed phone-method in additional-methods.js to phoneUS
* Added phoneUK and mobileUK methods to additional-methods.js (http://plugins.jquery.com/node/12359)
* Deep extend options to avoid modifying multiple forms when using the rules-method on a single element (http://plugins.jquery.com/node/12411)
* Bugfixes for compatibility with jQuery 1.4.2, while maintaining backwards-compatibility

1.6
---
* Added Arabic (AR), Portuguese (PTPT), Persian (FA), Finnish (FI) and Bulgarian (BR) localization
* Updated Swedish (SE) localization (some missing html iso characters)
* Fixed $.validator.addMethod to properly handle empty string vs. undefined for the message argument
* Fixed two accidental global variables
* Enhanced min/max/rangeWords (in additional-methods.js) to strip html before counting; good when counting words in a richtext editor
* Added localized methods for DE, NL and PT, removing the dateDE and numberDE methods (use messages_de.js and methods_de.js with date and number methods instead)
* Fixed remote form submit synchronization, kudos to Matas Petrikas
* Improved interactive select validation, now validating also on click (via option or select, inconsistent across browsers); doesn't work in Safari, which doesn't trigger a click event at all on select elements; fixes http://plugins.jquery.com/node/11520
* Updated to latest form plugin (2.36), fixing http://plugins.jquery.com/node/11487
* Bind to blur event for equalTo target to revalidate when that target changes, fixes http://plugins.jquery.com/node/11450
* Simplified select validation, delegating to jQuery's val() method to get the select value; should fix http://plugins.jquery.com/node/11239
* Fixed default message for digits (http://plugins.jquery.com/node/9853)
* Fixed issue with cached remote message (http://plugins.jquery.com/node/11029 and http://plugins.jquery.com/node/9351)
* Fixed a missing semicolon in additional-methods.js (http://plugins.jquery.com/node/9233)
* Added automatic detection of substitution parameters in messages, removing the need to provide format functions (http://plugins.jquery.com/node/11195)
* Fixed an issue with :filled/:blank somewhat caused by Sizzle (http://plugins.jquery.com/node/11144)
* Added an integer method to additional-methods.js (http://plugins.jquery.com/node/9612)
* Fixed errorsFor method where the for-attribute contains characters that need escaping to be valid inside a selector (http://plugins.jquery.com/node/9611)

1.5.5
---
* Fix for http://plugins.jquery.com/node/8659
* Fixed trailing comma in messages_cs.js

1.5.4
---
* Fixed remote method bug (http://plugins.jquery.com/node/8658)

1.5.3
---
* Fixed a bug related to the wrapper-option, where all ancestor-elements that matched the wrapper-option where selected (http://plugins.jquery.com/node/7624)
* Updated multipart demo to use latest jQuery UI accordion
* Added dateNL and time methods to additionalMethods.js
* Added Traditional Chinese (Taiwan, tw) and Kazakhstan (KK) localization
* Moved jQuery.format (formerly String.format) to jQuery.validator.format, jQuery.format is deprecated and will be removed in 1.6 (see http://code.google.com/p/jquery-utils/issues/detail?id=15 for details)
* Cleaned up messages_pl.js and messages_ptbr.js (still defined messages for max/min/rangeValue, which were removed in 1.4)
* Fixed flawed boolean logic in valid-plugin-method for multiple elements; now all elements need to be valid for a boolean-true result (http://plugins.jquery.com/node/8481)
* Enhancement $.validator.addMethod: An undefined third message-argument won't overwrite an existing message (http://plugins.jquery.com/node/8443)
* Enhancement to submitHandler option: When used, click events on submit buttons are captured and the submitting button is inserted into the form before calling submitHandler, and removed afterwards; keeps submit buttons intact (http://plugins.jquery.com/node/7183#comment-3585)
* Added option validClass, default "valid", which adds that class to all valid elements, after validation (http://dev.jquery.com/ticket/2205)
* Added creditcardtypes method to additionalMethods.js, including tests (via http://dev.jquery.com/ticket/3635)
* Improved remote method to allow serverside message as a string, or true for valid, or false for invalid using the clientside defined message (http://dev.jquery.com/ticket/3807)
* Improved accept method to also accept a Drupal-style comma-separated list of values (http://plugins.jquery.com/node/8580)

1.5.2
---
* Fixed messages in additional-methods.js for maxWords, minWords, and rangeWords to include call to $.format
* Fixed value passed to methods to exclude carriage return (\r), same as jQuery's val() does
* Added slovak (sk) localization
* Added demo for integration with jQuery UI tabs
* Added selects-grouping example to tabs demo (see second tab, birthdate field)

1.5.1
---
* Updated marketo demo to use invalidHandler option instead of binding invalid-form event
* Added TinyMCE integration example
* Added ukrainian (ua) localization
* Fixed length validation to work with trimmed value (regression from 1.5 where general trimming before validation was removed)
* Various small fixes for compatibility with both 1.2.6 and 1.3

1.5
---
* Improved basic demo, validating confirm-password field after password changed
* Fixed basic validation to pass the untrimmed input value as the first parameter to validation methods, changed required accordingly; breaks existing custom method that rely on the trimming
* Added norwegian (no), italian (it), hungarian (hu) and romanian (ro) localization
* Fixed #3195: Two flaws in swedish localization
* Fixed #3503: Extended rules("add") to accept messages property: use to specify add custom messages to an element via rules("add", { messages: { required: "Required! " } });
* Fixed #3356: Regression from #2908 when using meta-option
* Fixed #3370: Added ignoreTitle option, set to skip reading messages from the title attribute, helps to avoid issues with Google Toolbar; default is false for compatibility
* Fixed #3516: Trigger invalid-form event even when remote validation is involved
* Added invalidHandler option as a shortcut to bind("invalid-form", function() {})
* Fixed Safari issue for loading indicator in ajaxSubmit-integration-demo (append to body first, then hide)
* Added test for creditcard validation and improved default message
* Enhanced remote validation, accepting options to passthrough to $.ajax as parameter (either url string or options, including url property plus everything else that $.ajax supports)

1.4
---
* Fixed #2931, validate elements in document order and ignore type=image inputs
* Fixed usage of $ and jQuery variables, now fully compatible with all variations of noConflict usage
* Implemented #2908, enabling custom messages via metadata ala class="{required:true,messages:{required:'required field'}}", added demo/custom-messages-metadata-demo.html
* Removed deprecated methods minValue (min), maxValue (max), rangeValue (rangevalue), minLength (minlength), maxLength (maxlength), rangeLength (rangelength)
* Fixed #2215 regression: Call unhighlight only for current elements, not everything
* Implemented #2989, enabling image button to cancel validation
* Fixed issue where IE incorrectly validates against maxlength=0
* Added czech (cs) localization
* Reset validator.submitted on validator.resetForm(), enabling a full reset when necessary
* Fixed #3035, skipping all falsy attributes when reading rules (0, undefined, empty string), removed part of the maxlength workaround (for 0)
* Added dutch (nl) localization (#3201)

1.3
---
* Fixed invalid-form event, now only triggered when form is invalid
* Added spanish (es), russian (ru), portuguese brazilian (ptbr), turkish (tr), and polish (pl) localization
* Added removeAttrs plugin to facilitate adding and removing multiple attributes
* Added groups option to display a single message for multiple elements, via groups: { arbitraryGroupName: "fieldName1 fieldName2[, fieldNameN" }
* Enhanced rules() for adding and removing (static) rules: rules("add", "method1[, methodN]"/{method1:param[, method_n:param]}) and rules("remove"[, "method1[, method_n]")
* Enhanced rules-option, accepts space-separated string-list of methods, eg. {birthdate: "required date"}
* Fixed checkbox group validation with inline rules: As long as the rules are specified on the first element, the group is now properly validated on click
* Fixed #2473, ignoring all rules with an explicit parameter of boolean-false, eg. required:false is the same as not specifying required at all (it was handled as required:true so far)
* Fixed #2424, with a modified patch from #2473: Methods returning a dependency-mismatch don't stop other rules from being evaluated anymore; still, success isn't applied for optional fields
* Fixed url and email validation to not use trimmed values
* Fixed creditcard validation to accept only digits and dashes ("asdf" is not a valid creditcard number)
* Allow both button and input elements for cancel buttons (via class="cancel")
* Fixed #2215: Fixed message display to call unhighlight as part of showing and hiding messages, no more visual side-effects while checking an element and extracted validator.checkForm to validate a form without UI sideeffects
* Rewrote custom selectors (:blank, :filled, :unchecked) with functions for compatibility with AIR

1.2.1
-----

* Bundled delegate plugin with validate plugin - its always required anyway
* Improved remote validation to include parts from the ajaxQueue plugin for proper synchronization (no additional plugin necessary)
* Fixed stopRequest to prevent pendingRequest < 0
* Added jQuery.validator.autoCreateRanges property, defaults to false, enable to convert min/max to range and minlength/maxlength to rangelength; this basically fixes the issue introduced by automatically creating ranges in 1.2
* Fixed optional-methods to not highlight anything at all if the field is blank, that is, don't trigger success
* Allow false/null for highlight/unhighlight options instead of forcing a do-nothing-callback even when nothing needs to be highlighted
* Fixed validate() call with no elements selected, returning undefined instead of throwing an error
* Improved demo, replacing metadata with classes/attributes for specifying rules
* Fixed error when no custom message is used for remote validation
* Modified email and url validation to require domain label and top label
* Fixed url and email validation to require TLD (actually to require domain label); 1.2 version (TLD is optional) is moved to additions as url2 and email2
* Fixed dynamic-totals demo in IE6/7 and improved templating, using textarea to store multiline template and string interpolation
* Added login form example with "Email password" link that makes the password field optional
* Enhanced dynamic-totals demo with an example of a single message for two fields

1.2
---

* Added AJAX-captcha validation example (based on http://psyrens.com/captcha/)
* Added remember-the-milk-demo (thanks RTM team for the permission!)
* Added marketo-demo (thanks Glen Lipka!)
* Added support for ajax-validation, see method "remote"; serverside returns JSON, true for valid elements, false or a String for invalid, String is used as message
* Added highlight and unhighlight options, by default toggles errorClass on element, allows custom highlighting
* Added valid() plugin method for easy programmatic checking of forms and fields without the need to use the validator API
* Added rules() plugin method to read and write rules for an element (currently read only)
* Replaced regex for email method, thanks to the contribution by Scott Gonzalez, see http://projects.scottsplayground.com/email_address_validation/
* Restructured event architecture to rely solely on delegation, both improving performance, and ease-of-use for the developer (requires jquery.delegate.js)
* Moved documentation from inline to http://docs.jquery.com/Plugins/Validation - including interactive examples for all methods
* Removed validator.refresh(), validation is now completely dynamic
* Renamed minValue to min, maxValue to max and rangeValue to range, deprecating the previous names (to be removed in 1.3)
* Renamed minLength to minlength, maxLength to maxlength and rangeLength to rangelength, deprecating the previous names (to be removed in 1.3)
* Added feature to merge min + max into and range and minlength + maxlength into rangelength
* Added support for dynamic rule parameters, allowing to specify a function as a parameter eg. for minlength, called when validating the element
* Allow to specify null or an empty string as a message to display nothing (see marketo demo)
* Rules overhaul: Now supports combination of rules-option, metadata, classes (new) and attributes (new), see rules() for details

1.1.2
---

* Replaced regex for URL method, thanks to the contribution by Scott Gonzalez, see http://projects.scottsplayground.com/iri/
* Improved email method to better handle unicode characters
* Fixed error container to hide when all elements are valid, not only on form submit
* Fixed String.format to jQuery.format (moving into jQuery namespace)
* Fixed accept method to accept both upper and lowercase extensions
* Fixed validate() plugin method to create only one validator instance for a given form and always return that one instance (avoids binding events multiple times)
* Changed debug-mode console log from "error" to "warn" level

1.1.1
-----

* Fixed invalid XHTML, preventing error label creation in IE since jQuery 1.1.4
* Fixed and improved String.format: Global search & replace, better handling of array arguments
* Fixed cancel-button handling to use validator-object for storing state instead of form element
* Fixed name selectors to handle "complex" names, eg. containing brackets ("list[]")
* Added button and disabled elements to exclude from validation
* Moved element event handlers to refresh to be able to add handlers to new elements
* Fixed email validation to allow long top level domains (eg. ".travel")
* Moved showErrors() from valid() to form()
* Added validator.size(): returns the number of current errors
* Call submitHandler with validator as scope for easier access of it's methods, eg. to find error labels using errorsFor(Element)
* Compatible with jQuery 1.1.x and 1.2.x

1.1
---

* Added validation on blur, keyup and click (for checkboxes and radiobutton). Replaces event-option.
* Fixed resetForm
* Fixed custom-methods-demo

1.0
---

* Improved number and numberDE methods to check for correct decimal numbers with delimiters
* Only elements that have rules are checked (otherwise success-option is applied to all elements)
* Added creditcard number method (thanks to Brian Klug)
* Added ignore-option, eg. ignore: "[@type=hidden]", using that expression to exclude elements to validate. Default: none, though submit and reset buttons are always ignored
* Heavily enhanced Functions-as-messages by providing a flexible String.format helper
* Accept Functions as messages, providing runtime-custom-messages
* Fixed exclusion of elements without rules from successList
* Fixed custom-method-demo, replaced the alert with message displaying the number of errors
* Fixed form-submit-prevention when using submitHandler
* Completely removed dependency on element IDs, though they are still used (when present) to link error labels to inputs. Achieved by using
  an array with {name, message, element} instead of an object with id:message pairs for the internal errorList.
* Added support for specifying simple rules as simple strings, eg. "required" is equivalent to {required: true}
* Added feature: Add errorClass to invalid field�s parent element, making it easy to style the label/field container or the label for the field.
* Added feature: focusCleanup - If enabled, removes the errorClass from the invalid elements and hides all errors messages whenever the element is focused.
* Added success option to show the a field was validated successfully
* Fixed Opera select-issue (avoiding a attribute-collision)
* Fixed problems with focussing hidden elements in IE
* Added feature to skip validation for submit buttons with class "cancel"
* Fixed potential issues with Google Toolbar by preferring plugin option messages over title attribute
* submitHandler is only called when an actual submit event was handled, validator.form() returns false only for invalid forms
* Invalid elements are now focused only on submit or via validator.focusInvalid(), avoiding all trouble with focus-on-blur
* IE6 error container layout issue is solved
* Customize error element via errorElement option
* Added validator.refresh() to find new inputs in the form
* Added accept validation method, checks file extensions
* Improved dependency feature by adding two custom expressions: ":blank" to select elements with an empty value and �:filled� to select elements with a value, both excluding whitespace
* Added a resetForm() method to the validator: Resets each form element (using the form plugin, if available), removes classes on invalid elements and hides all error messages
* Fixed docs for validator.showErrors()
* Fixed error label creation to always use html() instead of text(), allowing arbitrary HTML passed in as messages
* Fixed error label creation to use specified error class
* Added dependency feature: The requires method accepts both String (jQuery expressions) and Functions as the argument
* Heavily improved customizing of error message display: Use normal messages and show/hide an additional container; Completely replace message display with own mechanism (while being able to delegate to the default handler; Customize placing of generated labels (instead of default below-element)
* Fixed two major bugs in IE (error containers) and Opera (metadata)
* Modified validation methods to accept empty fields as valid (exception: of course �required� and also �equalTo� methods)
* Renamed "min" to "minLength", "max" to "maxLength", "length" to "rangeLength"
* Added "minValue", "maxValue" and "rangeValue"
* Streamlined API for support of different events. The default, submit, can be disabled. If any event is specified, that is applied to each element (instead of the entire form). Combining keyup-validation with submit-validation is now extremely easy to setup
* Added support for one-message-per-rule when defining messages via plugin settings
* Added support to wrap metadata in some parent element. Useful when metadata is used for other plugins, too.
* Refactored tests and demos: Less files, better demos
* Improved documentation: More examples for methods, more reference texts explaining some basics
