// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This code uses a lot of `.then` instead of `await` and TSLint doesn't like it.
// tslint:disable:no-floating-promises

import { AbortError, DefaultHttpClient, HttpClient, HttpRequest, HttpResponse, HttpTransportType, HubConnectionBuilder, IHttpConnectionOptions, JsonHubProtocol, NullLogger } from "@microsoft/signalr";
import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";
import { getUserAgentHeader, Platform } from "@microsoft/signalr/dist/esm/Utils";

import { eachTransport, eachTransportAndProtocolAndHttpClient, ENDPOINT_BASE_HTTPS_URL, ENDPOINT_BASE_URL } from "./Common";
import "./LogBannerReporter";
import { TestLogger } from "./TestLogger";

import { PromiseSource } from "../../signalr/tests/Utils";

import * as RX from "rxjs";

const TESTHUBENDPOINT_URL = ENDPOINT_BASE_URL + "/testhub";
const TESTHUBENDPOINT_HTTPS_URL = ENDPOINT_BASE_HTTPS_URL ? (ENDPOINT_BASE_HTTPS_URL + "/testhub") : undefined;

const TESTHUB_NOWEBSOCKETS_ENDPOINT_URL = ENDPOINT_BASE_URL + "/testhub-nowebsockets";
const TESTHUB_REDIRECT_ENDPOINT_URL = ENDPOINT_BASE_URL + "/redirect?numRedirects=0&baseUrl=" + ENDPOINT_BASE_URL;

const commonOptions: IHttpConnectionOptions = {
    logMessageContent: true,
};

// Run test in Node or Chrome, but not on macOS
const shouldRunHttpsTests =
    // Need to have an HTTPS URL
    !!TESTHUBENDPOINT_HTTPS_URL &&

    // Run on Node, unless macOS
    (process && process.platform !== "darwin") &&

    // Only run under Chrome browser
    (typeof navigator === "undefined" || navigator.userAgent.search("Chrome") !== -1);

function getConnectionBuilder(transportType?: HttpTransportType, url?: string, options?: IHttpConnectionOptions): HubConnectionBuilder {
    let actualOptions: IHttpConnectionOptions = options || {};
    if (transportType) {
        actualOptions.transport = transportType;
    }
    actualOptions = { ...actualOptions, ...commonOptions };

    return new HubConnectionBuilder()
        .configureLogging(TestLogger.instance)
        .withUrl(url || TESTHUBENDPOINT_URL, actualOptions);
}

