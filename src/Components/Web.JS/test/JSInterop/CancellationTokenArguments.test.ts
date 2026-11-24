import { expect } from "@jest/globals";
import * as DotNet from "../../src/JSInterop/Microsoft.JSInterop";

interface Runtime {
    dispatcher: DotNet.ICallDispatcher;
    sentToDotNet: { callId: number; methodIdentifier: string }[];
    results: { callId: number; succeeded: boolean }[];
}

function createRuntime(): Runtime {
    const runtime: Runtime = { dispatcher: null!, sentToDotNet: [], results: [] };
    runtime.dispatcher = DotNet.attachDispatcher({
        beginInvokeDotNetFromJS: (callId, assemblyName, methodIdentifier) => {
            runtime.sentToDotNet.push({ callId, methodIdentifier });
        },
        sendByteArray: () => { },
        endInvokeJSFromDotNet: (callId, succeeded) => {
            runtime.results.push({ callId, succeeded });
        },
    });
    return runtime;
}

let received: AbortSignal[] = [];
(window as any).cancellationTokenTests = {
    receive: (...signals: AbortSignal[]) => {
        received.push(...signals);
    },
};

const token = (id: number | null, isCancellationRequested = false) => ({ __dotNetCancellationToken: id, isCancellationRequested });

async function call(runtime: Runtime, ...args: any[]): Promise<void> {
    await runtime.dispatcher.beginInvokeJSFromDotNet(1, "cancellationTokenTests.receive", JSON.stringify(args), DotNet.JSCallResultType.JSVoidResult, 0, DotNet.JSCallType.FunctionCall);
}

// Both messages are one-way, as .NET sends them with async handle zero.
async function send(runtime: Runtime, identifier: string, ...ids: number[]): Promise<void> {
    await runtime.dispatcher.beginInvokeJSFromDotNet(0, identifier, JSON.stringify(ids), DotNet.JSCallResultType.JSVoidResult, 0, DotNet.JSCallType.FunctionCall);
}

const cancel = (runtime: Runtime, ...ids: number[]) => send(runtime, "__CancellationTokenCanceled", ...ids);
const release = (runtime: Runtime, ...ids: number[]) => send(runtime, "__CancellationTokenReleased", ...ids);

describe("CancellationToken arguments", () => {
    beforeEach(() => {
        received = [];
    });

    test("A token id becomes a live signal that the cancel message aborts without calling .NET", async () => {
        const runtime = createRuntime();
        await call(runtime, token(5));

        expect(runtime.results).toEqual([{ callId: 1, succeeded: true }]);
        expect(received[0]).toBeInstanceOf(AbortSignal);
        expect(received[0].aborted).toBe(false);

        await cancel(runtime, 5);
        expect(received[0].aborted).toBe(true);
        expect(runtime.sentToDotNet).toEqual([]);
    });

    test("A canceled token becomes an aborted signal, and a token that cannot be canceled a signal that is not aborted", async () => {
        const runtime = createRuntime();
        await call(runtime, token(null, true), token(null));

        expect(received.map(signal => signal.aborted)).toEqual([true, false]);
        expect(runtime.sentToDotNet).toEqual([]);
    });

    test("One message aborts every id it carries", async () => {
        const runtime = createRuntime();
        await call(runtime, token(6), token(7), token(8));

        await cancel(runtime, 6, 8);
        expect(received.map(signal => signal.aborted)).toEqual([true, false, true]);
    });

    test("Repeated ids give the same signal", async () => {
        const runtime = createRuntime();
        await call(runtime, token(14), token(14));
        await call(runtime, token(14));

        // Compared as booleans: printing a failed match of two jsdom signals crashes jest.
        expect(received[1] === received[0]).toBe(true);
        expect(received[2] === received[0]).toBe(true);
        await cancel(runtime, 14);
        expect(received[0].aborted).toBe(true);
    });

    test("Release forgets the id without aborting, so a later cancel message does nothing", async () => {
        const runtime = createRuntime();
        await call(runtime, token(9), token(10));

        await release(runtime, 9, 10);
        await cancel(runtime, 9, 10);
        expect(received.map(signal => signal.aborted)).toEqual([false, false]);
    });

    test("Cancellation messages are handled by the dispatcher, not looked up on window", async () => {
        const runtime = createRuntime();
        const lookedUp: string[] = [];
        (window as any).__CancellationTokenCanceled = () => lookedUp.push("canceled");
        (window as any).__CancellationTokenReleased = () => lookedUp.push("released");
        try {
            await call(runtime, token(11));
            await cancel(runtime, 11, 404);
            await release(runtime, 404);
            expect(received[0].aborted).toBe(true);
            expect(lookedUp).toEqual([]);
        } finally {
            delete (window as any).__CancellationTokenCanceled;
            delete (window as any).__CancellationTokenReleased;
        }
    });

    test("Ids belong to one dispatcher", async () => {
        const first = createRuntime();
        const second = createRuntime();
        await call(first, token(12));
        await call(second, token(12));

        await cancel(first, 12);
        expect(received.map(signal => signal.aborted)).toEqual([true, false]);
    });

    test("A token in a .NET result becomes a signal that the cancel message aborts", async () => {
        const runtime = createRuntime();
        const result = runtime.dispatcher.invokeDotNetStaticMethodAsync<AbortSignal>("App", "GetToken");
        const { callId } = runtime.sentToDotNet[0];
        runtime.dispatcher.endInvokeDotNetFromJS(String(callId), true, JSON.stringify(token(13)));
        const signal = await result;

        expect(signal).toBeInstanceOf(AbortSignal);
        expect(signal.aborted).toBe(false);
        await cancel(runtime, 13);
        expect(signal.aborted).toBe(true);
    });
});
