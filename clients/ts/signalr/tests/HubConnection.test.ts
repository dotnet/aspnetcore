// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HubConnection, HubConnectionState } from "../src/HubConnection";
import { IConnection } from "../src/IConnection";
import { HubMessage, IHubProtocol, MessageType } from "../src/IHubProtocol";
import { ILogger, LogLevel } from "../src/ILogger";
import { TransferFormat } from "../src/ITransport";
import { JsonHubProtocol } from "../src/JsonHubProtocol";
import { NullLogger } from "../src/Loggers";
import { IStreamSubscriber } from "../src/Stream";
import { TextMessageFormat } from "../src/TextMessageFormat";

import { delay, PromiseSource } from "./Utils";

function createHubConnection(connection: IConnection, logger?: ILogger, protocol?: IHubProtocol) {
    return HubConnection.create(connection, logger || NullLogger.instance, protocol || new JsonHubProtocol());
}

describe("HubConnection", () => {

    describe("start", () => {
        it("sends negotiation message", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                await hubConnection.start();
                expect(connection.sentData.length).toBe(1);
                expect(JSON.parse(connection.sentData[0])).toEqual({
                    protocol: "json",
                    version: 1,
                });
            } finally {
                await hubConnection.stop();
            }
        });

        it("state connected", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
            try {
                await hubConnection.start();
                expect(hubConnection.state).toBe(HubConnectionState.Connected);
            } finally {
                await hubConnection.stop();
            }
        });
    });

    describe("ping", () => {
        it("automatically sends multiple pings", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);

            hubConnection.keepAliveIntervalInMilliseconds = 5;

            try {
                await hubConnection.start();
                await delay(500);

                const numPings = connection.sentData.filter((s) => JSON.parse(s).type === MessageType.Ping).length;
                expect(numPings).toBeGreaterThanOrEqual(2);
            } finally {
                await hubConnection.stop();
            }
        });
    });

    describe("stop", () => {
        it("state disconnected", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
            try {
                await hubConnection.start();
                expect(hubConnection.state).toBe(HubConnectionState.Connected);
            } finally {
                await hubConnection.stop();
                expect(hubConnection.state).toBe(HubConnectionState.Disconnected);
            }
        });
    });

    describe("send", () => {
        it("sends a non blocking invocation", async () => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection);
            try {
                const invokePromise = hubConnection.send("testMethod", "arg", 42)
                    .catch((_) => { }); // Suppress exception and unhandled promise rejection warning.

                // Verify the message is sent
                expect(connection.sentData.length).toBe(1);
                expect(JSON.parse(connection.sentData[0])).toEqual({
                    arguments: [
                        "arg",
                        42,
                    ],
                    target: "testMethod",
                    type: MessageType.Invocation,
                });
            } finally {
                // Close the connection
                hubConnection.stop();
            }
        });
    });

    describe("invoke", () => {
        it("sends an invocation", async () => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection);
            try {
                const invokePromise = hubConnection.invoke("testMethod", "arg", 42)
                    .catch((_) => { }); // Suppress exception and unhandled promise rejection warning.

                // Verify the message is sent
                expect(connection.sentData.length).toBe(1);
                expect(JSON.parse(connection.sentData[0])).toEqual({
                    arguments: [
                        "arg",
                        42,
                    ],
                    invocationId: connection.lastInvocationId,
                    target: "testMethod",
                    type: MessageType.Invocation,
                });

            } finally {
                // Close the connection
                hubConnection.stop();
            }
        });

        it("can process handshake from text", async () => {
            let protocolCalled = false;

            const mockProtocol = new TestProtocol(TransferFormat.Text);
            mockProtocol.onreceive = (d) => {
                protocolCalled = true;
            };

            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection, null, mockProtocol);
            try {
                const data = "{}" + TextMessageFormat.RecordSeparator;

                connection.receiveText(data);

                // message only contained handshake response
                expect(protocolCalled).toEqual(false);
            } finally {
                hubConnection.stop();
            }
        });

        it("can process handshake from binary", async () => {
            let protocolCalled = false;

            const mockProtocol = new TestProtocol(TransferFormat.Binary);
            mockProtocol.onreceive = (d) => {
                protocolCalled = true;
            };

            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection, null, mockProtocol);
            try {
                // handshake response + message separator
                const data = [0x7b, 0x7d, 0x1e];

                connection.receiveBinary(new Uint8Array(data).buffer);

                // message only contained handshake response
                expect(protocolCalled).toEqual(false);
            } finally {
                hubConnection.stop();
            }
        });

        it("can process handshake and additional messages from binary", async () => {
            let receivedProcotolData: ArrayBuffer;

            const mockProtocol = new TestProtocol(TransferFormat.Binary);
            mockProtocol.onreceive = (d) => receivedProcotolData = d as ArrayBuffer;

            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection, null, mockProtocol);
            try {
                // handshake response + message separator + message pack message
                const data = [
                    0x7b, 0x7d, 0x1e, 0x65, 0x95, 0x03, 0x80, 0xa1, 0x30, 0x01, 0xd9, 0x5d, 0x54, 0x68, 0x65, 0x20, 0x63, 0x6c,
                    0x69, 0x65, 0x6e, 0x74, 0x20, 0x61, 0x74, 0x74, 0x65, 0x6d, 0x70, 0x74, 0x65, 0x64, 0x20, 0x74, 0x6f, 0x20,
                    0x69, 0x6e, 0x76, 0x6f, 0x6b, 0x65, 0x20, 0x74, 0x68, 0x65, 0x20, 0x73, 0x74, 0x72, 0x65, 0x61, 0x6d, 0x69,
                    0x6e, 0x67, 0x20, 0x27, 0x45, 0x6d, 0x70, 0x74, 0x79, 0x53, 0x74, 0x72, 0x65, 0x61, 0x6d, 0x27, 0x20, 0x6d,
                    0x65, 0x74, 0x68, 0x6f, 0x64, 0x20, 0x69, 0x6e, 0x20, 0x61, 0x20, 0x6e, 0x6f, 0x6e, 0x2d, 0x73, 0x74, 0x72,
                    0x65, 0x61, 0x6d, 0x69, 0x6e, 0x67, 0x20, 0x66, 0x61, 0x73, 0x68, 0x69, 0x6f, 0x6e, 0x2e,
                ];

                connection.receiveBinary(new Uint8Array(data).buffer);

                // left over data is the message pack message
                expect(receivedProcotolData.byteLength).toEqual(102);
            } finally {
                hubConnection.stop();
            }
        });

        it("can process handshake and additional messages from text", async () => {
            let receivedProcotolData: string;

            const mockProtocol = new TestProtocol(TransferFormat.Text);
            mockProtocol.onreceive = (d) => receivedProcotolData = d as string;

            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection, null, mockProtocol);
            try {
                const data = "{}" + TextMessageFormat.RecordSeparator + "{\"type\":6}" + TextMessageFormat.RecordSeparator;

                connection.receiveText(data);

                expect(receivedProcotolData).toEqual("{\"type\":6}" + TextMessageFormat.RecordSeparator);
            } finally {
                hubConnection.stop();
            }
        });

        it("rejects the promise when an error is received", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                const invokePromise = hubConnection.invoke("testMethod", "arg", 42);

                connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, error: "foo" });

                await expect(invokePromise).rejects.toThrow("foo");
            } finally {
                hubConnection.stop();
            }
        });

        it("resolves the promise when a result is received", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                const invokePromise = hubConnection.invoke("testMethod", "arg", 42);

                connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, result: "foo" });

                expect(await invokePromise).toBe("foo");
            } finally {
                hubConnection.stop();
            }
        });

        it("completes pending invocations when stopped", async () => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection);

            connection.receiveHandshakeResponse();

            const invokePromise = hubConnection.invoke("testMethod");
            hubConnection.stop();

            expect(invokePromise).rejects.toThrow("Invocation canceled due to connection being closed.");
        });

        it("completes pending invocations when connection is lost", async () => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                const invokePromise = hubConnection.invoke("testMethod");
                // Typically this would be called by the transport
                connection.onclose(new Error("Connection lost"));

                expect(invokePromise).rejects.toThrow("Connection lost");
            } finally {
                hubConnection.stop();
            }
        });
    });

    describe("on", () => {
        it("invocations ignored in callbacks not registered", async () => {
            const warnings: string[] = [];
            const logger = {
                log: (logLevel: LogLevel, message: string) => {
                    if (logLevel === LogLevel.Warning) {
                        warnings.push(message);
                    }
                },
            } as ILogger;
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection, logger);
            try {
                connection.receiveHandshakeResponse();

                connection.receive({
                    arguments: ["test"],
                    nonblocking: true,
                    target: "message",
                    type: MessageType.Invocation,
                });

                expect(warnings).toEqual(["No client method with the name 'message' found."]);
            } finally {
                hubConnection.stop();
            }
        });

        it("invocations ignored in callbacks that have registered then unregistered", async () => {
            const warnings: string[] = [];
            const logger = {
                log: (logLevel: LogLevel, message: string) => {
                    if (logLevel === LogLevel.Warning) {
                        warnings.push(message);
                    }
                },
            } as ILogger;
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection, logger);
            try {
                connection.receiveHandshakeResponse();

                const handler = () => { };
                hubConnection.on("message", handler);
                hubConnection.off("message", handler);

                connection.receive({
                    arguments: ["test"],
                    invocationId: "0",
                    nonblocking: true,
                    target: "message",
                    type: MessageType.Invocation,
                });

                expect(warnings).toEqual(["No client method with the name 'message' found."]);
            } finally {
                hubConnection.stop();
            }
        });

        it("all handlers can be unregistered with just the method name", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                let count = 0;
                const handler = () => { count++; };
                const secondHandler = () => { count++; };
                hubConnection.on("inc", handler);
                hubConnection.on("inc", secondHandler);

                connection.receive({
                    arguments: [],
                    invocationId: "0",
                    nonblocking: true,
                    target: "inc",
                    type: MessageType.Invocation,
                });

                hubConnection.off("inc");

                connection.receive({
                    arguments: [],
                    invocationId: "0",
                    nonblocking: true,
                    target: "inc",
                    type: MessageType.Invocation,
                });

                expect(count).toBe(2);
            } finally {
                hubConnection.stop();
            }
        });

        it("a single handler can be unregistered with the method name and handler", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                let count = 0;
                const handler = () => { count++; };
                const secondHandler = () => { count++; };
                hubConnection.on("inc", handler);
                hubConnection.on("inc", secondHandler);

                connection.receive({
                    arguments: [],
                    invocationId: "0",
                    nonblocking: true,
                    target: "inc",
                    type: MessageType.Invocation,
                });

                hubConnection.off("inc", handler);

                connection.receive({
                    arguments: [],
                    invocationId: "0",
                    nonblocking: true,
                    target: "inc",
                    type: MessageType.Invocation,
                });

                expect(count).toBe(3);
            } finally {
                hubConnection.stop();
            }
        });

        it("can't register the same handler multiple times", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                let count = 0;
                const handler = () => { count++; };
                hubConnection.on("inc", handler);
                hubConnection.on("inc", handler);

                connection.receive({
                    arguments: [],
                    invocationId: "0",
                    nonblocking: true,
                    target: "inc",
                    type: MessageType.Invocation,
                });

                expect(count).toBe(1);
            } finally {
                hubConnection.stop();
            }
        });

        it("callback invoked when servers invokes a method on the client", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                let value = "";
                hubConnection.on("message", (v) => value = v);

                connection.receive({
                    arguments: ["test"],
                    invocationId: "0",
                    nonblocking: true,
                    target: "message",
                    type: MessageType.Invocation,
                });

                expect(value).toBe("test");
            } finally {
                hubConnection.stop();
            }
        });

        it("stop on handshake error", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                let closeError: Error = null;
                hubConnection.onclose((e) => closeError = e);

                connection.receiveHandshakeResponse("Error!");

                expect(closeError.message).toEqual("Server returned handshake error: Error!");
            } finally {
                hubConnection.stop();
            }
        });

        it("stop on close message", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                let isClosed = false;
                let closeError: Error = null;
                hubConnection.onclose((e) => {
                    isClosed = true;
                    closeError = e;
                });

                connection.receiveHandshakeResponse();

                connection.receive({
                    type: MessageType.Close,
                });

                expect(isClosed).toEqual(true);
                expect(closeError).toEqual(null);
            } finally {
                hubConnection.stop();
            }
        });

        it("stop on error close message", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                let isClosed = false;
                let closeError: Error = null;
                hubConnection.onclose((e) => {
                    isClosed = true;
                    closeError = e;
                });

                connection.receiveHandshakeResponse();

                connection.receive({
                    error: "Error!",
                    type: MessageType.Close,
                });

                expect(isClosed).toEqual(true);
                expect(closeError.message).toEqual("Server returned an error on close: Error!");
            } finally {
                hubConnection.stop();
            }
        });

        it("can have multiple callbacks", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                let numInvocations1 = 0;
                let numInvocations2 = 0;
                hubConnection.on("message", () => numInvocations1++);
                hubConnection.on("message", () => numInvocations2++);

                connection.receive({
                    arguments: [],
                    invocationId: "0",
                    nonblocking: true,
                    target: "message",
                    type: MessageType.Invocation,
                });

                expect(numInvocations1).toBe(1);
                expect(numInvocations2).toBe(1);
            } finally {
                hubConnection.stop();
            }
        });

        it("can unsubscribe from on", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                let numInvocations = 0;
                const callback = () => numInvocations++;
                hubConnection.on("message", callback);

                connection.receive({
                    arguments: [],
                    invocationId: "0",
                    nonblocking: true,
                    target: "message",
                    type: MessageType.Invocation,
                });

                hubConnection.off("message", callback);

                connection.receive({
                    arguments: [],
                    invocationId: "0",
                    nonblocking: true,
                    target: "message",
                    type: MessageType.Invocation,
                });

                expect(numInvocations).toBe(1);
            } finally {
                hubConnection.stop();
            }
        });

        it("unsubscribing from non-existing callbacks no-ops", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                hubConnection.off("_", () => { });
                hubConnection.on("message", (t) => { });
                hubConnection.on("message", () => { });
            } finally {
                hubConnection.stop();
            }
        });

        it("using null/undefined for methodName or method no-ops", async () => {
            const warnings: string[] = [];
            const logger = {
                log(logLevel: LogLevel, message: string) {
                    if (logLevel === LogLevel.Warning) {
                        warnings.push(message);
                    }

                },
            } as ILogger;

            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection, logger);
            try {
                connection.receiveHandshakeResponse();

                hubConnection.on(null, undefined);
                hubConnection.on(undefined, null);
                hubConnection.on("message", null);
                hubConnection.on("message", undefined);
                hubConnection.on(null, () => { });
                hubConnection.on(undefined, () => { });

                // invoke a method to make sure we are not trying to use null/undefined
                connection.receive({
                    arguments: [],
                    invocationId: "0",
                    nonblocking: true,
                    target: "message",
                    type: MessageType.Invocation,
                });

                expect(warnings).toEqual(["No client method with the name 'message' found."]);

                hubConnection.off(null, undefined);
                hubConnection.off(undefined, null);
                hubConnection.off("message", null);
                hubConnection.off("message", undefined);
                hubConnection.off(null, () => { });
                hubConnection.off(undefined, () => { });
            } finally {
                hubConnection.stop();
            }
        });
    });

    describe("stream", () => {
        it("sends an invocation", async () => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection);
            try {
                const invokePromise = hubConnection.stream("testStream", "arg", 42);

                // Verify the message is sent
                expect(connection.sentData.length).toBe(1);
                expect(JSON.parse(connection.sentData[0])).toEqual({
                    arguments: [
                        "arg",
                        42,
                    ],
                    invocationId: connection.lastInvocationId,
                    target: "testStream",
                    type: MessageType.StreamInvocation,
                });

                // Close the connection
                hubConnection.stop();
            } finally {
                hubConnection.stop();
            }
        });

        it("completes with an error when an error is yielded", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                const observer = new TestObserver();
                hubConnection.stream<any>("testMethod", "arg", 42)
                    .subscribe(observer);

                connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, error: "foo" });

                expect(observer.completed).rejects.toThrow("Error: foo");
            } finally {
                hubConnection.stop();
            }
        });

        it("completes the observer when a completion is received", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                const observer = new TestObserver();
                hubConnection.stream<any>("testMethod", "arg", 42)
                    .subscribe(observer);

                connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId });

                expect(await observer.completed).toEqual([]);
            } finally {
                hubConnection.stop();
            }
        });

        it("completes pending streams when stopped", async () => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection);
            try {
                const observer = new TestObserver();
                hubConnection.stream<any>("testMethod")
                    .subscribe(observer);
                hubConnection.stop();

                expect(observer.completed).rejects.toThrow("Error: Invocation canceled due to connection being closed.");
            } finally {
                hubConnection.stop();
            }
        });

        it("completes pending streams when connection is lost", async () => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection);
            try {
                const observer = new TestObserver();
                hubConnection.stream<any>("testMethod")
                    .subscribe(observer);

                // Typically this would be called by the transport
                connection.onclose(new Error("Connection lost"));

                expect(observer.completed).rejects.toThrow("Error: Connection lost");
            } finally {
                hubConnection.stop();
            }
        });

        it("yields items as they arrive", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                const observer = new TestObserver();
                hubConnection.stream<any>("testMethod")
                    .subscribe(observer);

                connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 1 });
                expect(observer.itemsReceived).toEqual([1]);

                connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 2 });
                expect(observer.itemsReceived).toEqual([1, 2]);

                connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 3 });
                expect(observer.itemsReceived).toEqual([1, 2, 3]);

                connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId });
                expect(await observer.completed).toEqual([1, 2, 3]);
            } finally {
                hubConnection.stop();
            }
        });

        it("does not require error function registered", async () => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection);
            try {
                const observer = hubConnection.stream("testMethod").subscribe(NullSubscriber.instance);

                // Typically this would be called by the transport
                // triggers observer.error()
                connection.onclose(new Error("Connection lost"));
            } finally {
                hubConnection.stop();
            }
        });

        it("does not require complete function registered", async () => {
            const connection = new TestConnection();

            const hubConnection = createHubConnection(connection);
            try {
                const observer = hubConnection.stream("testMethod").subscribe(NullSubscriber.instance);

                // Send completion to trigger observer.complete()
                // Expectation is connection.receive will not to throw
                connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId });
            } finally {
                hubConnection.stop();
            }
        });

        it("can be canceled", () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                connection.receiveHandshakeResponse();

                const observer = new TestObserver();
                const subscription = hubConnection.stream("testMethod")
                    .subscribe(observer);

                connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 1 });
                expect(observer.itemsReceived).toEqual([1]);

                subscription.dispose();

                connection.receive({ type: MessageType.StreamItem, invocationId: connection.lastInvocationId, item: 2 });
                // Observer should no longer receive messages
                expect(observer.itemsReceived).toEqual([1]);

                // Verify the cancel is sent
                expect(connection.sentData.length).toBe(2);
                expect(JSON.parse(connection.sentData[1])).toEqual({
                    invocationId: connection.lastInvocationId,
                    type: MessageType.CancelInvocation,
                });
            } finally {
                hubConnection.stop();
            }
        });
    });

    describe("onClose", () => {
        it("can have multiple callbacks", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                let invocations = 0;
                hubConnection.onclose((e) => invocations++);
                hubConnection.onclose((e) => invocations++);
                // Typically this would be called by the transport
                connection.onclose();
                expect(invocations).toBe(2);
            } finally {
                hubConnection.stop();
            }
        });

        it("callbacks receive error", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                let error: Error;
                hubConnection.onclose((e) => error = e);

                // Typically this would be called by the transport
                connection.onclose(new Error("Test error."));
                expect(error.message).toBe("Test error.");
            } finally {
                hubConnection.stop();
            }
        });

        it("ignores null callbacks", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                hubConnection.onclose(null);
                hubConnection.onclose(undefined);
                // Typically this would be called by the transport
                connection.onclose();
                // expect no errors
            } finally {
                hubConnection.stop();
            }
        });

        it("state disconnected", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                let state: HubConnectionState;
                hubConnection.onclose((e) => state = hubConnection.state);
                // Typically this would be called by the transport
                connection.onclose();

                expect(state).toBe(HubConnectionState.Disconnected);
            } finally {
                hubConnection.stop();
            }
        });
    });

    describe("keepAlive", () => {
        it("can receive ping messages", async () => {
            // Receive the ping mid-invocation so we can see that the rest of the flow works fine

            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                const invokePromise = hubConnection.invoke("testMethod", "arg", 42);

                connection.receive({ type: MessageType.Ping });
                connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, result: "foo" });

                expect(await invokePromise).toBe("foo");
            } finally {
                hubConnection.stop();
            }
        });

        it("does not terminate if messages are received", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                hubConnection.serverTimeoutInMilliseconds = 200;

                const p = new PromiseSource<Error>();
                hubConnection.onclose((e) => p.resolve(e));

                await hubConnection.start();

                for (let i = 0; i < 6; i++) {
                    await pingAndWait(connection);
                }

                connection.stop();

                const error = await p.promise;

                expect(error).toBeUndefined();
            } finally {
                hubConnection.stop();
            }
        });

        it("does not timeout if message was received before HubConnection.start", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                hubConnection.serverTimeoutInMilliseconds = 200;

                const p = new PromiseSource<Error>();
                hubConnection.onclose((e) => p.resolve(e));

                // send message before start to trigger timeout handler
                // testing for regression where we didn't cleanup timer if request received before start created a timer
                await connection.receive({ type: MessageType.Ping });

                await hubConnection.start();

                for (let i = 0; i < 6; i++) {
                    await pingAndWait(connection);
                }

                connection.stop();

                const error = await p.promise;

                expect(error).toBeUndefined();
            } finally {
                hubConnection.stop();
            }
        });

        it("terminates if no messages received within timeout interval", async () => {
            const connection = new TestConnection();
            const hubConnection = createHubConnection(connection);
            try {
                hubConnection.serverTimeoutInMilliseconds = 100;

                const p = new PromiseSource<Error>();
                hubConnection.onclose((e) => p.resolve(e));

                await hubConnection.start();

                const error = await p.promise;

                expect(error).toEqual(new Error("Server timeout elapsed without receiving a message from the server."));
            } finally {
                hubConnection.stop();
            }
        });
    });
});