describe("hubConnection", () => {
    eachTransportAndProtocolAndHttpClient((transportType, protocol, httpClient) => {
        describe("using " + protocol.name + " over " + HttpTransportType[transportType] + " transport", () => {
            it("can invoke server method and receive result", (done) => {
                const message = "你好，世界！";

                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

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

            if (shouldRunHttpsTests) {
                // xit will skip the test
                xit("using https, can invoke server method and receive result", (done) => {
                    const message = "你好，世界！";

                    const hubConnection = getConnectionBuilder(transportType, TESTHUBENDPOINT_HTTPS_URL, { httpClient })
                        .withHubProtocol(protocol)
                        .build();

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
            }

            it("can invoke server method non-blocking and not receive result", (done) => {
                const message = "你好，世界！";

                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

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
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

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
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    done();
                });

                const received: string[] = [];
                hubConnection.start().then(() => {
                    hubConnection.stream<string>("Stream").subscribe({
                        complete() {
                            expect(received).toEqual(["a", "b", "c"]);
                            hubConnection.stop();
                        },
                        error(err) {
                            fail(err);
                            hubConnection.stop();
                        },
                        next(item) {
                            received.push(item);
                        },
                    });
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("can stream server method and cancel stream", (done) => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    done();
                });

                hubConnection.on("StreamCanceled", () => {
                    hubConnection.stop();
                });

                hubConnection.start().then(() => {
                    const subscription = hubConnection.stream<string>("InfiniteStream").subscribe({
                        complete() {
                        },
                        error(err) {
                            fail(err);
                            hubConnection.stop();
                        },
                        next() {
                        },
                    });

                    subscription.dispose();
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("rethrows an exception from the server when invoking", (done) => {
                const errorMessage = "An unexpected error occurred invoking 'ThrowException' on the server. InvalidOperationException: An error occurred.";
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

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
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

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
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

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
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                hubConnection.start().then(() => {
                    hubConnection.stream("StreamThrowException", "An error occurred.").subscribe({
                        complete() {
                            hubConnection.stop();
                            fail();
                        },
                        error(err) {
                            expect(err.message).toEqual(errorMessage);
                            hubConnection.stop();
                            done();
                        },
                        next() {
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
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                hubConnection.start().then(() => {
                    hubConnection.stream("Echo", "42").subscribe({
                        complete() {
                            hubConnection.stop();
                            fail();
                        },
                        error(err) {
                            expect(err.message).toEqual("The client attempted to invoke the non-streaming 'Echo' method with a streaming invocation.");
                            hubConnection.stop();
                            done();
                        },
                        next() {
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
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

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
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

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

            it("closed with error or start fails if hub cannot be created", async (done) => {
                const hubConnection = getConnectionBuilder(transportType, ENDPOINT_BASE_URL + "/uncreatable", { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                const expectedErrorMessage = "Server returned an error on close: Connection closed with an error. InvalidOperationException: Unable to resolve service for type 'System.Object' while attempting to activate 'FunctionalTests.UncreatableHub'.";

                // Either start will fail or onclose will be called. Never both.
                hubConnection.onclose((error) => {
                    expect(error!.message).toEqual(expectedErrorMessage);
                    done();
                });

                try {
                    await hubConnection.start();
                } catch (error) {
                    expect(error!.message).toEqual(expectedErrorMessage);
                    done();
                }
            });

            it("can handle different types", (done) => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

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
                    Guid: "00010203-0405-0607-0706-050403020100",
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
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

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
                    Guid: "00010203-0405-0607-0706-050403020100",
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

                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

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

            it("can stream from client to server with rxjs", async (done) => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                await hubConnection.start();
                const subject = new RX.Subject<string>();
                const resultPromise = hubConnection.invoke<string>("StreamingConcat", subject.asObservable());
                subject.next("Hello ");
                subject.next("world");
                subject.next("!");
                subject.complete();
                expect(await resultPromise).toBe("Hello world!");
                await hubConnection.stop();
                done();
            });

            it("can stream from client to server and close with error with rxjs", async (done) => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                await hubConnection.start();
                const subject = new RX.Subject<string>();
                const resultPromise = hubConnection.invoke<string>("StreamingConcat", subject.asObservable());
                subject.next("Hello ");
                subject.next("world");
                subject.next("!");
                subject.error(new Error("Something bad"));
                try {
                    await resultPromise;
                    expect(false).toBe(true);
                } catch (err) {
                    expect(err.message).toEqual("An unexpected error occurred invoking 'StreamingConcat' on the server. Exception: Something bad");
                } finally {
                    await hubConnection.stop();
                }
                done();
            });
        });
    });

    eachTransport((transportType) => {
        describe("over " + HttpTransportType[transportType] + " transport", () => {

            it("can connect to hub with authorization", async (done) => {
                const message = "你好，世界！";

                try {
                    const jwtToken = await getJwtToken(ENDPOINT_BASE_URL + "/generateJwtToken");

                    const hubConnection = getConnectionBuilder(transportType, ENDPOINT_BASE_URL + "/authorizedhub", {
                        accessTokenFactory: () => jwtToken,
                    }).build();

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
                    const hubConnection = getConnectionBuilder(transportType, ENDPOINT_BASE_URL + "/authorizedhub", {
                        accessTokenFactory: () => getJwtToken(ENDPOINT_BASE_URL + "/generateJwtToken"),
                    }).build();

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
                it("terminates if no messages received within timeout interval", async (done) => {
                    const hubConnection = getConnectionBuilder(transportType).build();

                    hubConnection.onclose((error) => {
                        expect(error).toEqual(new Error("Server timeout elapsed without receiving a message from the server."));
                        done();
                    });

                    await hubConnection.start();

                    // set this after start completes to avoid network issues with the handshake taking over 100ms and causing a failure
                    hubConnection.serverTimeoutInMilliseconds = 1;

                    // invoke a method with a response to reset the timeout using the new value
                    await hubConnection.invoke("Echo", "");
                });
            }

            it("preserves cookies between requests", async (done) => {
                const hubConnection = getConnectionBuilder(transportType).build();
                await hubConnection.start();
                const cookieValue = await hubConnection.invoke<string>("GetCookie", "testCookie");
                const cookieValue2 = await hubConnection.invoke<string>("GetCookie", "testCookie2");
                expect(cookieValue).toEqual("testValue");
                expect(cookieValue2).toEqual("testValue2");
                await hubConnection.stop();
                done();
            });

            it("expired cookies are not preserved", async (done) => {
                const hubConnection = getConnectionBuilder(transportType).build();
                await hubConnection.start();
                const cookieValue = await hubConnection.invoke<string>("GetCookie", "expiredCookie");
                expect(cookieValue).toBeNull();
                await hubConnection.stop();
                done();
            });

            it("can reconnect", async (done) => {
                try {
                    const reconnectingPromise = new PromiseSource();
                    const reconnectedPromise = new PromiseSource<string | undefined>();
                    const hubConnection = getConnectionBuilder(transportType)
                        .withAutomaticReconnect()
                        .build();

                    hubConnection.onreconnecting(() => {
                        reconnectingPromise.resolve();
                    });

                    hubConnection.onreconnected((connectionId?) => {
                        reconnectedPromise.resolve(connectionId);
                    });

                    await hubConnection.start();

                    const initialConnectionId = (hubConnection as any).connection.connectionId as string;

                    // Induce reconnect
                    (hubConnection as any).serverTimeout();

                    await reconnectingPromise;
                    const newConnectionId = await reconnectedPromise;

                    expect(newConnectionId).not.toBe(initialConnectionId);

                    const response = await hubConnection.invoke("Echo", "test");

                    expect(response).toEqual("test");

                    await hubConnection.stop();

                    done();
                } catch (err) {
                    fail(err);
                    done();
                }
            });
        });

        it("can change url in reconnecting state", async (done) => {
            try {
                const reconnectingPromise = new PromiseSource();
                const hubConnection = getConnectionBuilder(transportType)
                    .withAutomaticReconnect()
                    .build();

                hubConnection.onreconnecting(() => {
                    hubConnection.baseUrl = "http://example123.com";
                    reconnectingPromise.resolve();
                });

                await hubConnection.start();

                // Induce reconnect
                (hubConnection as any).serverTimeout();

                await reconnectingPromise;

                expect(hubConnection.baseUrl).toBe("http://example123.com");

                await hubConnection.stop();

                done();
            } catch (err) {
                fail(err);
                done();
            }
        });
    });

    it("can reconnect after negotiate redirect", async (done) => {
        try {
            const reconnectingPromise = new PromiseSource();
            const reconnectedPromise = new PromiseSource<string | undefined>();
            const hubConnection = getConnectionBuilder(undefined, TESTHUB_REDIRECT_ENDPOINT_URL)
                .withAutomaticReconnect()
                .build();

            hubConnection.onreconnecting(() => {
                reconnectingPromise.resolve();
            });

            hubConnection.onreconnected((connectionId?) => {
                reconnectedPromise.resolve(connectionId);
            });

            await hubConnection.start();

            const preReconnectRedirects = await hubConnection.invoke<number>("GetNumRedirects");

            const initialConnectionId = (hubConnection as any).connection.connectionId as string;

            // Induce reconnect
            (hubConnection as any).serverTimeout();

            await reconnectingPromise;
            const newConnectionId = await reconnectedPromise;

            expect(newConnectionId).not.toBe(initialConnectionId);

            const serverConnectionId = await hubConnection.invoke<string>("GetCallerConnectionId");
            expect(newConnectionId).toBe(serverConnectionId);

            const postReconnectRedirects = await hubConnection.invoke<number>("GetNumRedirects");

            expect(postReconnectRedirects).toBeGreaterThan(preReconnectRedirects);

            await hubConnection.stop();

            done();
        } catch (err) {
            fail(err);
            done();
        }
    });

    it("can reconnect after skipping negotiation", async (done) => {
        try {
            const reconnectingPromise = new PromiseSource();
            const reconnectedPromise = new PromiseSource<string | undefined>();
            const hubConnection = getConnectionBuilder(
                    HttpTransportType.WebSockets,
                    undefined,
                    { skipNegotiation: true },
                )
                .withAutomaticReconnect()
                .build();

            hubConnection.onreconnecting(() => {
                reconnectingPromise.resolve();
            });

            hubConnection.onreconnected((connectionId?) => {
                reconnectedPromise.resolve(connectionId);
            });

            await hubConnection.start();

            // Induce reconnect
            (hubConnection as any).serverTimeout();

            await reconnectingPromise;
            const newConnectionId = await reconnectedPromise;

            expect(newConnectionId).toBeUndefined();

            const response = await hubConnection.invoke("Echo", "test");

            expect(response).toEqual("test");

            await hubConnection.stop();

            done();
        } catch (err) {
            fail(err);
            done();
        }
    });

    it("connection id matches server side connection id", async (done) => {
        try {
            const reconnectingPromise = new PromiseSource();
            const reconnectedPromise = new PromiseSource<string | undefined>();
            const hubConnection = getConnectionBuilder(undefined, TESTHUB_REDIRECT_ENDPOINT_URL)
                .withAutomaticReconnect()
                .build();

            hubConnection.onreconnecting(() => {
                reconnectingPromise.resolve();
            });

            hubConnection.onreconnected((connectionId?) => {
                reconnectedPromise.resolve(connectionId);
            });

            expect(hubConnection.connectionId).toBeNull();

            await hubConnection.start();

            expect(hubConnection.connectionId).not.toBeNull();
            let serverConnectionId = await hubConnection.invoke<string>("GetCallerConnectionId");
            expect(hubConnection.connectionId).toBe(serverConnectionId);

            const initialConnectionId = hubConnection.connectionId!;

            // Induce reconnect
            (hubConnection as any).serverTimeout();

            await reconnectingPromise;
            const newConnectionId = await reconnectedPromise;

            expect(newConnectionId).not.toBe(initialConnectionId);

            serverConnectionId = await hubConnection.invoke<string>("GetCallerConnectionId");
            expect(newConnectionId).toBe(serverConnectionId);

            await hubConnection.stop();
            expect(hubConnection.connectionId).toBeNull();

            done();
        } catch (err) {
            fail(err);
            done();
        }
    });

    it("connection id is alwys null is negotiation is skipped", async (done) => {
        try {
            const hubConnection = getConnectionBuilder(
                    HttpTransportType.WebSockets,
                    undefined,
                    { skipNegotiation: true },
                )
                .build();

            expect(hubConnection.connectionId).toBeNull();

            await hubConnection.start();

            expect(hubConnection.connectionId).toBeNull();

            await hubConnection.stop();

            expect(hubConnection.connectionId).toBeNull();

            done();
        } catch (err) {
            fail(err);
            done();
        }
    });

    if (typeof EventSource !== "undefined") {
        it("allows Server-Sent Events when negotiating for JSON protocol", async (done) => {
            const hubConnection = getConnectionBuilder(undefined, TESTHUB_NOWEBSOCKETS_ENDPOINT_URL)
                .withHubProtocol(new JsonHubProtocol())
                .build();

            try {
                await hubConnection.start();

                // Check what transport was used by asking the server to tell us.
                expect(await hubConnection.invoke("GetActiveTransportName")).toEqual("ServerSentEvents");

                await hubConnection.stop();
                done();
            } catch (e) {
                fail(e);
            }
        });
    }

    it("skips Server-Sent Events when negotiating for MessagePack protocol", async (done) => {
        const hubConnection = getConnectionBuilder(undefined, TESTHUB_NOWEBSOCKETS_ENDPOINT_URL)
            .withHubProtocol(new MessagePackHubProtocol())
            .build();

        try {
            await hubConnection.start();

            // Check what transport was used by asking the server to tell us.
            expect(await hubConnection.invoke("GetActiveTransportName")).toEqual("LongPolling");

            await hubConnection.stop();
            done();
        } catch (e) {
            fail(e);
        }
    });

    it("transport falls back from WebSockets to SSE or LongPolling", async (done) => {
        // Skip test on Node as there will always be a WebSockets implementation on Node
        if (typeof window === "undefined") {
            done();
            return;
        }

        // Replace Websockets with a function that just
        // throws to force fallback.
        const oldWebSocket = (window as any).WebSocket;
        (window as any).WebSocket = () => {
            throw new Error("Kick rocks");
        };

        const hubConnection = getConnectionBuilder()
            .withHubProtocol(new JsonHubProtocol())
            .build();

        try {
            await hubConnection.start();

            // Make sure that we connect with SSE or LongPolling after Websockets fail
            const transportName = await hubConnection.invoke("GetActiveTransportName");
            expect(transportName === "ServerSentEvents" || transportName === "LongPolling").toBe(true);
            await hubConnection.stop();
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
            public pollPromise: Promise<HttpResponse> | null;

            constructor() {
                super();
                this.pollPromise = null;
            }

            public send(request: HttpRequest): Promise<HttpResponse> {
                if (request.method === "GET") {
                    this.pollPromise = defaultClient.send(request);
                    return this.pollPromise;
                }
                return defaultClient.send(request);
            }
        }

        const testClient = new TestClient();
        const hubConnection = getConnectionBuilder(HttpTransportType.LongPolling, TESTHUBENDPOINT_URL, {
            httpClient: testClient,
        }).build();

        try {
            await hubConnection.start();

            expect(testClient.pollPromise).not.toBeNull();

            // Stop the connection and await the poll terminating
            const stopPromise = hubConnection.stop();

            try {
                await testClient.pollPromise;
            } catch (e) {
                if (e instanceof AbortError) {
                    // Poll request may have been aborted
                } else {
                    throw e;
                }
            }

            await stopPromise;
        } catch (e) {
            fail(e);
        } finally {
            done();
        }
    });

    it("populates the Content-Type header when sending XMLHttpRequest", async (done) => {
        // Skip test on Node as this header isn't set (it was added for React-Native)
        if (typeof window === "undefined") {
            done();
            return;
        }
        const hubConnection = getConnectionBuilder(HttpTransportType.LongPolling, TESTHUB_NOWEBSOCKETS_ENDPOINT_URL)
            .withHubProtocol(new JsonHubProtocol())
            .build();

        try {
            await hubConnection.start();

            // Check what transport was used by asking the server to tell us.
            expect(await hubConnection.invoke("GetActiveTransportName")).toEqual("LongPolling");
            // Check to see that the Content-Type header is set the expected value
            expect(await hubConnection.invoke("GetContentTypeHeader")).toEqual("text/plain;charset=UTF-8");

            await hubConnection.stop();
            done();
        } catch (e) {
            fail(e);
        }
    });

    eachTransport((t) => {
        it("sets the user agent header", async (done) => {
            const hubConnection = getConnectionBuilder(t, TESTHUBENDPOINT_URL)
                .withHubProtocol(new JsonHubProtocol())
                .build();

            try {
                await hubConnection.start();

                // Check to see that the Content-Type header is set the expected value
                const [name, value] = getUserAgentHeader();
                const headerValue = await hubConnection.invoke("GetHeader", name);

                if ((t === HttpTransportType.ServerSentEvents || t === HttpTransportType.WebSockets) && !Platform.isNode) {
                    expect(headerValue).toBeNull();
                } else {
                    expect(headerValue).toEqual(value);
                }

                await hubConnection.stop();
                done();
            } catch (e) {
                fail(e);
            }
        });
    });

    function getJwtToken(url: string): Promise<string> {
        return new Promise((resolve, reject) => {
            const httpClient = new DefaultHttpClient(NullLogger.instance);
            httpClient.get(url).then((response) => {
                if (response.statusCode >= 200 && response.statusCode < 300) {
                    resolve(response.content as string);
                } else {
                    reject(new Error(response.statusText));
                }
            });
        });
    }
});
