import { TextMessageFormat, ServerSentEventsFormat } from "../Microsoft.AspNetCore.SignalR.Client.TS/Formatters"
import { Message, MessageType } from "../Microsoft.AspNetCore.SignalR.Client.TS/Message";

describe("Text Message Formatter", () => {
    it("should return empty array on empty input", () => {
        let messages = TextMessageFormat.parse("");
        expect(messages).toEqual([]);
    });
    ([
        ["T0:T:;", [new Message(MessageType.Text, "")]],
        ["T0:C:;", [new Message(MessageType.Close, "")]],
        ["T0:E:;", [new Message(MessageType.Error, "")]],
        ["T5:T:Hello;", [new Message(MessageType.Text, "Hello")]],
        ["T5:T:Hello;5:C:World;5:E:Error;", [new Message(MessageType.Text, "Hello"), new Message(MessageType.Close, "World"), new Message(MessageType.Error, "Error")]],
    ] as [[string, Message[]]]).forEach(([payload, expected_messages]) => {
        it(`should parse '${payload}' correctly`, () => {
            let messages = TextMessageFormat.parse(payload);
            expect(messages).toEqual(expected_messages);
        })
    });

    ([
        ["TABC", "Invalid length: 'ABC'"],
        ["X1:T:A", "Unsupported message format: 'X'"],
        ["T1:T:A;12ab34:", "Invalid length: '12ab34'"],
        ["T1:T:A;1:asdf:", "Unknown type value: 'asdf'"],
        ["T1:T:A;1::", "Message is incomplete"],
        ["T1:T:A;1:AB:", "Message is incomplete"],
        ["T1:T:A;5:T:A", "Message is incomplete"],
        ["T1:T:A;5:T:AB", "Message is incomplete"],
        ["T1:T:A;5:T:ABCDE", "Message is incomplete"],
        ["T1:T:A;5:X:ABCDE", "Message is incomplete"],
        ["T1:T:A;5:T:ABCDEF", "Message missing trailer character"],
    ] as [[string, string]]).forEach(([payload, expected_error]) => {
        it(`should fail to parse '${payload}'`, () => {
            expect(() => TextMessageFormat.parse(payload)).toThrow(expected_error);
        });
    });
});

describe("Server-Sent Events Formatter", () => {
    ([
        ["", "Message is missing header"],
        ["A", "Unknown type value: 'A'"],
        ["BOO\r\nBlarg", "Unknown type value: 'BOO'"]
    ] as [string, string][]).forEach(([payload, expected_error]) => {
        it(`should fail to parse '${payload}`, () => {
            expect(() => ServerSentEventsFormat.parse(payload)).toThrow(expected_error);
        });
    });

    ([
        ["T\r\nTest", new Message(MessageType.Text, "Test")],
        ["C\r\nTest", new Message(MessageType.Close, "Test")],
        ["E\r\nTest", new Message(MessageType.Error, "Test")],
        ["T", new Message(MessageType.Text, "")],
        ["T\r\n", new Message(MessageType.Text, "")],
        ["T\r\nFoo\r\nBar", new Message(MessageType.Text, "Foo\r\nBar")]
    ] as [string, Message][]).forEach(([payload, expected_message]) => {
        it(`should parse '${payload}' correctly`, () => {
            let message = ServerSentEventsFormat.parse(payload);
            expect(message).toEqual(expected_message);
        });
    });
});