async function pingAndWait(connection: TestConnection): Promise<void> {
    await connection.receive({ type: MessageType.Ping });
    await delay(50);
}

class TestConnection implements IConnection {
    public readonly features: any = {};

    public start(): Promise<void> {
        return Promise.resolve();
    }

    public send(data: any): Promise<void> {
        const invocation = TextMessageFormat.parse(data)[0];
        const invocationId = JSON.parse(invocation).invocationId;
        if (invocationId) {
            this.lastInvocationId = invocationId;
        }
        if (this.sentData) {
            this.sentData.push(invocation);
        } else {
            this.sentData = [invocation];
        }
        return Promise.resolve();
    }

    public stop(error?: Error): Promise<void> {
        if (this.onclose) {
            this.onclose(error);
        }
        return Promise.resolve();
    }

    public receiveHandshakeResponse(error?: string): void {
        this.receive({ error });
    }

    public receive(data: any): void {
        const payload = JSON.stringify(data);
        this.onreceive(TextMessageFormat.write(payload));
    }

    public receiveText(data: string) {
        this.onreceive(data);
    }

    public receiveBinary(data: ArrayBuffer) {
        this.onreceive(data);
    }

    public onreceive: (data: string | ArrayBuffer) => void;
    public onclose: (error?: Error) => void;
    public sentData: any[];
    public lastInvocationId: string;
}

