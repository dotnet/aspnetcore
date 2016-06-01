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
	var fs = __webpack_require__(2);
	var net = __webpack_require__(3);
	var path = __webpack_require__(4);
	var readline = __webpack_require__(5);
	var virtualConnectionServer = __webpack_require__(6);
	// Webpack doesn't support dynamic requires for files not present at compile time, so grab a direct
	// reference to Node's runtime 'require' function.
	var dynamicRequire = eval('require');
	var parsedArgs = parseArgs(process.argv);
	if (parsedArgs.watch) {
	    autoQuitOnFileChange(process.cwd(), parsedArgs.watch.split(','));
	}
	// Signal to the .NET side when we're ready to accept invocations
	var server = net.createServer().on('listening', function () {
	    console.log('[Microsoft.AspNetCore.NodeServices:Listening]');
	});
	// Each virtual connection represents a separate invocation
	virtualConnectionServer.createInterface(server).on('connection', function (connection) {
	    readline.createInterface(connection, null).on('line', function (line) {
	        try {
	            // Get a reference to the function to invoke
	            var invocation = JSON.parse(line);
	            var invokedModule = dynamicRequire(path.resolve(process.cwd(), invocation.moduleName));
	            var invokedFunction = invocation.exportedFunctionName ? invokedModule[invocation.exportedFunctionName] : invokedModule;
	            // Actually invoke it, passing the callback followed by any supplied args
	            var invocationCallback = function (errorValue, successValue) {
	                connection.end(JSON.stringify({
	                    result: successValue,
	                    errorMessage: errorValue && (errorValue.message || errorValue),
	                    errorDetails: errorValue && (errorValue.stack || null)
	                }));
	            };
	            invokedFunction.apply(null, [invocationCallback].concat(invocation.args));
	        }
	        catch (ex) {
	            connection.end(JSON.stringify({
	                errorMessage: ex.message,
	                errorDetails: ex.stack
	            }));
	        }
	    });
	});
	// Begin listening now. The underlying transport varies according to the runtime platform.
	// On Windows it's Named Pipes; on Linux/OSX it's Domain Sockets.
	var useWindowsNamedPipes = /^win/.test(process.platform);
	var listenAddress = (useWindowsNamedPipes ? '\\\\.\\pipe\\' : '/tmp/') + parsedArgs.pipename;
	server.listen(listenAddress);
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


