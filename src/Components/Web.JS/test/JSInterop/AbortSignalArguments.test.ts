import { expect, jest } from "@jest/globals";
import { collect } from "./FakeFinalizationRegistry";
import * as DotNet from "../../src/JSInterop/Microsoft.JSInterop";

interface SentCall {
    callId: number;
    methodIdentifier: string;
    argsJson: string;
}

let sent: SentCall[] = [];
const dotNetCallDispatcher: DotNet.DotNetCallDispatcher = {
    beginInvokeDotNetFromJS: function (callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string): void {
        sent.push({ callId, methodIdentifier, argsJson });
    },
    sendByteArray: function (id: number, data: Uint8Array): void { },
    endInvokeJSFromDotNet: function (callId: number, succeeded: boolean, resultOrError: any): void { },
};
const dispatcher: DotNet.ICallDispatcher = DotNet.attachDispatcher(dotNetCallDispatcher);

const flush = () => new Promise(resolve => setTimeout(resolve, 0));
const messages = (methodIdentifier: string) => sent.filter(call => call.methodIdentifier === methodIdentifier);

function callWith(signal: AbortSignal): SentCall {
    dispatcher.invokeDotNetStaticMethodAsync("App", "Receive", signal).catch(() => { });
    return sent[sent.length - 1];
}

function complete(call: SentCall, success = true): void {
    dispatcher.endInvokeDotNetFromJS(String(call.callId), success, success ? "null" : "failure");
}

describe("AbortSignal arguments", () => {
    beforeEach(() => {
        sent = [];
    });

    test("Every signal is written with its id and state, and an aborted signal keeps its id", () => {
        const controller = new AbortController();
        const first = JSON.parse(callWith(controller.signal).argsJson)[0];
        controller.abort();
        const second = JSON.parse(callWith(controller.signal).argsJson)[0];

        expect(first.__abortSignal).toBeGreaterThan(0);
        expect(first).toEqual({ __abortSignal: first.__abortSignal, aborted: false });
        expect(second).toEqual({ __abortSignal: first.__abortSignal, aborted: true });
    });

    test("A signal first seen already aborted gets an id too", () => {
        const controller = new AbortController();
        controller.abort();
        const written = JSON.parse(callWith(controller.signal).argsJson)[0];

        expect(written.__abortSignal).toBeGreaterThan(0);
        expect(written).toEqual({ __abortSignal: written.__abortSignal, aborted: true });
    });

    test("Release waits until every call that carried the signal has completed", async () => {
        const signal = new AbortController().signal;
        const first = callWith(signal);
        const second = callWith(signal);
        const id = JSON.parse(first.argsJson)[0].__abortSignal;

        collect(signal);
        await flush();
        expect(messages("__AbortSignalReleased")).toHaveLength(0);

        complete(first);
        await flush();
        expect(messages("__AbortSignalReleased")).toHaveLength(0);

        complete(second, false);
        await flush();
        expect(messages("__AbortSignalReleased")).toEqual([{ callId: 0, methodIdentifier: "__AbortSignalReleased", argsJson: `[${id}]` }]);
    });

    test("Abort is an acknowledged call, and release waits for the acknowledgment", async () => {
        const controller = new AbortController();
        const call = callWith(controller.signal);
        const id = JSON.parse(call.argsJson)[0].__abortSignal;
        complete(call);

        controller.abort();
        await flush();
        const aborts = messages("__AbortSignalAborted");
        expect(aborts).toHaveLength(1);
        expect(aborts[0].callId).toBeGreaterThan(0);
        expect(aborts[0].argsJson).toBe(`[${id}]`);

        collect(controller.signal);
        await flush();
        expect(messages("__AbortSignalReleased")).toHaveLength(0);

        complete(aborts[0]);
        await flush();
        expect(messages("__AbortSignalReleased")).toHaveLength(1);
    });

    test("A failed abort acknowledgment is logged and still lets release go", async () => {
        const error = jest.spyOn(console, "error").mockImplementation(() => { });
        try {
            const controller = new AbortController();
            complete(callWith(controller.signal));
            controller.abort();
            await flush();
            collect(controller.signal);

            complete(messages("__AbortSignalAborted")[0], false);
            await flush();
            expect(error).toHaveBeenCalled();
            expect(messages("__AbortSignalReleased")).toHaveLength(1);
        } finally {
            error.mockRestore();
        }
    });

    test.each([["call first"], ["abort first"]])("Release waits for both the call reply and the abort reply (%s)", async (order) => {
        const controller = new AbortController();
        const call = callWith(controller.signal);
        controller.abort();
        await flush();
        const abort = messages("__AbortSignalAborted")[0];
        collect(controller.signal);

        complete(order === "call first" ? call : abort);
        await flush();
        expect(messages("__AbortSignalReleased")).toHaveLength(0);

        complete(order === "call first" ? abort : call);
        await flush();
        expect(messages("__AbortSignalReleased")).toHaveLength(1);
    });

    test("A reply that cannot be revived still ends the call's hold on the signal", async () => {
        const signal = new AbortController().signal;
        const call = callWith(signal);

        expect(() => dispatcher.endInvokeDotNetFromJS(String(call.callId), true, "not json")).toThrow();
        collect(signal);
        await flush();
        expect(messages("__AbortSignalReleased")).toHaveLength(1);
    });

    test("Nested serialization does not lose the outer call's dispatcher", () => {
        const signal = new AbortController().signal;
        const nested = {
            get value() {
                dispatcher.invokeDotNetStaticMethodAsync("App", "Nested", 1).catch(() => { });
                return 1;
            },
        };
        dispatcher.invokeDotNetStaticMethodAsync("App", "Outer", nested, signal).catch(() => { });

        const outer = messages("Outer");
        expect(outer).toHaveLength(1);
        expect(JSON.parse(outer[0].argsJson)[1].__abortSignal).toBeGreaterThan(0);
    });

    test("A call that completes before beginInvokeDotNetFromJS returns is counted", async () => {
        const inlineSent: SentCall[] = [];
        const inlineDispatcher: DotNet.ICallDispatcher = DotNet.attachDispatcher({
            beginInvokeDotNetFromJS: function (callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string): void {
                inlineSent.push({ callId, methodIdentifier, argsJson });
                if (methodIdentifier === "Receive") {
                    inlineDispatcher.endInvokeDotNetFromJS(String(callId), true, "null");
                }
            },
            sendByteArray: function (id: number, data: Uint8Array): void { },
            endInvokeJSFromDotNet: function (callId: number, succeeded: boolean, resultOrError: any): void { },
        });
        const signal = new AbortController().signal;
        await inlineDispatcher.invokeDotNetStaticMethodAsync("App", "Receive", signal);

        collect(signal);
        await flush();
        expect(inlineSent.filter(call => call.methodIdentifier === "__AbortSignalReleased")).toHaveLength(1);
    });

    test("A signal in a value returned to .NET is rejected", () => {
        const signal = new AbortController().signal;
        const objectId = DotNet.createJSObjectReference({ getSignal: () => signal })["__jsObjectId"];

        expect(() => dispatcher.invokeJSFromDotNet(
            "getSignal",
            "[]",
            DotNet.JSCallResultType.Default,
            objectId,
            DotNet.JSCallType.FunctionCall
        )).toThrow();
    });
});
