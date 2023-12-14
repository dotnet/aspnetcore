// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AbortError, DefaultHttpClient, HttpClient, HttpRequest, HttpResponse, HttpTransportType, HubConnectionBuilder, IHttpConnectionOptions, JsonHubProtocol, NullLogger } from "@microsoft/signalr";
import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";
import { getUserAgentHeader, Platform } from "@microsoft/signalr/dist/esm/Utils";

import { DEFAULT_TIMEOUT_INTERVAL, eachTransport, eachTransportAndProtocolAndHttpClient, ENDPOINT_BASE_HTTPS_URL, ENDPOINT_BASE_URL, shouldRunHttpsTests } from "./Common";
import "./LogBannerReporter";
import { TestLogger } from "./TestLogger";

import * as RX from "rxjs";
import { PromiseSource } from "./Utils";

const TESTHUBENDPOINT_URL = ENDPOINT_BASE_URL + "/testhub";
const TESTHUBENDPOINT_HTTPS_URL = ENDPOINT_BASE_HTTPS_URL ? (ENDPOINT_BASE_HTTPS_URL + "/testhub") : undefined;
const HTTPORHTTPS_TESTHUBENDPOINT_URL = shouldRunHttpsTests ? TESTHUBENDPOINT_HTTPS_URL : TESTHUBENDPOINT_URL;

const TESTHUB_NOWEBSOCKETS_ENDPOINT_URL = ENDPOINT_BASE_URL + "/testhub-nowebsockets";
const TESTHUB_REDIRECT_ENDPOINT_URL = ENDPOINT_BASE_URL + "/redirect?numRedirects=0&baseUrl=" + ENDPOINT_BASE_URL;

jasmine.DEFAULT_TIMEOUT_INTERVAL = DEFAULT_TIMEOUT_INTERVAL;

