// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { IConnection } from "../Microsoft.AspNetCore.SignalR.Client.TS/IConnection"
import { HubConnection } from "../Microsoft.AspNetCore.SignalR.Client.TS/HubConnection"
import { DataReceived, ConnectionClosed } from "../Microsoft.AspNetCore.SignalR.Client.TS/Common"
import { TransportType, ITransport, TransferMode } from "../Microsoft.AspNetCore.SignalR.Client.TS/Transports"
import { Observer } from "../Microsoft.AspNetCore.SignalR.Client.TS/Observable"
import { TextMessageFormat } from "../Microsoft.AspNetCore.SignalR.Client.TS/Formatters"
import { ILogger, LogLevel } from "../Microsoft.AspNetCore.SignalR.Client.TS/ILogger"
import { MessageType } from "../Microsoft.AspNetCore.SignalR.Client.TS/IHubProtocol"

import { asyncit as it, captureException, delay, PromiseSource } from './Utils';
import { IHubConnectionOptions } from "../Microsoft.AspNetCore.SignalR.Client.TS/IHubConnectionOptions";

describe("HubConnection", () => {

    describe("start", () => {
        it("sends negotiation message", async () => {
            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection, { logging: null });
            await hubConnection.start();
            expect(connection.sentData.length).toBe(1)
            expect(JSON.parse(connection.sentData[0])).toEqual({
                protocol: "json"
            });
            await hubConnection.stop();
        });
    });

    describe("send", () => {
        it("sends a non blocking invocation", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let invokePromise = hubConnection.send("testMethod", "arg", 42)
                .catch((_) => { }); // Suppress exception and unhandled promise rejection warning.

            // Verify the message is sent
            expect(connection.sentData.length).toBe(1);
            expect(JSON.parse(connection.sentData[0])).toEqual({
                type: MessageType.Invocation,
                invocationId: connection.lastInvocationId,
                target: "testMethod",
                nonblocking: true,
                arguments: [
                    "arg",
                    42
                ]
            });

            // Close the connection
            hubConnection.stop();
        });
    });

    describe("invoke", () => {
        it("sends an invocation", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let invokePromise = hubConnection.invoke("testMethod", "arg", 42)
                .catch((_) => { }); // Suppress exception and unhandled promise rejection warning.

            // Verify the message is sent
            expect(connection.sentData.length).toBe(1);
            expect(JSON.parse(connection.sentData[0])).toEqual({
                type: MessageType.Invocation,
                invocationId: connection.lastInvocationId,
                target: "testMethod",
                nonblocking: false,
                arguments: [
                    "arg",
                    42
                ]
            });

            // Close the connection
            hubConnection.stop();
        });

        it("rejects the promise when an error is received", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let invokePromise = hubConnection.invoke("testMethod", "arg", 42);

            connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, error: "foo" });

            let ex = await captureException(async () => invokePromise);
            expect(ex.message).toBe("foo");
        });

        it("resolves the promise when a result is received", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let invokePromise = hubConnection.invoke("testMethod", "arg", 42);

            connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, result: "foo" });

            expect(await invokePromise).toBe("foo");
        });

        it("completes pending invocations when stopped", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let invokePromise = hubConnection.invoke("testMethod");
            hubConnection.stop();

            let ex = await captureException(async () => await invokePromise);
            expect(ex.message).toBe("Invocation canceled due to connection being closed.");
        });

        it("completes pending invocations when connection is lost", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let invokePromise = hubConnection.invoke("testMethod");
            // Typically this would be called by the transport
            connection.onclose(new Error("Connection lost"));

            let ex = await captureException(async () => await invokePromise);
            expect(ex.message).toBe("Connection lost");
        });
    });

    describe("on", () => {
        it("invocations ignored in callbacks not registered", async () => {
            let warnings: string[] = [];
            let logger = <ILogger>{
                log: function (logLevel: LogLevel, message: string) {
                    if (logLevel === LogLevel.Warning) {
                        warnings.push(message);
                    }
                }
            };
            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection, { logging: logger });

            connection.receive({
                type: MessageType.Invocation,
                invocationId: 0,
                target: "message",
                arguments: ["test"],
                nonblocking: true
            });

            expect(warnings).toEqual(["No client method with the name 'message' found."]);
        });

        it("callback invoked when servers invokes a method on the client", async () => {
            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection);
            let value = 0;
            hubConnection.on("message", v => value = v);

            connection.receive({
                type: MessageType.Invocation,
                invocationId: 0,
                target: "message",
                arguments: ["test"],
                nonblocking: true
            });

            expect(value).toBe("test");
        });

        it("can have multiple callbacks", async () => {
            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection);
            let numInvocations1 = 0;
            let numInvocations2 = 0;
            hubConnection.on("message", () => numInvocations1++);
            hubConnection.on("message", () => numInvocations2++);

            connection.receive({
                type: MessageType.Invocation,
                invocationId: 0,
                target: "message",
                arguments: [],
                nonblocking: true
            });

            expect(numInvocations1).toBe(1);
            expect(numInvocations2).toBe(1);
        });

        it("can unsubscribe from on", async () => {
            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection);

            var numInvocations = 0;
            var callback = () => numInvocations++;
            hubConnection.on("message", callback);

            connection.receive({
                type: MessageType.Invocation,
                invocationId: 0,
                target: "message",
                arguments: [],
                nonblocking: true
            });

            hubConnection.off("message", callback);

            connection.receive({
                type: MessageType.Invocation,
                invocationId: 0,
                target: "message",
                arguments: [],
                nonblocking: true
            });

            expect(numInvocations).toBe(1);
        });

        it("unsubscribing from non-existing callbacks no-ops", async () => {
            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection);

            hubConnection.off("_", () => { });
            hubConnection.on("message", t => { });
            hubConnection.on("message", () => { });
        });

        it("using null/undefined for methodName or method no-ops", async () => {
            let warnings: string[] = [];
            let logger = <ILogger>{
                log: function (logLevel: LogLevel, message: string) {
                    if (logLevel === LogLevel.Warning) {
                        warnings.push(message);
                    }

                }
            };

            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection, { logging: logger });

            hubConnection.on(null, undefined);
            hubConnection.on(undefined, null);
            hubConnection.on("message", null);
            hubConnection.on("message", undefined);
            hubConnection.on(null, () => { });
            hubConnection.on(undefined, () => { });

            // invoke a method to make sure we are not trying to use null/undefined
            connection.receive({
                type: MessageType.Invocation,
                invocationId: 0,
                target: "message",
                arguments: [],
                nonblocking: true
            });

            expect(warnings).toEqual(["No client method with the name 'message' found."]);

            hubConnection.off(null, undefined);
            hubConnection.off(undefined, null);
            hubConnection.off("message", null);
            hubConnection.off("message", undefined);
            hubConnection.off(null, () => { });
            hubConnection.off(undefined, () => { });
        });
    });

    describe("stream", () => {
        it("sends an invocation", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let invokePromise = hubConnection.stream("testStream", "arg", 42);

            // Verify the message is sent
            expect(connection.sentData.length).toBe(1);
            expect(JSON.parse(connection.sentData[0])).toEqual({
                type: MessageType.StreamInvocation,
                invocationId: connection.lastInvocationId,
                target: "testStream",
                arguments: [
                    "arg",
                    42
                ]
            });

            // Close the connection
            hubConnection.stop();
        });

        it("completes with an error when an error is yielded", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let observer = new TestObserver();
            hubConnection.stream<any>("testMethod", "arg", 42)
                .subscribe(observer);

            connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, error: "foo" });

            let ex = await captureException(async () => await observer.completed);
            expect(ex.message).toEqual("Error: foo");
        });

        it("completes the observer when a completion is received", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let observer = new TestObserver();
            hubConnection.stream<any>("testMethod", "arg", 42)
                .subscribe(observer);

            connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId });

            expect(await observer.completed).toEqual([]);
        });

        it("completes pending streams when stopped", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let observer = new TestObserver();
            hubConnection.stream<any>("testMethod")
                .subscribe(observer);
            hubConnection.stop();

            let ex = await captureException(async () => await observer.completed);
            expect(ex.message).toEqual("Error: Invocation canceled due to connection being closed.");
        });

        it("completes pending streams when connection is lost", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let observer = new TestObserver();
            hubConnection.stream<any>("testMethod")
                .subscribe(observer);

            // Typically this would be called by the transport
            connection.onclose(new Error("Connection lost"));

            let ex = await captureException(async () => await observer.completed);
            expect(ex.message).toEqual("Error: Connection lost");
        });

        it("yields items as they arrive", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let observer = new TestObserver();
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
        });

        it("does not require error function registered", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let observer = hubConnection.stream("testMethod").subscribe({
                next: val => { }
            });

            // Typically this would be called by the transport
            // triggers observer.error()
            connection.onclose(new Error("Connection lost"));
        });

        it("does not require complete function registered", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let observer = hubConnection.stream("testMethod").subscribe({
                next: val => { }
            });

            // Send completion to trigger observer.complete()
            // Expectation is connection.receive will not to throw
            connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId });
        });
    });

    describe("onClose", () => {
        it("can have multiple callbacks", async () => {
            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection);
            let invocations = 0;
            hubConnection.onclose(e => invocations++);
            hubConnection.onclose(e => invocations++);
            // Typically this would be called by the transport
            connection.onclose();
            expect(invocations).toBe(2);
        });

        it("callbacks receive error", async () => {
            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection);
            let error: Error;
            hubConnection.onclose(e => error = e);

            // Typically this would be called by the transport
            connection.onclose(new Error("Test error."));
            expect(error.message).toBe("Test error.");
        });

        it("ignores null callbacks", async () => {
            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection);
            hubConnection.onclose(null);
            hubConnection.onclose(undefined);
            // Typically this would be called by the transport
            connection.onclose();
            // expect no errors
        });
    });

    describe("keepAlive", () => {
        it("can receive ping messages", async () => {
            // Receive the ping mid-invocation so we can see that the rest of the flow works fine

            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection);
            let invokePromise = hubConnection.invoke("testMethod", "arg", 42);

            connection.receive({ type: MessageType.Ping });
            connection.receive({ type: MessageType.Completion, invocationId: connection.lastInvocationId, result: "foo" });

            expect(await invokePromise).toBe("foo");
        });

        it("does not terminate if messages are received", async () => {
            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection, { serverTimeoutInMilliseconds: 100 });

            let p = new PromiseSource<Error>();
            hubConnection.onclose(error => p.resolve(error));

            await hubConnection.start();

            await connection.receive({ type: MessageType.Ping });
            await delay(50);
            await connection.receive({ type: MessageType.Ping });
            await delay(50);
            await connection.receive({ type: MessageType.Ping });
            await delay(50);
            await connection.receive({ type: MessageType.Ping });
            await delay(50);

            connection.stop();

            let error = await p.promise;

            expect(error).toBeUndefined();
        });

        it("terminates if no messages received within timeout interval", async () => {
            let connection = new TestConnection();
            let hubConnection = new HubConnection(connection, { serverTimeoutInMilliseconds: 100 });

            let p = new PromiseSource<Error>();
            hubConnection.onclose(error => p.resolve(error));

            await hubConnection.start();

            let error = await p.promise;

            expect(error).toEqual(new Error("Server timeout elapsed without receiving a message from the server."));
        });
    })
});

