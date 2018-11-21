(function (e, a) { for (var i in a) e[i] = a[i]; }(exports, /******/(function (modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if (installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
            /******/
        }
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
            /******/
        };
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
        /******/
    }
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function (exports, name, getter) {
/******/ 		if (!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, { enumerable: true, get: getter });
            /******/
        }
        /******/
    };
/******/
/******/ 	// define __esModule on exports
/******/ 	__webpack_require__.r = function (exports) {
/******/ 		if (typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 			Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
            /******/
        }
/******/ 		Object.defineProperty(exports, '__esModule', { value: true });
        /******/
    };
/******/
/******/ 	// create a fake namespace object
/******/ 	// mode & 1: value is a module id, require it
/******/ 	// mode & 2: merge all properties of value into the ns
/******/ 	// mode & 4: return value when already ns object
/******/ 	// mode & 8|1: behave like require
/******/ 	__webpack_require__.t = function (value, mode) {
/******/ 		if (mode & 1) value = __webpack_require__(value);
/******/ 		if (mode & 8) return value;
/******/ 		if ((mode & 4) && typeof value === 'object' && value && value.__esModule) return value;
/******/ 		var ns = Object.create(null);
/******/ 		__webpack_require__.r(ns);
/******/ 		Object.defineProperty(ns, 'default', { enumerable: true, value: value });
/******/ 		if (mode & 2 && typeof value != 'string') for (var key in value) __webpack_require__.d(ns, key, function (key) { return value[key]; }.bind(null, key));
/******/ 		return ns;
        /******/
    };
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function (module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
        /******/
    };
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function (object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = 0);
    /******/
})
/************************************************************************/
/******/([
/* 0 */
/***/ (function (module, exports, __webpack_require__) {

            module.exports = __webpack_require__(1);


            /***/
        }),
/* 1 */
/***/ (function (module, exports, __webpack_require__) {

            "use strict";

            exports.__esModule = true;
            // Limit dependencies to core Node modules. This means the code in this file has to be very low-level and unattractive,
            // but simplifies things for the consumer of this module.
            __webpack_require__(2);
            var net = __webpack_require__(3);
            var path = __webpack_require__(4);
            var readline = __webpack_require__(5);
            var ArgsUtil_1 = __webpack_require__(6);
            var ExitWhenParentExits_1 = __webpack_require__(7);
            var virtualConnectionServer = __webpack_require__(8);
            // Webpack doesn't support dynamic requires for files not present at compile time, so grab a direct
            // reference to Node's runtime 'require' function.
            var dynamicRequire = eval('require');
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
                        // Prepare a callback for accepting non-streamed JSON responses
                        var hasInvokedCallback_1 = false;
                        var invocationCallback = function (errorValue, successValue) {
                            if (hasInvokedCallback_1) {
                                throw new Error('Cannot supply more than one result. The callback has already been invoked,'
                                    + ' or the result stream has already been accessed');
                            }
                            hasInvokedCallback_1 = true;
                            connection.end(JSON.stringify({
                                result: successValue,
                                errorMessage: errorValue && (errorValue.message || errorValue),
                                errorDetails: errorValue && (errorValue.stack || null)
                            }));
                        };
                        // Also support streamed binary responses
                        Object.defineProperty(invocationCallback, 'stream', {
                            enumerable: true,
                            get: function () {
                                hasInvokedCallback_1 = true;
                                return connection;
                            }
                        });
                        // Actually invoke it, passing through any supplied args
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
            var parsedArgs = ArgsUtil_1.parseArgs(process.argv);
            var listenAddress = (useWindowsNamedPipes ? '\\\\.\\pipe\\' : '/tmp/') + parsedArgs.listenAddress;
            server.listen(listenAddress);
            ExitWhenParentExits_1.exitWhenParentExits(parseInt(parsedArgs.parentPid), /* ignoreSigint */ true);


            /***/
        }),
/* 2 */
/***/ (function (module, exports) {

            // When Node writes to stdout/strerr, we capture that and convert the lines into calls on the
            // active .NET ILogger. But by default, stdout/stderr don't have any way of distinguishing
            // linebreaks inside log messages from the linebreaks that delimit separate log messages,
            // so multiline strings will end up being written to the ILogger as multiple independent
            // log messages. This makes them very hard to make sense of, especially when they represent
            // something like stack traces.
            //
            // To fix this, we intercept stdout/stderr writes, and replace internal linebreaks with a
            // marker token. When .NET receives the lines, it converts the marker tokens back to regular
            // linebreaks within the logged messages.
            //
            // Note that it's better to do the interception at the stdout/stderr level, rather than at
            // the console.log/console.error (etc.) level, because this takes place after any native
            // message formatting has taken place (e.g., inserting values for % placeholders).
            var findInternalNewlinesRegex = /\n(?!$)/g;
            var encodedNewline = '__ns_newline__';
            encodeNewlinesWrittenToStream(process.stdout);
            encodeNewlinesWrittenToStream(process.stderr);
            function encodeNewlinesWrittenToStream(outputStream) {
                var origWriteFunction = outputStream.write;
                outputStream.write = function (value) {
                    // Only interfere with the write if it's definitely a string
                    if (typeof value === 'string') {
                        var argsClone = Array.prototype.slice.call(arguments, 0);
                        argsClone[0] = encodeNewlinesInString(value);
                        origWriteFunction.apply(this, argsClone);
                    }
                    else {
                        origWriteFunction.apply(this, arguments);
                    }
                };
            }
            function encodeNewlinesInString(str) {
                return str.replace(findInternalNewlinesRegex, encodedNewline);
            }


            /***/
        }),
/* 3 */
/***/ (function (module, exports) {

            module.exports = require("net");

            /***/
        }),
/* 4 */
/***/ (function (module, exports) {

            module.exports = require("path");

            /***/
        }),
/* 5 */
/***/ (function (module, exports) {

            module.exports = require("readline");

            /***/
        }),
/* 6 */
/***/ (function (module, exports, __webpack_require__) {

            "use strict";

            exports.__esModule = true;
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


            /***/
        }),
/* 7 */
/***/ (function (module, exports, __webpack_require__) {

            "use strict";

            /*
            In general, we want the Node child processes to be terminated as soon as the parent .NET processes exit,
            because we have no further use for them. If the .NET process shuts down gracefully, it will run its
            finalizers, one of which (in OutOfProcessNodeInstance.cs) will kill its associated Node process immediately.
            
            But if the .NET process is terminated forcefully (e.g., on Linux/OSX with 'kill -9'), then it won't have
            any opportunity to shut down its child processes, and by default they will keep running. In this case, it's
            up to the child process to detect this has happened and terminate itself.
            
            There are many possible approaches to detecting when a parent process has exited, most of which behave
            differently between Windows and Linux/OS X:
            
             - On Windows, the parent process can mark its child as being a 'job' that should auto-terminate when
               the parent does (http://stackoverflow.com/a/4657392). Not cross-platform.
             - The child Node process can get a callback when the parent disconnects (process.on('disconnect', ...)).
               But despite http://stackoverflow.com/a/16487966, no callback fires in any case I've tested (Windows / OS X).
             - The child Node process can get a callback when its stdin/stdout are disconnected, as described at
               http://stackoverflow.com/a/15693934. This works well on OS X, but calling stdout.resume() on Windows
               causes the process to terminate prematurely.
             - I don't know why, but on Windows, it's enough to invoke process.stdin.resume(). For some reason this causes
               the child Node process to exit as soon as the parent one does, but I don't see this documented anywhere.
             - You can poll to see if the parent process, or your stdin/stdout connection to it, is gone
               - You can directly pass a parent process PID to the child, and then have the child poll to see if it's
                 still running (e.g., using process.kill(pid, 0), which doesn't kill it but just tests whether it exists,
                 as per https://nodejs.org/api/process.html#process_process_kill_pid_signal)
               - Or, on each poll, you can try writing to process.stdout. If the parent has died, then this will throw.
                 However I don't see this documented anywhere. It would be nice if you could just poll for whether or not
                 process.stdout is still connected (without actually writing to it) but I haven't found any property whose
                 value changes until you actually try to write to it.
            
            Of these, the only cross-platform approach that is actually documented as a valid strategy is simply polling
            to check whether the parent PID is still running. So that's what we do here.
            */
            exports.__esModule = true;
            var pollIntervalMs = 1000;
            function exitWhenParentExits(parentPid, ignoreSigint) {
                setInterval(function () {
                    if (!processExists(parentPid)) {
                        // Can't log anything at this point, because out stdout was connected to the parent,
                        // but the parent is gone.
                        process.exit();
                    }
                }, pollIntervalMs);
                if (ignoreSigint) {
                    // Pressing ctrl+c in the terminal sends a SIGINT to all processes in the foreground process tree.
                    // By default, the Node process would then exit before the .NET process, because ASP.NET implements
                    // a delayed shutdown to allow ongoing requests to complete.
                    //
                    // This is problematic, because if Node exits first, the CopyToAsync code in ConditionalProxyMiddleware
                    // will experience a read fault, and logs a huge load of errors. Fortunately, since the Node process is
                    // already set up to shut itself down if it detects the .NET process is terminated, all we have to do is
                    // ignore the SIGINT. The Node process will then terminate automatically after the .NET process does.
                    //
                    // A better solution would be to have WebpackDevMiddleware listen for SIGINT and gracefully close any
                    // ongoing EventSource connections before letting the Node process exit, independently of the .NET
                    // process exiting. However, doing this well in general is very nontrivial (see all the discussion at
                    // https://github.com/nodejs/node/issues/2642).
                    process.on('SIGINT', function () {
                        console.log('Received SIGINT. Waiting for .NET process to exit...');
                    });
                }
            }
            exports.exitWhenParentExits = exitWhenParentExits;
            function processExists(pid) {
                try {
                    // Sending signal 0 - on all platforms - tests whether the process exists. As long as it doesn't
                    // throw, that means it does exist.
                    process.kill(pid, 0);
                    return true;
                }
                catch (ex) {
                    // If the reason for the error is that we don't have permission to ask about this process,
                    // report that as a separate problem.
                    if (ex.code === 'EPERM') {
                        throw new Error("Attempted to check whether process " + pid + " was running, but got a permissions error.");
                    }
                    return false;
                }
            }


            /***/
        }),
/* 8 */
/***/ (function (module, exports, __webpack_require__) {

            "use strict";

            exports.__esModule = true;
            var events_1 = __webpack_require__(9);
            var VirtualConnection_1 = __webpack_require__(10);
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
            var VirtualConnectionsCollection = /** @class */ (function () {
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
                    var newVirtualConnection = new VirtualConnection_1.VirtualConnection(beginWriteCallback);
                    newVirtualConnection.on('end', function () {
                        // The virtual connection was closed remotely. Clean up locally.
                        _this._onVirtualConnectionWasClosed(header.connectionIdString);
                    });
                    newVirtualConnection.on('finish', function () {
                        // The virtual connection was closed locally. Clean up locally, and notify the remote that we're done.
                        _this._onVirtualConnectionWasClosed(header.connectionIdString);
                        _this._sendFrame(header.connectionIdBinary, Buffer.alloc(0));
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
                    var buf = Buffer.alloc(4);
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


            /***/
        }),
/* 9 */
/***/ (function (module, exports) {

            module.exports = require("events");

            /***/
        }),
/* 10 */
/***/ (function (module, exports, __webpack_require__) {

            "use strict";

            var __extends = (this && this.__extends) || (function () {
                var extendStatics = function (d, b) {
                    extendStatics = Object.setPrototypeOf ||
                        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
                        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
                    return extendStatics(d, b);
                }
                return function (d, b) {
                    extendStatics(d, b);
                    function __() { this.constructor = d; }
                    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
                };
            })();
            exports.__esModule = true;
            var stream_1 = __webpack_require__(11);
            /**
             * Represents a virtual connection. Multiple virtual connections may be multiplexed over a single physical socket connection.
             */
            var VirtualConnection = /** @class */ (function (_super) {
                __extends(VirtualConnection, _super);
                function VirtualConnection(_beginWriteCallback) {
                    var _this = _super.call(this) || this;
                    _this._beginWriteCallback = _beginWriteCallback;
                    _this._flowing = false;
                    _this._receivedDataQueue = [];
                    return _this;
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
                        chunk = Buffer.from(chunk, encodingIfString);
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


            /***/
        }),
/* 11 */
/***/ (function (module, exports) {

            module.exports = require("stream");

            /***/
        })
/******/])));
