import { IConnection } from "../Microsoft.AspNetCore.SignalR.Client.TS/IConnection"
import { HubConnection } from "../Microsoft.AspNetCore.SignalR.Client.TS/HubConnection"
import { DataReceived, ConnectionClosed } from "../Microsoft.AspNetCore.SignalR.Client.TS/Common"
import { TransportType, ITransport } from "../Microsoft.AspNetCore.SignalR.Client.TS/Transports"

describe("HubConnection", () => {
    it("completes pending invocations when stopped", async done => {
        let connection: IConnection = {
            start(transportType: TransportType | ITransport): Promise<void> {
                return Promise.resolve();
            },

            send(data: any): Promise<void> {
                return Promise.resolve();
            },

            stop(): void {
                if (this.onClosed) {
                    this.onClosed();
                }
            },

            onDataReceived: null,
            onClosed: null
        };

        let hubConnection = new HubConnection(connection);
        var invokePromise = hubConnection.invoke("testMethod");
        hubConnection.stop();

        try {
            await invokePromise;
            fail();
        }
        catch (e) {
            expect(e.message).toBe("Invocation cancelled due to connection being closed.");
        }
        done();
    });

    it("completes pending invocations when connection is lost", async done => {
        let connection: IConnection = {
            start(transportType: TransportType | ITransport): Promise<void> {
                return Promise.resolve();
            },

            send(data: any): Promise<void> {
                return Promise.resolve();
            },

            stop(): void {
                if (this.onClosed) {
                    this.onClosed();
                }
            },

            onDataReceived: null,
            onClosed: null
        };

        let hubConnection = new HubConnection(connection);
        var invokePromise = hubConnection.invoke("testMethod");
        // Typically this would be called by the transport
        connection.onClosed(new Error("Connection lost"));

        try {
            await invokePromise;
            fail();
        }
        catch (e) {
            expect(e.message).toBe("Connection lost");
        }
        done();
    });

    it("sends invocations as nonblocking", async done => {
        let dataSent: string;
        let connection: IConnection = {
            start(transportType: TransportType): Promise<void> {
                return Promise.resolve();
            },

            send(data: any): Promise<void> {
                dataSent = data;
                return Promise.resolve();
            },

            stop(): void {
                if (this.onClosed) {
                    this.onClosed();
                }
            },

            onDataReceived: null,
            onClosed: null
        };

        let hubConnection = new HubConnection(connection);
        let invokePromise = hubConnection.invoke("testMethod");

        expect(JSON.parse(dataSent).nonblocking).toBe(false);

        // will clean pending promises
        connection.onClosed();

        try {
            await invokePromise;
            fail(); // exception is expected because the call has not completed
        }
        catch (e) {
        }
        done();
    });

    it("rejects streaming responses", async done => {
        let connection: IConnection = {
            start(transportType: TransportType): Promise<void> {
                return Promise.resolve();
            },

            send(data: any): Promise<void> {
                return Promise.resolve();
            },

            stop(): void {
                if (this.onClosed) {
                    this.onClosed();
                }
            },

            onDataReceived: null,
            onClosed: null
        };

        let hubConnection = new HubConnection(connection);
        let invokePromise = hubConnection.invoke("testMethod");

        connection.onDataReceived("{ \"type\": 2, \"invocationId\": \"0\", \"result\": null }");
        connection.onClosed();

        try {
            await invokePromise;
            fail();
        }
        catch (e) {
            expect(e.message).toBe("Streaming is not supported.");
        }

        done();
    });
});