/***/ },
/* 2 */
/***/ function(module, exports) {

	module.exports = require("fs");

/***/ },
/* 3 */
/***/ function(module, exports) {

	module.exports = require("net");

/***/ },
/* 4 */
/***/ function(module, exports) {

	module.exports = require("path");

/***/ },
/* 5 */
/***/ function(module, exports) {

	module.exports = require("readline");

/***/ },
/* 6 */
/***/ function(module, exports, __webpack_require__) {

	"use strict";
	var events_1 = __webpack_require__(7);
	var VirtualConnection_1 = __webpack_require__(8);
	// Keep this in sync with the equivalent constant in the .NET code. Both sides split up their transmissions into frames with this max length,
	// and both will reject longer frames.
	var MaxFrameBodyLength = 16 * 1024;
	/**
	 * Accepts connections to a net.Server and adapts them to behave as multiplexed connections. That is, for each physical socket connection,
	 * we track a list of 'virtual connections' whose API is a Duplex stream. The remote clients may open and close as many virtual connections
	 * as they wish, reading and writing to them independently, without the overhead of establishing new physical connections each time.
	 */
	function createInterface(server) {
	    var emitter = new events_1.EventEmitter();
	    server.on('connection', function (socket) {
	        // For each physical socket connection, maintain a set of virtual connections. Issue a notification whenever
	        // a new virtual connections is opened.
	        var childSockets = new VirtualConnectionsCollection(socket, function (virtualConnection) {
	            emitter.emit('connection', virtualConnection);
	        });
	    });
	    return emitter;
	}
	exports.createInterface = createInterface;
	/**
	 * Tracks the 'virtual connections' associated with a single physical socket connection.
	 */
	var VirtualConnectionsCollection = (function () {
	    function VirtualConnectionsCollection(_socket, _onVirtualConnectionCallback) {
	        var _this = this;
	        this._socket = _socket;
	        this._onVirtualConnectionCallback = _onVirtualConnectionCallback;
	        this._currentFrameHeader = null;
	        this._virtualConnections = {};
	        // If the remote end closes the physical socket, treat all the virtual connections as being closed remotely too
	        this._socket.on('close', function () {
	            Object.getOwnPropertyNames(_this._virtualConnections).forEach(function (id) {
	                // A 'null' frame signals that the connection was closed remotely
	                _this._virtualConnections[id].onReceivedData(null);
	            });
	        });
	        this._socket.on('readable', this._onIncomingDataAvailable.bind(this));
	    }
	    /**
	     * This is called whenever the underlying socket signals that it may have some data available to read. It will synchronously read as many
	     * message frames as it can from the underlying socket, opens virtual connections as needed, and dispatches data to them.
	     */
	    VirtualConnectionsCollection.prototype._onIncomingDataAvailable = function () {
	        var exhaustedAllData = false;
	        while (!exhaustedAllData) {
	            // We might already have a pending frame header from the previous time this method ran, but if not, that's the next thing we need to read
	            if (this._currentFrameHeader === null) {
	                this._currentFrameHeader = this._readNextFrameHeader();
	            }
	            if (this._currentFrameHeader === null) {
	                // There's not enough data to fill a frameheader, so wait until more arrives later
	                // The next attempt to read from the socket will start from the same place this one did (incomplete reads don't consume any data)
	                exhaustedAllData = true;
	            }
	            else {
	                var frameBodyLength = this._currentFrameHeader.bodyLength;
	                var frameBodyOrNull = frameBodyLength > 0 ? this._socket.read(this._currentFrameHeader.bodyLength) : null;
	                if (frameBodyOrNull !== null || frameBodyLength === 0) {
	                    // We have a complete frame header+body pair, so we can now dispatch this to a virtual connection. We set _currentFrameHeader back to null
	                    // so that the next thing we try to read is the next frame header.
	                    var headerCopy = this._currentFrameHeader;
	                    this._currentFrameHeader = null;
	                    this._onReceivedCompleteFrame(headerCopy, frameBodyOrNull);
	                }
	                else {
	                    // There's not enough data to fill the pending frame body, so wait until more arrives later
	                    // The next attempt to read from the socket will start from the same place this one did (incomplete reads don't consume any data)
	                    exhaustedAllData = true;
	                }
	            }
	        }
	    };
	    VirtualConnectionsCollection.prototype._onReceivedCompleteFrame = function (header, bodyIfNotEmpty) {
	        // An incoming zero-length frame signals that there's no more data to read.
	        // Signal this to the Node stream APIs by pushing a 'null' chunk to it.
	        var virtualConnection = this._getOrOpenVirtualConnection(header);
	        virtualConnection.onReceivedData(header.bodyLength > 0 ? bodyIfNotEmpty : null);
	    };
	    VirtualConnectionsCollection.prototype._getOrOpenVirtualConnection = function (header) {
	        if (this._virtualConnections.hasOwnProperty(header.connectionIdString)) {
	            // It's an existing virtual connection
	            return this._virtualConnections[header.connectionIdString];
	        }
	        else {
	            // It's a new one
	            return this._openVirtualConnection(header);
	        }
	    };
	    VirtualConnectionsCollection.prototype._openVirtualConnection = function (header) {
	        var _this = this;
	        var beginWriteCallback = function (data, writeCompletedCallback) {
	            // Only send nonempty frames, since empty ones are a signal to close the virtual connection
	            if (data.length > 0) {
	                _this._sendFrame(header.connectionIdBinary, data, writeCompletedCallback);
	            }
	        };
	        var newVirtualConnection = new VirtualConnection_1.VirtualConnection(beginWriteCallback)
	            .on('end', function () {
	            // The virtual connection was closed remotely. Clean up locally.
	            _this._onVirtualConnectionWasClosed(header.connectionIdString);
	        }).on('finish', function () {
	            // The virtual connection was closed locally. Clean up locally, and notify the remote that we're done.
	            _this._onVirtualConnectionWasClosed(header.connectionIdString);
	            _this._sendFrame(header.connectionIdBinary, new Buffer(0));
	        });
	        this._virtualConnections[header.connectionIdString] = newVirtualConnection;
	        this._onVirtualConnectionCallback(newVirtualConnection);
	        return newVirtualConnection;
	    };
	    /**
	     * Attempts to read a complete frame header, synchronously, from the underlying socket.
	     * If not enough data is available synchronously, returns null without consuming any data from the socket.
	     */
	    VirtualConnectionsCollection.prototype._readNextFrameHeader = function () {
	        var headerBuf = this._socket.read(12);
	        if (headerBuf !== null) {
	            // We have enough data synchronously
	            var connectionIdBinary = headerBuf.slice(0, 8);
	            var connectionIdString = connectionIdBinary.toString('hex');
	            var bodyLength = headerBuf.readInt32LE(8);
	            if (bodyLength < 0 || bodyLength > MaxFrameBodyLength) {
	                // Throwing here is going to bring down the whole process, so this cannot be allowed to happen in real use.
	                // But it won't happen in real use, because this is only used with our .NET client, which doesn't violate this rule.
	                throw new Error('Illegal frame body length: ' + bodyLength);
	            }
	            return { connectionIdBinary: connectionIdBinary, connectionIdString: connectionIdString, bodyLength: bodyLength };
	        }
	        else {
	            // Not enough bytes are available synchronously, so none were consumed
	            return null;
	        }
	    };
	    VirtualConnectionsCollection.prototype._sendFrame = function (connectionIdBinary, data, callback) {
	        // For all sends other than the last one, only invoke the callback if it failed.
	        // Also, only invoke the callback at most once.
	        var hasInvokedCallback = false;
	        var finalCallback = callback && (function (error) {
	            if (!hasInvokedCallback) {
	                hasInvokedCallback = true;
	                callback(error);
	            }
	        });
	        var notFinalCallback = callback && (function (error) {
	            if (error) {
	                finalCallback(error);
	            }
	        });
	        // The amount of data we're writing might exceed MaxFrameBodyLength, so split into frames as needed.
	        // Note that we always send at least one frame, even if it's empty (because that's the close-virtual-connection signal).
	        // If needed, this could be changed to send frames asynchronously, so that large sends could proceed in parallel
	        // (though that would involve making a clone of 'data', to avoid the risk of it being mutated during the send).
	        var bytesSent = 0;
	        do {
	            var nextFrameBodyLength = Math.min(MaxFrameBodyLength, data.length - bytesSent);
	            var isFinalChunk = (bytesSent + nextFrameBodyLength) === data.length;
	            this._socket.write(connectionIdBinary, notFinalCallback);
	            this._sendInt32LE(nextFrameBodyLength, notFinalCallback);
	            this._socket.write(data.slice(bytesSent, bytesSent + nextFrameBodyLength), isFinalChunk ? finalCallback : notFinalCallback);
	            bytesSent += nextFrameBodyLength;
	        } while (bytesSent < data.length);
	    };
	    /**
	     * Sends a number serialized in the correct format for .NET to receive as a System.Int32
	     */
	    VirtualConnectionsCollection.prototype._sendInt32LE = function (value, callback) {
	        var buf = new Buffer(4);
	        buf.writeInt32LE(value, 0);
	        this._socket.write(buf, callback);
	    };
	    VirtualConnectionsCollection.prototype._onVirtualConnectionWasClosed = function (id) {
	        if (this._virtualConnections.hasOwnProperty(id)) {
	            delete this._virtualConnections[id];
	        }
	    };
	    return VirtualConnectionsCollection;
	}());


/***/ },
/* 7 */
/***/ function(module, exports) {

	module.exports = require("events");

/***/ },
/* 8 */
/***/ function(module, exports, __webpack_require__) {

	"use strict";
	var __extends = (this && this.__extends) || function (d, b) {
	    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
	    function __() { this.constructor = d; }
	    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
	};
	var stream_1 = __webpack_require__(9);
	/**
	 * Represents a virtual connection. Multiple virtual connections may be multiplexed over a single physical socket connection.
	 */
	var VirtualConnection = (function (_super) {
	    __extends(VirtualConnection, _super);
	    function VirtualConnection(_beginWriteCallback) {
	        _super.call(this);
	        this._beginWriteCallback = _beginWriteCallback;
	        this._flowing = false;
	        this._receivedDataQueue = [];
	    }
	    VirtualConnection.prototype._read = function () {
	        this._flowing = true;
	        // Keep pushing data until we run out, or the underlying framework asks us to stop.
	        // When we finish, the 'flowing' state is detemined by whether more data is still being requested.
	        while (this._flowing && this._receivedDataQueue.length > 0) {
	            var nextChunk = this._receivedDataQueue.shift();
	            this._flowing = this.push(nextChunk);
	        }
	    };
	    VirtualConnection.prototype._write = function (chunk, encodingIfString, callback) {
	        if (typeof chunk === 'string') {
	            chunk = new Buffer(chunk, encodingIfString);
	        }
	        this._beginWriteCallback(chunk, callback);
	    };
	    VirtualConnection.prototype.onReceivedData = function (dataOrNullToSignalEOF) {
	        if (this._flowing) {
	            this._flowing = this.push(dataOrNullToSignalEOF);
	        }
	        else {
	            this._receivedDataQueue.push(dataOrNullToSignalEOF);
	        }
	    };
	    return VirtualConnection;
	}(stream_1.Duplex));
	exports.VirtualConnection = VirtualConnection;


/***/ },
/* 9 */
/***/ function(module, exports) {

	module.exports = require("stream");

/***/ }
/******/ ])));