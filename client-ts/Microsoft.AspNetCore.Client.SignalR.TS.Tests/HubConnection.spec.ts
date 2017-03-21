import { IConnection } from "../Microsoft.AspNetCore.SignalR.Client.TS/IConnection"
import { HubConnection } from "../Microsoft.AspNetCore.SignalR.Client.TS/HubConnection"
import { DataReceived, ConnectionClosed } from "../Microsoft.AspNetCore.SignalR.Client.TS/Common"

describe("HubConnection", () => {
    it("completes pending invocations when stopped", async (done) => {
        let connection: IConnection = {
            start(transportName: string): Promise<void> {
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
        invokePromise
            .then(() => {
                fail();
                done();
            })
            .catch((error: Error) => {
                expect(error.message).toBe("Invocation cancelled due to connection being closed.");
                done();
            });
    });

    it("completes pending invocations when connection is lost", async (done) => {
        let connection: IConnection = {
            start(transportName: string): Promise<void> {
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
        invokePromise
            .then(() => {
                fail();
                done();
            })
            .catch((error: Error) => {
                expect(error.message).toBe("Connection lost");
                done();
            });

        // Typically this would be called by the transport
        connection.onClosed(new Error("Connection lost"));
    });
});