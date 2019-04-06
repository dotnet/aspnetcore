// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { DefaultReconnectPolicy } from "../src/DefaultReconnectPolicy";
import { HubConnection, HubConnectionState } from "../src/HubConnection";
import { JsonHubProtocol } from "../src/JsonHubProtocol";

import { VerifyLogger } from "./Common";
import { TestConnection } from "./TestConnection";
import {  PromiseSource } from "./Utils";

describe("auto reconnect", () => {
    it("is not enabled by default", async () => {
        await VerifyLogger.run(async (logger) => {
            const closePromise = new PromiseSource();
            let onreconnectingCalled = false;

            const connection = new TestConnection();
            const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol());

            hubConnection.onclose(() => {
                closePromise.resolve();
            });

            hubConnection.onreconnecting(() => {
                onreconnectingCalled = true;
            });

            await hubConnection.start();

            // Typically this would be called by the transport
            connection.onclose!(new Error("Connection lost"));

            await closePromise;

            expect(onreconnectingCalled).toBe(false);
        },
        "Connection disconnected with error 'Error: WebSocket closed with status code: 0 ().'.");
    });

    // it("can be opted into", async () => {
    //     let negotiateCount = 0;

    //     await VerifyLogger.run(async (logger) => {
    //         const options = {
    //             ...commonOptions,
    //             httpClient: new TestHttpClient()
    //                 .on("POST", () => {
    //                     negotiateCount++;
    //                     return defaultNegotiateResponse;
    //                 }),
    //             logger,
    //             reconnectPolicy: new DefaultReconnectPolicy(),
    //         };

    //         const reconnectingPromise = new PromiseSource();
    //         const reconnectedPromise = new PromiseSource();
    //         let oncloseCalled = false;
    //         let onreconnectingCallCount = 0;

    //         const connection = new HttpConnection("http://tempuri.org", options);

    //         TestWebSocket.webSocketSet = new PromiseSource();

    //         const startPromise = connection.start(TransferFormat.Text);

    //         await TestWebSocket.webSocketSet;
    //         await TestWebSocket.webSocket.closeSet;

    //         TestWebSocket.webSocket.onopen(new TestEvent());

    //         await startPromise;

    //         connection.onclose = (e) => {
    //             oncloseCalled = true;
    //         };

    //         connection.onreconnecting = (e) => {
    //             onreconnectingCallCount++;
    //             reconnectingPromise.resolve();
    //         };

    //         connection.onreconnected = (connectionId) => {
    //             reconnectedPromise.resolve();
    //         };

    //         TestWebSocket.webSocketSet = new PromiseSource();

    //         TestWebSocket.webSocket.onclose(new TestCloseEvent());

    //         await reconnectingPromise;

    //         await TestWebSocket.webSocketSet;
    //         await TestWebSocket.webSocket.closeSet;

    //         TestWebSocket.webSocket.onopen(new TestEvent());

    //         await reconnectedPromise;

    //         expect(onreconnectingCallCount).toBe(1);
    //         expect(oncloseCalled).toBe(false);

    //         await connection.stop();

    //         expect(onreconnectingCallCount).toBe(1);
    //         expect(oncloseCalled).toBe(true);

    //         expect(negotiateCount).toBe(2);
    //     },
    //     "Connection disconnected with error 'Error: WebSocket closed with status code: 0 ().'.");
    // });

    // it("can be triggered by calling connectionLost()", async () => {
    //     await VerifyLogger.run(async (logger) => {
    //         const options = {
    //             ...commonOptions,
    //             logger,
    //             reconnectPolicy: new DefaultReconnectPolicy(),
    //         };

    //         const reconnectingPromise = new PromiseSource();
    //         const reconnectedPromise = new PromiseSource();
    //         let oncloseCalled = false;
    //         let onreconnectingCallCount = 0;

    //         const connection = new HttpConnection("http://tempuri.org", options);

    //         TestWebSocket.webSocketSet = new PromiseSource();

    //         const startPromise = connection.start(TransferFormat.Text);

    //         await TestWebSocket.webSocketSet;
    //         await TestWebSocket.webSocket.closeSet;

    //         TestWebSocket.webSocket.onopen(new TestEvent());

    //         await startPromise;

    //         connection.onclose = (e) => {
    //             oncloseCalled = true;
    //         };

    //         connection.onreconnecting = (e) => {
    //             onreconnectingCallCount++;
    //             reconnectingPromise.resolve();
    //         };

    //         connection.onreconnected = (connectionId) => {
    //             reconnectedPromise.resolve();
    //         };

    //         TestWebSocket.webSocketSet = new PromiseSource();

    //         await connection.connectionLost(new Error());

    //         await reconnectingPromise;

    //         await TestWebSocket.webSocketSet;
    //         await TestWebSocket.webSocket.closeSet;

    //         TestWebSocket.webSocket.onopen(new TestEvent());

    //         await reconnectedPromise;

    //         expect(onreconnectingCallCount).toBe(1);
    //         expect(oncloseCalled).toBe(false);

    //         await connection.stop();

    //         expect(onreconnectingCallCount).toBe(1);
    //         expect(oncloseCalled).toBe(true);
    //     },
    //     "Connection disconnected with error 'Error: WebSocket closed with status code: 0 ().'.");
    // });

    // it("will attempt to use all available transports", async () => {
    //     await VerifyLogger.run(async (logger) => {
    //         const options = {
    //             ...commonOptions,
    //             logger,
    //             reconnectPolicy: new DefaultReconnectPolicy(),
    //             transport: undefined,
    //         };

    //         const reconnectingPromise = new PromiseSource();
    //         const reconnectedPromise = new PromiseSource();
    //         let oncloseCalled = false;
    //         let onreconnectingCallCount = 0;

    //         const connection = new HttpConnection("http://tempuri.org", options);

    //         TestWebSocket.webSocketSet = new PromiseSource();

    //         const startPromise = connection.start(TransferFormat.Text);

    //         await TestWebSocket.webSocketSet;
    //         await TestWebSocket.webSocket.closeSet;

    //         TestWebSocket.webSocket.onopen(new TestEvent());

    //         await startPromise;

    //         connection.onclose = (e) => {
    //             oncloseCalled = true;
    //         };

    //         connection.onreconnecting = (e) => {
    //             onreconnectingCallCount++;
    //             reconnectingPromise.resolve();
    //         };

    //         connection.onreconnected = (connectionId) => {
    //             reconnectedPromise.resolve();
    //         };

    //         TestWebSocket.webSocketSet = new PromiseSource();
    //         TestEventSource.eventSourceSet = new PromiseSource();

    //         TestWebSocket.webSocket.onclose(new TestCloseEvent());

    //         await reconnectingPromise;

    //         await TestWebSocket.webSocketSet;
    //         await TestWebSocket.webSocket.closeSet;

    //         TestWebSocket.webSocket.onerror(new TestEvent());

    //         await TestEventSource.eventSourceSet;
    //         await TestEventSource.eventSource.openSet;
    //         TestEventSource.eventSource.onopen(new TestMessageEvent());

    //         await reconnectedPromise;

    //         expect(onreconnectingCallCount).toBe(1);
    //         expect(oncloseCalled).toBe(false);

    //         await connection.stop();

    //         expect(onreconnectingCallCount).toBe(1);
    //         expect(oncloseCalled).toBe(true);
    //     },
    //     "Connection disconnected with error 'Error: WebSocket closed with status code: 0 ().'.",
    //     "Failed to start the transport 'WebSockets': null");
    // });

    // it("attempts can be exhausted", async () => {
    //     const reconnectDelays = [0, 0, 0, 0];
    //     let negotiateCount = 0;

    //     await VerifyLogger.run(async (logger) => {
    //         const options = {
    //             ...commonOptions,
    //             httpClient: new TestHttpClient()
    //                 .on("POST", () => {
    //                     negotiateCount++;
    //                     return defaultNegotiateResponse;
    //                 }),
    //             logger,
    //             reconnectPolicy: new DefaultReconnectPolicy(reconnectDelays),
    //         };

    //         const reconnectingPromise = new PromiseSource();
    //         const closePromise = new PromiseSource();
    //         let onreconnectedCalled = false;
    //         let onreconnectingCallCount = 0;

    //         const connection = new HttpConnection("http://tempuri.org", options);

    //         TestWebSocket.webSocketSet = new PromiseSource();

    //         const startPromise = connection.start(TransferFormat.Text);

    //         await TestWebSocket.webSocketSet;
    //         await TestWebSocket.webSocket.closeSet;

    //         TestWebSocket.webSocket.onopen(new TestEvent());

    //         await startPromise;

    //         connection.onclose = (e) => {
    //             closePromise.resolve();
    //         };

    //         connection.onreconnecting = (e) => {
    //             onreconnectingCallCount++;
    //             reconnectingPromise.resolve();
    //         };

    //         connection.onreconnected = (connectionId) => {
    //             onreconnectedCalled = true;
    //         };

    //         TestWebSocket.webSocketSet = new PromiseSource();

    //         TestWebSocket.webSocket.onclose(new TestCloseEvent());

    //         await reconnectingPromise;

    //         for (const _ of reconnectDelays) {
    //             await TestWebSocket.webSocketSet;
    //             await TestWebSocket.webSocket.closeSet;

    //             TestWebSocket.webSocketSet = new PromiseSource();

    //             TestWebSocket.webSocket.onerror(new TestEvent());
    //         }

    //         await closePromise;

    //         expect(onreconnectingCallCount).toBe(1);
    //         expect(onreconnectedCalled).toBe(false);

    //         expect(negotiateCount).toBe(5);
    //     },
    //     "Connection disconnected with error 'Error: WebSocket closed with status code: 0 ().'.",
    //     "Failed to start the transport 'WebSockets': null",
    //     "Failed to start the transport 'ServerSentEvents': Error: 'ServerSentEvents' is disabled by the client.",
    //     "Failed to start the transport 'LongPolling': Error: 'LongPolling' is disabled by the client.");
    // });

    // it("does not reset after hub handshake failure", async () => {
    //     const reconnectDelays = [0, 0];
    //     let negotiateCount = 0;

    //     await VerifyLogger.run(async (logger) => {
    //         const options = {
    //             ...commonOptions,
    //             httpClient: new TestHttpClient()
    //                 .on("POST", () => {
    //                     negotiateCount++;
    //                     return defaultNegotiateResponse;
    //                 }),
    //             logger,
    //             reconnectPolicy: new DefaultReconnectPolicy(reconnectDelays),
    //         };

    //         const reconnectingPromise = new PromiseSource();
    //         const closePromise = new PromiseSource();
    //         let onreconnectedCalled = false;
    //         let onreconnectingCallCount = 0;

    //         const connection = new HttpConnection("http://tempuri.org", options);
    //         const hubConnection = HubConnection.create(connection, logger, new JsonHubProtocol());

    //         TestWebSocket.webSocketSet = new PromiseSource();

    //         const startPromise = hubConnection.start();

    //         await TestWebSocket.webSocketSet;
    //         await TestWebSocket.webSocket.closeSet;

    //         TestWebSocket.webSocket.readyState = TestWebSocket.OPEN;
    //         TestWebSocket.webSocket.onopen(new TestEvent());

    //         await new Promise((resolve) => setTimeout(resolve, 100));

    //         TestWebSocket.webSocket.onmessage({ data: "{}" + TextMessageFormat.RecordSeparator } as MessageEvent);

    //         await startPromise;

    //         hubConnection.onclose((e) => {
    //             closePromise.resolve();
    //         });

    //         hubConnection.onreconnecting((e) => {
    //             onreconnectingCallCount++;
    //             reconnectingPromise.resolve();
    //         });

    //         hubConnection.onreconnected((connectionId) => {
    //             onreconnectedCalled = true;
    //         });

    //         TestWebSocket.webSocketSet = new PromiseSource();

    //         TestWebSocket.webSocket.onclose(new TestCloseEvent());

    //         await reconnectingPromise;

    //         expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);

    //         await TestWebSocket.webSocketSet;
    //         await TestWebSocket.webSocket.closeSet;

    //         TestWebSocket.webSocketSet = new PromiseSource();

    //         TestWebSocket.webSocket.readyState = TestWebSocket.OPEN;
    //         TestWebSocket.webSocket.onopen(new TestEvent());
    //         await new Promise((resolve) => setTimeout(resolve, 100));
    //         expect(() => TestWebSocket.webSocket.onmessage({ data: "invalid handshake response" } as MessageEvent)).toThrow("Error parsing handshake response: Error: Message is incomplete.");

    //         expect(hubConnection.state).toBe(HubConnectionState.Reconnecting);

    //         await TestWebSocket.webSocketSet;
    //         await TestWebSocket.webSocket.closeSet;

    //         TestWebSocket.webSocket.readyState = TestWebSocket.OPEN;
    //         TestWebSocket.webSocket.onopen(new TestEvent());
    //         await new Promise((resolve) => setTimeout(resolve, 100));
    //         expect(() => TestWebSocket.webSocket.onmessage({ data: "invalid handshake response" } as MessageEvent)).toThrow("Error parsing handshake response: Error: Message is incomplete.");

    //         await closePromise;

    //         expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
    //         expect(onreconnectingCallCount).toBe(1);
    //         expect(onreconnectedCalled).toBe(false);

    //         expect(negotiateCount).toBe(3);
    //     },
    //     "Connection disconnected with error 'Error: WebSocket closed with status code: 0 ().'.",
    //     "Error parsing handshake response: Error: Message is incomplete.",
    //     "Connection disconnected with error 'Error: Error parsing handshake response: Error: Message is incomplete.'.");
    // });
});
