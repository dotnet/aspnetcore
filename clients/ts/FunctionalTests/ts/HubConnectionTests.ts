// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { DefaultHttpClient, HttpClient, HttpRequest, HttpResponse, HttpTransportType, HubConnection, IHubConnectionOptions, JsonHubProtocol, LogLevel } from "@aspnet/signalr";
import { MessagePackHubProtocol } from "@aspnet/signalr-protocol-msgpack";

import { eachTransport, eachTransportAndProtocol } from "./Common";
import { TestLogger } from "./TestLogger";

const TESTHUBENDPOINT_URL = "/testhub";
const TESTHUB_NOWEBSOCKETS_ENDPOINT_URL = "/testhub-nowebsockets";

const commonOptions: IHubConnectionOptions = {
    logMessageContent: true,
    logger: TestLogger.instance,
};

// On slower CI machines, these tests sometimes take longer than 5s
jasmine.DEFAULT_TIMEOUT_INTERVAL = 10 * 1000;

describe("hubConnection", () => {
    eachTransportAndProtocol((transportType, protocol) => {
        describe("using " + protocol.name + " over " + HttpTransportType[transportType] + " transport", async () => {
            it("can invoke server method and receive result", (done) => {
                const message = "你好，世界！";

                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });
                hubConnection.onclose((error) => {
                    expect(error).toBeUndefined();
                    done();
                });

                hubConnection.start().then(() => {
                    hubConnection.invoke("Echo", message).then((result) => {
                        expect(result).toBe(message);
                    }).catch((e) => {
                        fail(e);
                    }).then(() => {
                        hubConnection.stop();
                    });
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("can invoke server method non-blocking and not receive result", (done) => {
                const message = "你好，世界！";

                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });
                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    done();
                });

                hubConnection.start().then(() => {
                    hubConnection.send("Echo", message).catch((e) => {
                        fail(e);
                    }).then(() => {
                        hubConnection.stop();
                    });
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("can invoke server method structural object and receive structural result", (done) => {
                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });

                hubConnection.on("CustomObject", (customObject) => {
                    expect(customObject.Name).toBe("test");
                    expect(customObject.Value).toBe(42);
                    hubConnection.stop();
                });

                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    done();
                });

                hubConnection.start().then(() => {
                    hubConnection.send("SendCustomObject", { Name: "test", Value: 42 });
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("can stream server method and receive result", (done) => {
                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });

                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    done();
                });

                const received = [];
                hubConnection.start().then(() => {
                    hubConnection.stream("Stream").subscribe({
                        complete: function complete() {
                            expect(received).toEqual(["a", "b", "c"]);
                            hubConnection.stop();
                        },
                        error: function error(err) {
                            fail(err);
                            hubConnection.stop();
                        },
                        next: function next(item) {
                            received.push(item);
                        },
                    });
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("rethrows an exception from the server when invoking", (done) => {
                const errorMessage = "An unexpected error occurred invoking 'ThrowException' on the server. InvalidOperationException: An error occurred.";
                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });

                hubConnection.start().then(() => {
                    hubConnection.invoke("ThrowException", "An error occurred.").then(() => {
                        // exception expected but none thrown
                        fail();
                    }).catch((e) => {
                        expect(e.message).toBe(errorMessage);
                    }).then(() => {
                        return hubConnection.stop();
                    }).then(() => {
                        done();
                    });
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("throws an exception when invoking streaming method with invoke", (done) => {
                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });

                hubConnection.start().then(() => {
                    hubConnection.invoke("EmptyStream").then(() => {
                        // exception expected but none thrown
                        fail();
                    }).catch((e) => {
                        expect(e.message).toBe("The client attempted to invoke the streaming 'EmptyStream' method with a non-streaming invocation.");
                    }).then(() => {
                        return hubConnection.stop();
                    }).then(() => {
                        done();
                    });
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("throws an exception when receiving a streaming result for method called with invoke", (done) => {
                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });

                hubConnection.start().then(() => {
                    hubConnection.invoke("Stream").then(() => {
                        // exception expected but none thrown
                        fail();
                    }).catch((e) => {
                        expect(e.message).toBe("The client attempted to invoke the streaming 'Stream' method with a non-streaming invocation.");
                    }).then(() => {
                        return hubConnection.stop();
                    }).then(() => {
                        done();
                    });
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("rethrows an exception from the server when streaming", (done) => {
                const errorMessage = "An unexpected error occurred invoking 'StreamThrowException' on the server. InvalidOperationException: An error occurred.";
                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });

                hubConnection.start().then(() => {
                    hubConnection.stream("StreamThrowException", "An error occurred.").subscribe({
                        complete: function complete() {
                            hubConnection.stop();
                            fail();
                        },
                        error: function error(err) {
                            expect(err.message).toEqual(errorMessage);
                            hubConnection.stop();
                            done();
                        },
                        next: function next(item) {
                            hubConnection.stop();
                            fail();
                        },
                    });
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("throws an exception when invoking hub method with stream", (done) => {
                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });

                hubConnection.start().then(() => {
                    hubConnection.stream("Echo", "42").subscribe({
                        complete: function complete() {
                            hubConnection.stop();
                            fail();
                        },
                        error: function error(err) {
                            expect(err.message).toEqual("The client attempted to invoke the non-streaming 'Echo' method with a streaming invocation.");
                            hubConnection.stop();
                            done();
                        },
                        next: function next(item) {
                            hubConnection.stop();
                            fail();
                        },
                    });
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("can receive server calls", (done) => {
                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });

                const message = "你好 SignalR！";

                // client side method names are case insensitive
                let methodName = "message";
                const idx = Math.floor(Math.random() * (methodName.length - 1));
                methodName = methodName.substr(0, idx) + methodName[idx].toUpperCase() + methodName.substr(idx + 1);

                hubConnection.on(methodName, (msg) => {
                    expect(msg).toBe(message);
                    done();
                });

                hubConnection.start()
                    .then(() => {
                        return hubConnection.invoke("InvokeWithString", message);
                    })
                    .then(() => {
                        return hubConnection.stop();
                    })
                    .catch((e) => {
                        fail(e);
                        done();
                    });
            });

            it("can receive server calls without rebinding handler when restarted", (done) => {
                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });

                const message = "你好 SignalR！";

                // client side method names are case insensitive
                let methodName = "message";
                const idx = Math.floor(Math.random() * (methodName.length - 1));
                methodName = methodName.substr(0, idx) + methodName[idx].toUpperCase() + methodName.substr(idx + 1);

                let closeCount = 0;
                let invocationCount = 0;

                hubConnection.onclose((e) => {
                    expect(e).toBeUndefined();
                    closeCount += 1;
                    if (closeCount === 1) {
                        // Reconnect
                        hubConnection.start()
                            .then(() => {
                                return hubConnection.invoke("InvokeWithString", message);
                            })
                            .then(() => {
                                return hubConnection.stop();
                            })
                            .catch((error) => {
                                fail(error);
                                done();
                            });
                    } else {
                        expect(invocationCount).toBe(2);
                        done();
                    }
                });

                hubConnection.on(methodName, (msg) => {
                    expect(msg).toBe(message);
                    invocationCount += 1;
                });

                hubConnection.start()
                    .then(() => {
                        return hubConnection.invoke("InvokeWithString", message);
                    })
                    .then(() => {
                        return hubConnection.stop();
                    })
                    .catch((e) => {
                        fail(e);
                        done();
                    });
            });

            it("closed with error if hub cannot be created", (done) => {
                const hubConnection = new HubConnection("http://" + document.location.host + "/uncreatable", {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });

                hubConnection.onclose((error) => {
                    expect(error.message).toEqual("Server returned an error on close: Connection closed with an error. InvalidOperationException: Unable to resolve service for type 'System.Object' while attempting to activate 'FunctionalTests.UncreatableHub'.");
                    done();
                });
                hubConnection.start();
            });

            it("can handle different types", (done) => {
                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });
                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    done();
                });

                const complexObject = {
                    ByteArray: protocol.name === "json"
                        ? "aGVsbG8="
                        : new Uint8Array([0x68, 0x65, 0x6c, 0x6c, 0x6f]),
                    DateTime: protocol.name === "json"
                        ? "2002-04-01T10:20:15Z"
                        : new Date(Date.UTC(2002, 3, 1, 10, 20, 15)), // Apr 1, 2002, 10:20:15am UTC
                    GUID: "00010203-0405-0607-0706-050403020100",
                    IntArray: [0x01, 0x02, 0x03, 0xff],
                    String: "Hello, World!",
                };

                hubConnection.start()
                    .then(() => {
                        return hubConnection.invoke("EchoComplexObject", complexObject);
                    })
                    .then((value) => {
                        if (protocol.name === "messagepack") {
                            // msgpack5 creates a Buffer for byte arrays and jasmine fails to compare a Buffer
                            // and a Uint8Array even though Buffer instances are also Uint8Array instances
                            value.ByteArray = new Uint8Array(value.ByteArray);
                        }
                        expect(value).toEqual(complexObject);
                    })
                    .then(() => {
                        hubConnection.stop();
                    })
                    .catch((e) => {
                        fail(e);
                        done();
                    });
            });

            it("can receive different types", (done) => {
                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });
                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    done();
                });

                const complexObject = {
                    ByteArray: protocol.name === "json"
                        ? "AQID"
                        : new Uint8Array([0x1, 0x2, 0x3]),
                    DateTime: protocol.name === "json"
                        ? "2000-01-01T00:00:00Z"
                        : new Date(Date.UTC(2000, 0, 1)),
                    GUID: "00010203-0405-0607-0706-050403020100",
                    IntArray: [0x01, 0x02, 0x03],
                    String: "hello world",
                };

                hubConnection.start()
                    .then(() => {
                        return hubConnection.invoke("SendComplexObject");
                    })
                    .then((value) => {
                        if (protocol.name === "messagepack") {
                            // msgpack5 creates a Buffer for byte arrays and jasmine fails to compare a Buffer
                            // and a Uint8Array even though Buffer instances are also Uint8Array instances
                            value.ByteArray = new Uint8Array(value.ByteArray);
                        }
                        expect(value).toEqual(complexObject);
                    })
                    .then(() => {
                        hubConnection.stop();
                    })
                    .catch((e) => {
                        fail(e);
                        done();
                    });
            });

            it("can be restarted", (done) => {
                const message = "你好，世界！";

                const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                    ...commonOptions,
                    protocol,
                    transport: transportType,
                });

                let closeCount = 0;
                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);

                    // Start and invoke again
                    if (closeCount === 0) {
                        closeCount += 1;
                        hubConnection.start().then(() => {
                            hubConnection.invoke("Echo", message).then((result) => {
                                expect(result).toBe(message);
                            }).catch((e) => {
                                fail(e);
                            }).then(() => {
                                hubConnection.stop();
                            });
                        }).catch((e) => {
                            fail(e);
                            done();
                        });
                    } else {
                        done();
                    }
                });

                hubConnection.start().then(() => {
                    hubConnection.invoke("Echo", message).then((result) => {
                        expect(result).toBe(message);
                    }).catch((e) => {
                        fail(e);
                    }).then(() => {
                        hubConnection.stop();
                    });
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });
        });
    });

    eachTransport((transportType) => {
        describe("over " + HttpTransportType[transportType] + " transport", () => {

            it("can connect to hub with authorization", async (done) => {
                const message = "你好，世界！";

                try {
                    const jwtToken = await getJwtToken("http://" + document.location.host + "/generateJwtToken");
                    const hubConnection = new HubConnection("/authorizedhub", {
                        accessTokenFactory: () => jwtToken,
                        ...commonOptions,
                        transport: transportType,
                    });
                    hubConnection.onclose((error) => {
                        expect(error).toBe(undefined);
                        done();
                    });
                    await hubConnection.start();
                    const response = await hubConnection.invoke("Echo", message);

                    expect(response).toEqual(message);

                    await hubConnection.stop();

                    done();
                } catch (err) {
                    fail(err);
                    done();
                }
            });

            it("can connect to hub with authorization using async token factory", async (done) => {
                const message = "你好，世界！";

                try {
                    const hubConnection = new HubConnection("/authorizedhub", {
                        accessTokenFactory: () => getJwtToken("http://" + document.location.host + "/generateJwtToken"),
                        ...commonOptions,
                        transport: transportType,
                    });
                    hubConnection.onclose((error) => {
                        expect(error).toBe(undefined);
                        done();
                    });
                    await hubConnection.start();
                    const response = await hubConnection.invoke("Echo", message);

                    expect(response).toEqual(message);

                    await hubConnection.stop();

                    done();
                } catch (err) {
                    fail(err);
                    done();
                }
            });

            it("can connect to hub with authorization using async token factory", async (done) => {
                const message = "你好，世界！";

                try {
                    const hubConnection = new HubConnection("/authorizedhub", {
                        accessTokenFactory: () => getJwtToken("http://" + document.location.host + "/generateJwtToken"),
                        ...commonOptions,
                        transport: transportType,
                    });
                    hubConnection.onclose((error) => {
                        expect(error).toBe(undefined);
                        done();
                    });
                    await hubConnection.start();
                    const response = await hubConnection.invoke("Echo", message);

                    expect(response).toEqual(message);

                    await hubConnection.stop();

                    done();
                } catch (err) {
                    fail(err);
                    done();
                }
            });

            if (transportType !== HttpTransportType.LongPolling) {
                it("terminates if no messages received within timeout interval", (done) => {
                    const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
                        ...commonOptions,
                        timeoutInMilliseconds: 100,
                        transport: transportType,
                    });

                    const timeout = setTimeout(200, () => {
                        fail("Server timeout did not fire within expected interval");
                    });

                    hubConnection.start().then(() => {
                        hubConnection.onclose((error) => {
                            clearTimeout(timeout);
                            expect(error).toEqual(new Error("Server timeout elapsed without receiving a message from the server."));
                            done();
                        });
                    });
                });
            }
        });
    });

    if (typeof EventSource !== "undefined") {
        it("allows Server-Sent Events when negotiating for JSON protocol", async (done) => {
            const hubConnection = new HubConnection(TESTHUB_NOWEBSOCKETS_ENDPOINT_URL, {
                ...commonOptions,
                protocol: new JsonHubProtocol(),
            });

            try {
                await hubConnection.start();

                // Check what transport was used by asking the server to tell us.
                expect(await hubConnection.invoke("GetActiveTransportName")).toEqual("ServerSentEvents");
                done();
            } catch (e) {
                fail(e);
            }
        });
    }

    it("skips Server-Sent Events when negotiating for MessagePack protocol", async (done) => {
        const hubConnection = new HubConnection(TESTHUB_NOWEBSOCKETS_ENDPOINT_URL, {
            ...commonOptions,
            protocol: new MessagePackHubProtocol(),
        });

        try {
            await hubConnection.start();

            // Check what transport was used by asking the server to tell us.
            expect(await hubConnection.invoke("GetActiveTransportName")).toEqual("LongPolling");
            done();
        } catch (e) {
            fail(e);
        }
    });

    it("transport falls back from WebSockets to SSE or LongPolling", async (done) => {
        // Replace Websockets with a function that just
        // throws to force fallback.
        const oldWebSocket = (window as any).WebSocket;
        (window as any).WebSocket = () => {
            throw new Error("Kick rocks");
        };

        const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
            ...commonOptions,
            protocol: new JsonHubProtocol(),
        });

        try {
            await hubConnection.start();

            // Make sure that we connect with SSE or LongPolling after Websockets fail
            const transportName = await hubConnection.invoke("GetActiveTransportName");
            expect(transportName === "ServerSentEvents" || transportName === "LongPolling").toBe(true);
        } catch (e) {
            fail(e);
        } finally {
            (window as any).WebSocket = oldWebSocket;
            done();
        }
    });

    it("over LongPolling it sends DELETE request and waits for poll to terminate", async (done) => {
        // Create an HTTP client to capture the poll
        const defaultClient = new DefaultHttpClient(TestLogger.instance);

        class TestClient extends HttpClient {
            public pollPromise: Promise<HttpResponse>;

            public send(request: HttpRequest): Promise<HttpResponse> {
                if (request.method === "GET") {
                    this.pollPromise = defaultClient.send(request);
                    return this.pollPromise;
                }
                return defaultClient.send(request);
            }
        }

        const testClient = new TestClient();
        const hubConnection = new HubConnection(TESTHUBENDPOINT_URL, {
            ...commonOptions,
            httpClient: testClient,
        });
        try {
            await hubConnection.start();

            expect(testClient.pollPromise).not.toBeNull();

            // Stop the connection and await the poll terminating
            const stopPromise = hubConnection.stop();

            await testClient.pollPromise;
            await stopPromise;
        } catch (e) {
            fail(e);
        } finally {
            done();
        }
    });

    function getJwtToken(url): Promise<string> {
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();

            xhr.open("GET", url, true);
            xhr.setRequestHeader("X-Requested-With", "XMLHttpRequest");
            xhr.send();
            xhr.onload = () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    resolve(xhr.response || xhr.responseText);
                } else {
                    reject(new Error(xhr.statusText));
                }
            };

            xhr.onerror = () => {
                reject(new Error(xhr.statusText));
            };
        });
    }
});