class TestProtocol implements IHubProtocol {
    public readonly name: string = "TestProtocol";
    public readonly version: number = 1;

    public readonly transferFormat: TransferFormat;

    public onreceive: (data: string | ArrayBuffer) => void;

    constructor(transferFormat: TransferFormat) {
        this.transferFormat = transferFormat;
    }

    public parseMessages(input: any): HubMessage[] {
        if (this.onreceive) {
            this.onreceive(input);
        }

        return [];
    }

    public writeMessage(message: HubMessage): any {

    }
}

class TestObserver implements IStreamSubscriber<any> {
    public readonly closed: boolean;
    public itemsReceived: [any];
    private itemsSource: PromiseSource<[any]>;

    get completed(): Promise<[any]> {
        return this.itemsSource.promise;
    }

    constructor() {
        this.itemsReceived = [] as [any];
        this.itemsSource = new PromiseSource<[any]>();
    }

    public next(value: any) {
        this.itemsReceived.push(value);
    }

    public error(err: any) {
        this.itemsSource.reject(new Error(err));
    }

    public complete() {
        this.itemsSource.resolve(this.itemsReceived);
    }
}

class NullSubscriber<T> implements IStreamSubscriber<T> {
    public static instance: NullSubscriber<any> = new NullSubscriber();

    private constructor() {
    }

    public next(value: T): void {
    }
    public error(err: any): void {
    }
    public complete(): void {
    }
}