class TestConnection implements IConnection {
    readonly features: any = {};

    start(): Promise<void> {
        return Promise.resolve();
    };

    send(data: any): Promise<void> {
        let invocation = TextMessageFormat.parse(data)[0];
        let invocationId = JSON.parse(invocation).invocationId;
        if (invocationId) {
            this.lastInvocationId = invocationId;
        }
        if (this.sentData) {
            this.sentData.push(invocation);
        }
        else {
            this.sentData = [invocation];
        }
        return Promise.resolve();
    };

    stop(error?: Error): void {
        if (this.onclose) {
            this.onclose(error);
        }
    };

    receive(data: any): void {
        let payload = JSON.stringify(data);
        this.onreceive(TextMessageFormat.write(payload));
    }

    onreceive: DataReceived;
    onclose: ConnectionClosed;
    sentData: [any];
    lastInvocationId: string;
};

class TestObserver implements Observer<any>
{
    public itemsReceived: [any];
    private itemsSource: PromiseSource<[any]>;

    get completed(): Promise<[any]> {
        return this.itemsSource.promise;
    }

    constructor() {
        this.itemsReceived = <[any]>[];
        this.itemsSource = new PromiseSource<[any]>();
    }

    next(value: any) {
        this.itemsReceived.push(value);
    }

    error(err: any) {
        this.itemsSource.reject(new Error(err));
    }

    complete() {
        this.itemsSource.resolve(this.itemsReceived);
    }
};