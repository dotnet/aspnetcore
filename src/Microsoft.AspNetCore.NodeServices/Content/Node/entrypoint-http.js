(function(e, a) { for(var i in a) e[i] = a[i]; }(exports, /******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};

/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {

/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId])
/******/ 			return installedModules[moduleId].exports;

/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			exports: {},
/******/ 			id: moduleId,
/******/ 			loaded: false
/******/ 		};

/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);

/******/ 		// Flag the module as loaded
/******/ 		module.loaded = true;

/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}


/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;

/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;

/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";

/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(0);
/******/ })
/************************************************************************/
/******/ ([
/* 0 */
/***/ function(module, exports, __webpack_require__) {

	module.exports = __webpack_require__(1);


/***/ },
/* 1 */
/***/ function(module, exports, __webpack_require__) {

	"use strict";
	// Limit dependencies to core Node modules. This means the code in this file has to be very low-level and unattractive,
	// but simplifies things for the consumer of this module.
	var http = __webpack_require__(2);
	var path = __webpack_require__(3);
	var ArgsUtil_1 = __webpack_require__(4);
	var AutoQuit_1 = __webpack_require__(5);
	// Webpack doesn't support dynamic requires for files not present at compile time, so grab a direct
	// reference to Node's runtime 'require' function.
	var dynamicRequire = eval('require');
	var parsedArgs = ArgsUtil_1.parseArgs(process.argv);
	if (parsedArgs.watch) {
	    AutoQuit_1.autoQuitOnFileChange(process.cwd(), parsedArgs.watch.split(','));
	}
	var server = http.createServer(function (req, res) {
	    readRequestBodyAsJson(req, function (bodyJson) {
	        var hasSentResult = false;
	        var callback = function (errorValue, successValue) {
	            if (!hasSentResult) {
	                hasSentResult = true;
	                if (errorValue) {
	                    res.statusCode = 500;
	                    if (errorValue.stack) {
	                        res.end(errorValue.stack);
	                    }
	                    else {
	                        res.end(errorValue.toString());
	                    }
	                }
	                else if (typeof successValue !== 'string') {
	                    // Arbitrary object/number/etc - JSON-serialize it
	                    res.setHeader('Content-Type', 'application/json');
	                    res.end(JSON.stringify(successValue));
	                }
	                else {
	                    // String - can bypass JSON-serialization altogether
	                    res.setHeader('Content-Type', 'text/plain');
	                    res.end(successValue);
	                }
	            }
	        };
	        // Support streamed responses
	        Object.defineProperty(callback, 'stream', {
	            enumerable: true,
	            get: function () {
	                if (!hasSentResult) {
	                    hasSentResult = true;
	                    res.setHeader('Content-Type', 'application/octet-stream');
	                }
	                return res;
	            }
	        });
	        try {
	            var resolvedPath = path.resolve(process.cwd(), bodyJson.moduleName);
	            var invokedModule = dynamicRequire(resolvedPath);
	            var func = bodyJson.exportedFunctionName ? invokedModule[bodyJson.exportedFunctionName] : invokedModule;
	            if (!func) {
	                throw new Error('The module "' + resolvedPath + '" has no export named "' + bodyJson.exportedFunctionName + '"');
	            }
	            func.apply(null, [callback].concat(bodyJson.args));
	        }
	        catch (synchronousException) {
	            callback(synchronousException, null);
	        }
	    });
	});
	var requestedPortOrZero = parsedArgs.port || 0; // 0 means 'let the OS decide'
	server.listen(requestedPortOrZero, 'localhost', function () {
	    // Signal to HttpNodeHost which port it should make its HTTP connections on
	    console.log('[Microsoft.AspNetCore.NodeServices.HttpNodeHost:Listening on port ' + server.address().port + '\]');
	    // Signal to the NodeServices base class that we're ready to accept invocations
	    console.log('[Microsoft.AspNetCore.NodeServices:Listening]');
	});
	function readRequestBodyAsJson(request, callback) {
	    var requestBodyAsString = '';
	    request
	        .on('data', function (chunk) { requestBodyAsString += chunk; })
	        .on('end', function () { callback(JSON.parse(requestBodyAsString)); });
	}


/***/ },
/* 2 */
/***/ function(module, exports) {

	module.exports = require("http");

/***/ },
/* 3 */
/***/ function(module, exports) {

	module.exports = require("path");

/***/ },
/* 4 */
/***/ function(module, exports) {

	"use strict";
	function parseArgs(args) {
	    // Very simplistic parsing which is sufficient for the cases needed. We don't want to bring in any external
	    // dependencies (such as an args-parsing library) to this file.
	    var result = {};
	    var currentKey = null;
	    args.forEach(function (arg) {
	        if (arg.indexOf('--') === 0) {
	            var argName = arg.substring(2);
	            result[argName] = undefined;
	            currentKey = argName;
	        }
	        else if (currentKey) {
	            result[currentKey] = arg;
	            currentKey = null;
	        }
	    });
	    return result;
	}
	exports.parseArgs = parseArgs;


/***/ },
/* 5 */
/***/ function(module, exports, __webpack_require__) {

	"use strict";
	var fs = __webpack_require__(6);
	var path = __webpack_require__(3);
	function autoQuitOnFileChange(rootDir, extensions) {
	    // Note: This will only work on Windows/OS X, because the 'recursive' option isn't supported on Linux.
	    // Consider using a different watch mechanism (though ideally without forcing further NPM dependencies).
	    fs.watch(rootDir, { persistent: false, recursive: true }, function (event, filename) {
	        var ext = path.extname(filename);
	        if (extensions.indexOf(ext) >= 0) {
	            console.log('Restarting due to file change: ' + filename);
	            process.exit(0);
	        }
	    });
	}
	exports.autoQuitOnFileChange = autoQuitOnFileChange;


/***/ },
/* 6 */
/***/ function(module, exports) {

	module.exports = require("fs");

/***/ }
/******/ ])));