const commonOptions: IHttpConnectionOptions = {
    logMessageContent: true,
};

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
            it("can invoke server method and receive result", async () => {
                const message = "你好，世界！";

                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                const closePromise = new PromiseSource();
                hubConnection.onclose((error) => {
                    expect(error).toBeUndefined();
                    closePromise.resolve();
                });

                await hubConnection.start();
                const result = await hubConnection.invoke("Echo", message);
                expect(result).toBe(message);

                await hubConnection.stop();
                await closePromise;
            });

            if (shouldRunHttpsTests) {
                it("using https, can invoke server method and receive result", async () => {
                    const message = "你好，世界！";

                    const hubConnection = getConnectionBuilder(transportType, TESTHUBENDPOINT_HTTPS_URL, { httpClient })
                        .withHubProtocol(protocol)
                        .build();

                    const closePromise = new PromiseSource();
                    hubConnection.onclose((error) => {
                        expect(error).toBeUndefined();
                        closePromise.resolve();
                    });

                    await hubConnection.start();
                    const result = await hubConnection.invoke("Echo", message);
                    expect(result).toBe(message);

                    await hubConnection.stop();
                    await closePromise;
                });
            }

            it("can invoke server method non-blocking and not receive result", async () => {
                const message = "你好，世界！";

                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                const closePromise = new PromiseSource();
                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    closePromise.resolve();
                });

                await hubConnection.start();
                await hubConnection.send("Echo", message);

                await hubConnection.stop();
                await closePromise;
            });

            it("can invoke server method structural object and receive structural result", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                hubConnection.on("CustomObject", async (customObject) => {
                    expect(customObject.Name).toBe("test");
                    expect(customObject.Value).toBe(42);
                    await hubConnection.stop();
                });

                const closePromise = new PromiseSource();
                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    closePromise.resolve();
                });

                await hubConnection.start();
                await hubConnection.send("SendCustomObject", { Name: "test", Value: 42 });
                await closePromise;
            });

            it("can stream server method and receive result", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                const closePromise = new PromiseSource();
                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    closePromise.resolve();
                });

                const received: string[] = [];
                await hubConnection.start();
                hubConnection.stream<string>("Stream").subscribe({
                    async complete() {
                        expect(received).toEqual(["a", "b", "c"]);
                        await hubConnection.stop();
                    },
                    async error(err) {
                        fail(err);
                        await hubConnection.stop();
                    },
                    next(item) {
                        received.push(item);
                    },
                });
                await closePromise;
            });

            it("can stream server method and cancel stream", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                const closePromise = new PromiseSource();
                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    closePromise.resolve();
                });

                hubConnection.on("StreamCanceled", async () => {
                    await hubConnection.stop();
                });

                await hubConnection.start();
                const subscription = hubConnection.stream<string>("InfiniteStream").subscribe({
                    complete() {
                    },
                    async error(err) {
                        fail(err);
                        await hubConnection.stop();
                    },
                    next() {
                    },
                });

                subscription.dispose();
                await closePromise;
            });

            it("rethrows an exception from the server when invoking", async () => {
                const errorMessage = "An unexpected error occurred invoking 'ThrowException' on the server. InvalidOperationException: An error occurred.";
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                await hubConnection.start();
                try {
                    await hubConnection.invoke("ThrowException", "An error occurred.");
                    // exception expected but none thrown
                    fail();
                } catch (e) {
                    expect((e as any).message).toBe(errorMessage);
                }

                await hubConnection.stop();
            });

            it("throws an exception when invoking streaming method with invoke", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                await hubConnection.start();

                try {
                    await hubConnection.invoke("EmptyStream");
                    // exception expected but none thrown
                    fail();
                } catch (e) {
                    expect((e as any).message).toBe("The client attempted to invoke the streaming 'EmptyStream' method with a non-streaming invocation.");
                }

                await hubConnection.stop();
            });

            it("throws an exception when receiving a streaming result for method called with invoke", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                await hubConnection.start();

                try {
                    await hubConnection.invoke("Stream");
                    // exception expected but none thrown
                    fail();
                } catch (e) {
                    expect((e as any).message).toBe("The client attempted to invoke the streaming 'Stream' method with a non-streaming invocation.");
                }

                await hubConnection.stop();
            });

            it("rethrows an exception from the server when streaming", async () => {
                const errorMessage = "An unexpected error occurred invoking 'StreamThrowException' on the server. InvalidOperationException: An error occurred.";
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                const closePromise = new PromiseSource();
                await hubConnection.start();
                hubConnection.stream("StreamThrowException", "An error occurred.").subscribe({
                    async complete() {
                        await hubConnection.stop();
                        fail();
                    },
                    async error(err) {
                        expect(err.message).toEqual(errorMessage);
                        await hubConnection.stop();
                        closePromise.resolve();
                    },
                    async next() {
                        await hubConnection.stop();
                        fail();
                    },
                });
                await closePromise;
            });

            it("throws an exception when invoking hub method with stream", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                const closePromise = new PromiseSource();
                await hubConnection.start();
                hubConnection.stream("Echo", "42").subscribe({
                    async complete() {
                        await hubConnection.stop();
                        fail();
                    },
                    async error(err) {
                        expect(err.message).toEqual("The client attempted to invoke the non-streaming 'Echo' method with a streaming invocation.");
                        await hubConnection.stop();
                        closePromise.resolve();
                    },
                    async next() {
                        await hubConnection.stop();
                        fail();
                    },
                });

                await closePromise;
            });

            it("can receive server calls", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                const message = "你好 SignalR！";

                // client side method names are case insensitive
                let methodName = "message";
                const idx = Math.floor(Math.random() * (methodName.length - 1));
                methodName = methodName.substr(0, idx) + methodName[idx].toUpperCase() + methodName.substr(idx + 1);

                const receivePromise = new PromiseSource<string>();
                hubConnection.on(methodName, (msg) => {
                    receivePromise.resolve(msg);
                });

                await hubConnection.start();
                await hubConnection.invoke("InvokeWithString", message);

                const receiveMsg = await receivePromise;
                expect(receiveMsg).toBe(message);

                await hubConnection.stop();
            });

            it("can receive server calls without rebinding handler when restarted", async () => {
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

                const closePromise = new PromiseSource();
                hubConnection.onclose(async (e) => {
                    expect(e).toBeUndefined();
                    closeCount += 1;
                    if (closeCount === 1) {
                        // Reconnect
                        await hubConnection.start();
                        await hubConnection.invoke("InvokeWithString", message);
                        await hubConnection.stop();
                    } else {
                        expect(invocationCount).toBe(2);
                        closePromise.resolve();
                    }
                });

                hubConnection.on(methodName, (msg) => {
                    expect(msg).toBe(message);
                    invocationCount += 1;
                });

                await hubConnection.start();
                await hubConnection.invoke("InvokeWithString", message);
                await hubConnection.stop();
                await closePromise;
            });

            // Skipped: https://github.com/dotnet/aspnetcore/issues/44608
            xit("closed with error or start fails if hub cannot be created", async () => {
                const hubConnection = getConnectionBuilder(transportType, ENDPOINT_BASE_URL + "/uncreatable", { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                const expectedErrorMessage = "Server returned an error on close: Connection closed with an error. InvalidOperationException: Unable to resolve service for type 'System.Object' while attempting to activate 'FunctionalTests.UncreatableHub'.";

                const closePromise = new PromiseSource();
                // Either start will fail or onclose will be called. Never both.
                hubConnection.onclose((error) => {
                    expect(error!.message).toEqual(expectedErrorMessage);
                    closePromise.resolve();
                });

                try {
                    await hubConnection.start();
                } catch (error) {
                    if ((error as any)!.message.includes("404")) {
                        // SSE can race with the connection closing and the initial ping being successful or failing with a 404.
                        // LongPolling doesn't have pings and WebSockets is a synchronous API over a single HTTP request so it doesn't have the same issues
                        expect((error as any)!.message).toEqual("No Connection with that ID: Status code '404'");
                    } else {
                        expect((error as any)!.message).toEqual(expectedErrorMessage);
                    }
                    closePromise.resolve();
                }
                await closePromise;
            });

            it("can handle different types", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                const closePromise = new PromiseSource();
                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    closePromise.resolve();
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

                await hubConnection.start();
                const value = await hubConnection.invoke("EchoComplexObject", complexObject);
                expect(value).toEqual(complexObject);

                await hubConnection.stop();
                await closePromise;
            });

            it("can receive different types", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                const closePromise = new PromiseSource();
                hubConnection.onclose((error) => {
                    expect(error).toBe(undefined);
                    closePromise.resolve();
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

                await hubConnection.start();
                const value = await hubConnection.invoke("SendComplexObject");
                expect(value).toEqual(complexObject);

                await hubConnection.stop();
                await closePromise;
            });

            it("can be restarted", async () => {
                const message = "你好，世界！";

                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                let closeCount = 0;
                const closePromise = new PromiseSource();
                hubConnection.onclose(async (error) => {
                    expect(error).toBe(undefined);

                    // Start and invoke again
                    if (closeCount === 0) {
                        closeCount += 1;
                        await hubConnection.start();
                        const value = await hubConnection.invoke("Echo", message);
                        expect(value).toBe(message);
                        await hubConnection.stop();
                    } else {
                        closePromise.resolve();
                    }
                });

                await hubConnection.start();
                const result = await hubConnection.invoke("Echo", message);
                expect(result).toBe(message);

                await hubConnection.stop();
                await closePromise;
            });

            it("can stream from client to server with rxjs", async () => {
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
            });

            it("can stream from client to server and close with error with rxjs", async () => {
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
                    expect((err as any).message).toEqual("An unexpected error occurred invoking 'StreamingConcat' on the server. HubException: Something bad");
                } finally {
                    await hubConnection.stop();
                }
            });

            it("can return result to server", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                hubConnection.on("Result", () => {
                    return 10;
                });

                await hubConnection.start();

                const response = await httpClient.get(ENDPOINT_BASE_URL + `/clientresult/${hubConnection.connectionId}`);

                expect(response.content).toEqual("10");

                await hubConnection.stop();
            });

            it("can throw result to server", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                hubConnection.on("Result", () => {
                    throw new Error("from callback");
                });

                try {
                    await hubConnection.start();

                    const response = await httpClient.get(ENDPOINT_BASE_URL + `/clientresult/${hubConnection.connectionId}`);

                    expect(response.content).toEqual("Error: from callback");
                } finally {
                    await hubConnection.stop();
                }
            });

            it("returns result error to server when no result given", async () => {
                const hubConnection = getConnectionBuilder(transportType, undefined, { httpClient })
                    .withHubProtocol(protocol)
                    .build();

                try {
                    await hubConnection.start();

                    const response = await httpClient.get(ENDPOINT_BASE_URL + `/clientresult/${hubConnection.connectionId}`);

                    expect(response.content).toEqual("Client didn't provide a result.");
                } finally {
                    await hubConnection.stop();
                }
            });
        });
    });

    eachTransport((transportType) => {
        describe("over " + HttpTransportType[transportType] + " transport", () => {

            it("can connect to hub with authorization", async () => {
                const message = "你好，世界！";

                try {
                    const jwtToken = await getJwtToken(ENDPOINT_BASE_URL + "/generateJwtToken");

                    const hubConnection = getConnectionBuilder(transportType, ENDPOINT_BASE_URL + "/authorizedhub", {
                        accessTokenFactory: () => jwtToken,
                    }).build();

                    const closePromise = new PromiseSource();
                    hubConnection.onclose((error) => {
                        expect(error).toBe(undefined);
                        closePromise.resolve();
                    });
                    await hubConnection.start();
                    const response = await hubConnection.invoke("Echo", message);

                    expect(response).toEqual(message);

                    await hubConnection.stop();

                    await closePromise;
                } catch (err) {
                    fail(err);
                }
            });

            it("can connect to hub with authorization using async token factory", async () => {
                const message = "你好，世界！";

                try {
                    const hubConnection = getConnectionBuilder(transportType, ENDPOINT_BASE_URL + "/authorizedhub", {
                        accessTokenFactory: () => getJwtToken(ENDPOINT_BASE_URL + "/generateJwtToken"),
                    }).build();

                    const closePromise = new PromiseSource();
                    hubConnection.onclose((error) => {
                        expect(error).toBe(undefined);
                        closePromise.resolve();
                    });
                    await hubConnection.start();
                    const response = await hubConnection.invoke("Echo", message);

                    expect(response).toEqual(message);

                    await hubConnection.stop();

                    await closePromise;
                } catch (err) {
                    fail(err);
                }
            });

            it("can get error from unauthorized hub connection", async () => {
                try {
                    const hubConnection = getConnectionBuilder(transportType, ENDPOINT_BASE_URL + "/authorizedhub").build();

                    hubConnection.onclose((error) => {
                        expect(error).toBe(undefined);
                    });

                    await hubConnection.start();

                    fail("shouldn't reach here");
                } catch (err) {
                    expect(err).toEqual(new Error("Failed to complete negotiation with the server: Error: Unauthorized: Status code '401'"));
                }
            });

            if (transportType !== HttpTransportType.LongPolling) {
                it("terminates if no messages received within timeout interval", async () => {
                    const hubConnection = getConnectionBuilder(transportType).build();

                    const closePromise = new PromiseSource();
                    hubConnection.onclose((error) => {
                        expect(error).toEqual(new Error("Server timeout elapsed without receiving a message from the server."));
                        closePromise.resolve();
                    });

                    await hubConnection.start();

                    // set this after start completes to avoid network issues with the handshake taking over 100ms and causing a failure
                    hubConnection.serverTimeoutInMilliseconds = 1;

                    // invoke a method with a response to reset the timeout using the new value
                    await hubConnection.invoke("Echo", "");
                    await closePromise;
                });
            }

            if (shouldRunHttpsTests) {
                it("preserves cookies between requests", async () => {
                    const hubConnection = getConnectionBuilder(transportType, HTTPORHTTPS_TESTHUBENDPOINT_URL).build();
                    await hubConnection.start();
                    const cookieValue = await hubConnection.invoke<string>("GetCookie", "testCookie");
                    const cookieValue2 = await hubConnection.invoke<string>("GetCookie", "testCookie2");
                    expect(cookieValue).toEqual("testValue");
                    expect(cookieValue2).toEqual("testValue2");
                    await hubConnection.stop();
                });
            }

            it("expired cookies are not preserved", async () => {
                const hubConnection = getConnectionBuilder(transportType, HTTPORHTTPS_TESTHUBENDPOINT_URL).build();
                await hubConnection.start();
                const cookieValue = await hubConnection.invoke<string>("GetCookie", "expiredCookie");
                expect(cookieValue).toBeNull();
                await hubConnection.stop();
            });

            it("can reconnect", async () => {
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
                } catch (err) {
                    fail(err);
                }
            });
        });

        it("can change url in reconnecting state", async () => {
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
            } catch (err) {
                fail(err);
            }
        });
    });

    it("can reconnect after negotiate redirect", async () => {
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
        } catch (err) {
            fail(err);
        }
    });

    it("can reconnect after skipping negotiation", async () => {
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
        } catch (err) {
            fail(err);
        }
    });

    it("connection id matches server side connection id", async () => {
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
        } catch (err) {
            fail(err);
        }
    });

    it("connection id is alwys null is negotiation is skipped", async () => {
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
        } catch (err) {
            fail(err);
        }
    });

    if (typeof EventSource !== "undefined") {
        it("allows Server-Sent Events when negotiating for JSON protocol", async () => {
            const hubConnection = getConnectionBuilder(undefined, TESTHUB_NOWEBSOCKETS_ENDPOINT_URL)
                .withHubProtocol(new JsonHubProtocol())
                .build();

            try {
                await hubConnection.start();

                // Check what transport was used by asking the server to tell us.
                expect(await hubConnection.invoke("GetActiveTransportName")).toEqual("ServerSentEvents");

                await hubConnection.stop();
            } catch (e) {
                fail(e);
            }
        });
    }

    it("skips Server-Sent Events when negotiating for MessagePack protocol", async () => {
        const hubConnection = getConnectionBuilder(undefined, TESTHUB_NOWEBSOCKETS_ENDPOINT_URL)
            .withHubProtocol(new MessagePackHubProtocol())
            .build();

        try {
            await hubConnection.start();

            // Check what transport was used by asking the server to tell us.
            expect(await hubConnection.invoke("GetActiveTransportName")).toEqual("LongPolling");

            await hubConnection.stop();
        } catch (e) {
            fail(e);
        }
    });

    it("transport falls back from WebSockets to SSE or LongPolling", async () => {
        // Skip test on Node as there will always be a WebSockets implementation on Node
        if (typeof window === "undefined") {
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
        }
    });

    it("over LongPolling it sends DELETE request and waits for poll to terminate", async () => {
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
        }
    });

    eachTransport((t) => {
        it("sets the user agent header", async () => {
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
            } catch (e) {
                fail(e);
            }
        });

        it("overwrites library headers with user headers", async () => {
            const [name] = getUserAgentHeader();
            const headers = { [name]: "Custom Agent", "X-HEADER": "VALUE" };
            const hubConnection = getConnectionBuilder(t, TESTHUBENDPOINT_URL, { headers })
                .withHubProtocol(new JsonHubProtocol())
                .build();

            try {
                await hubConnection.start();

                const customUserHeader = await hubConnection.invoke("GetHeader", "X-HEADER");
                const headerValue = await hubConnection.invoke("GetHeader", name);

                if ((t === HttpTransportType.ServerSentEvents || t === HttpTransportType.WebSockets) && !Platform.isNode) {
                    expect(headerValue).toBeNull();
                    expect(customUserHeader).toBeNull();
                } else {
                    expect(headerValue).toEqual("Custom Agent");
                    expect(customUserHeader).toEqual("VALUE");
                }

                await hubConnection.stop();
            } catch (e) {
                fail(e);
            }
        });
    });

    function getJwtToken(url: string): Promise<string> {
        return new Promise((resolve, reject) => {
            const httpClient = new DefaultHttpClient(NullLogger.instance);
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
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
