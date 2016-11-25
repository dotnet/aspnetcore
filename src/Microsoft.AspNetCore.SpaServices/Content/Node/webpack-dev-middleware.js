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

	module.exports = __webpack_require__(4);


/***/ },
/* 1 */,
/* 2 */,
/* 3 */,
/* 4 */
/***/ function(module, exports, __webpack_require__) {

	"use strict";
	// Pass through the invocation to the 'aspnet-webpack' package, verifying that it can be loaded
	function createWebpackDevServer(callback) {
	    var aspNetWebpack;
	    try {
	        aspNetWebpack = __webpack_require__(5);
	    }
	    catch (ex) {
	        // Developers sometimes have trouble with badly-configured Node installations, where it's unable
	        // to find node_modules. Or they accidentally fail to deploy node_modules, or even to run 'npm install'.
	        // Make sure such errors are reported back to the .NET part of the app.
	        callback('Webpack dev middleware failed because of an error while loading \'aspnet-webpack\'. Error was: '
	            + ex.stack
	            + '\nCurrent directory is: '
	            + process.cwd());
	        return;
	    }
	    return aspNetWebpack.createWebpackDevServer.apply(this, arguments);
	}
	exports.createWebpackDevServer = createWebpackDevServer;


/***/ },
/* 5 */
/***/ function(module, exports) {

	module.exports = require("aspnet-webpack");

/***/ }
/******/ ])));