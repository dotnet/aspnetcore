import { IConnection } from "../Microsoft.AspNetCore.SignalR.Client.TS/IConnection"
import { HubConnection } from "../Microsoft.AspNetCore.SignalR.Client.TS/HubConnection"
import { DataReceived, ConnectionClosed } from "../Microsoft.AspNetCore.SignalR.Client.TS/Common"
import { TransportType, ITransport } from "../Microsoft.AspNetCore.SignalR.Client.TS/Transports"
import { Observer } from "../Microsoft.AspNetCore.SignalR.Client.TS/Observable"

import { asyncit as it, captureException } from './JasmineUtils';

describe("HubConnection", () => {
    describe("invoke", () => {
        it("sends an invocation", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            var invokePromise = hubConnection.invoke("testMethod", "arg", 42)
                .catch((_) => { }); // Suppress exception and unhandled promise rejection warning.

            // Verify the message is sent
            expect(connection.sentData.length).toBe(1);
            expect(JSON.parse(connection.sentData[0])).toEqual({
                type: 1,
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
            var invokePromise = hubConnection.invoke("testMethod", "arg", 42);

            connection.receive({ type: 3, invocationId: connection.lastInvocationId, error: "foo" });

            let ex = await captureException(async () => invokePromise);
            expect(ex.message).toBe("foo");
        });

        it("resolves the promise when a result is received", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            var invokePromise = hubConnection.invoke("testMethod", "arg", 42);

            connection.receive({ type: 3, invocationId: connection.lastInvocationId, result: "foo" });

            expect(await invokePromise).toBe("foo");
        });

        it("completes pending invocations when stopped", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            var invokePromise = hubConnection.invoke("testMethod");
            hubConnection.stop();

            let ex = await captureException(async () => await invokePromise);
            expect(ex.message).toBe("Invocation cancelled due to connection being closed.");
        });

        it("completes pending invocations when connection is lost", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            var invokePromise = hubConnection.invoke("testMethod");
            // Typically this would be called by the transport
            connection.onClosed(new Error("Connection lost"));

            let ex = await captureException(async () => await invokePromise);
            expect(ex.message).toBe("Connection lost");
        });

        it("rejects streaming responses made using 'invoke'", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let invokePromise = hubConnection.invoke("testMethod");

            connection.receive({ type: 2, invocationId: connection.lastInvocationId, item: null });
            connection.onClosed();

            let ex = await captureException(async () => await invokePromise);
            expect(ex.message).toBe("Streaming methods must be invoked using HubConnection.stream");
        });
    });

    describe("stream", () => {
        it("sends an invocation", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            var invokePromise = hubConnection.stream("testStream", "arg", 42);

            // Verify the message is sent
            expect(connection.sentData.length).toBe(1);
            expect(JSON.parse(connection.sentData[0])).toEqual({
                type: 1,
                invocationId: connection.lastInvocationId,
                target: "testStream",
                nonblocking: false,
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

            connection.receive({ type: 3, invocationId: connection.lastInvocationId, error: "foo" });

            let ex = await captureException(async () => await observer.completed);
            expect(ex.message).toEqual("Error: foo");

        });

        it("completes the observer when a completion is received", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let observer = new TestObserver();
            hubConnection.stream<any>("testMethod", "arg", 42)
                .subscribe(observer);

            connection.receive({ type: 3, invocationId: connection.lastInvocationId });

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
            expect(ex.message).toEqual("Error: Invocation cancelled due to connection being closed.");
        });

        it("completes pending streams when connection is lost", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let observer = new TestObserver();
            hubConnection.stream<any>("testMethod")
                .subscribe(observer);

            // Typically this would be called by the transport
            connection.onClosed(new Error("Connection lost"));

            let ex = await captureException(async () => await observer.completed);
            expect(ex.message).toEqual("Error: Connection lost");
        });

        it("rejects completion responses", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let observer = new TestObserver();
            hubConnection.stream<any>("testMethod")
                .subscribe(observer);

            connection.receive({ type: 3, invocationId: connection.lastInvocationId, result: "foo" });

            let ex = await captureException(async () => await observer.completed);
            expect(ex.message).toEqual("Error: Server provided a result in a completion response to a streamed invocation.");
        });

        it("yields items as they arrive", async () => {
            let connection = new TestConnection();

            let hubConnection = new HubConnection(connection);
            let observer = new TestObserver();
            hubConnection.stream<any>("testMethod")
                .subscribe(observer);

            connection.receive({ type: 2, invocationId: connection.lastInvocationId, item: 1 });
            expect(observer.itemsReceived).toEqual([1]);

            connection.receive({ type: 2, invocationId: connection.lastInvocationId, item: 2 });
            expect(observer.itemsReceived).toEqual([1, 2]);

            connection.receive({ type: 2, invocationId: connection.lastInvocationId, item: 3 });
            expect(observer.itemsReceived).toEqual([1, 2, 3]);

            connection.receive({ type: 3, invocationId: connection.lastInvocationId });
            expect(await observer.completed).toEqual([1, 2, 3]);
        });
    });
});

class TestConnection implements IConnection {
    start(transportType: TransportType | ITransport): Promise<void> {
        return Promise.resolve();
    };

    send(data: any): Promise<void> {
        this.lastInvocationId = JSON.parse(data).invocationId;
        if (this.sentData) {
            this.sentData.push(data);
        }
        else {
            this.sentData = [data];
        }
        return Promise.resolve();
    };

    stop(): void {
        if (this.onClosed) {
            this.onClosed();
        }
    };

    receive(data: any): void {
        this.onDataReceived(JSON.stringify(data));
    }

    onDataReceived: DataReceived;
    onClosed: ConnectionClosed;
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

class PromiseSource<T> {
    public promise: Promise<T>

    private resolver: (value?: T | PromiseLike<T>) => void;
    private rejecter: (reason?: any) => void;

    constructor() {
        this.promise = new Promise<T>((resolve, reject) => {
            this.resolver = resolve;
            this.rejecter = reject;
        });
    }

    resolve(value?: T | PromiseLike<T>) {
        this.resolver(value);
    }

    reject(reason?: any) {
        this.rejecter(reason);
    }
}