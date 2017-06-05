import { TextMessageFormat } from "../Microsoft.AspNetCore.SignalR.Client.TS/Formatters"
import { Message, MessageType } from "../Microsoft.AspNetCore.SignalR.Client.TS/Message";

describe("Text Message Formatter", () => {
    it("should return empty array on empty input", () => {
        let messages = TextMessageFormat.parse("");
        expect(messages).toEqual([]);
    });
    ([
        ["0:T:;", [new Message(MessageType.Text, "")]],
        ["5:T:Hello;", [new Message(MessageType.Text, "Hello")]],
    ] as [[string, Message[]]]).forEach(([payload, expected_messages]) => {
        it(`should parse '${payload}' correctly`, () => {
            let messages = TextMessageFormat.parse(payload);
            expect(messages).toEqual(expected_messages);
        })
    });

    ([
        ["ABC", new Error("Invalid length: 'ABC'")],
        ["1:X:A;", new Error("Unknown type value: 'X'")],
        ["1:T:A;12ab34:", new Error("Invalid length: '12ab34'")],
        ["1:T:A;1:asdf:", new Error("Unknown type value: 'asdf'")],
        ["1:T:A;1::", new Error("Message is incomplete")],
        ["1:T:A;1:AB:", new Error("Message is incomplete")],
        ["1:T:A;5:T:A", new Error("Message is incomplete")],
        ["1:T:A;5:T:AB", new Error("Message is incomplete")],
        ["1:T:A;5:T:ABCDE", new Error("Message is incomplete")],
        ["1:T:A;5:X:ABCDE", new Error("Message is incomplete")],
        ["1:T:A;5:T:ABCDEF", new Error("Message missing trailer character")],
    ] as [[string, Error]]).forEach(([payload, expected_error]) => {
        it(`should fail to parse '${payload}'`, () => {
            expect(() => TextMessageFormat.parse(payload)).toThrow(expected_error);
        });
    });
});
