import { TextMessageFormat } from "../Microsoft.AspNetCore.SignalR.Client.TS/Formatters"

describe("Text Message Formatter", () => {
    it("should return empty array on empty input", () => {
        let messages = TextMessageFormat.parse("");
        expect(messages).toEqual([]);
    });
    ([
        ["0:;", [""]],
        ["5:Hello;", ["Hello"]],
    ] as [[string, string[]]]).forEach(([payload, expected_messages]) => {
        it(`should parse '${payload}' correctly`, () => {
            let messages = TextMessageFormat.parse(payload);
            expect(messages).toEqual(expected_messages);
        })
    });

    ([
        ["ABC", new Error("Invalid length: 'ABC'")],
        ["1:A;12ab34:", new Error("Invalid length: '12ab34'")],
        ["1:A;1:", new Error("Message is incomplete")],
        ["1:A;1:AB:", new Error("Message missing trailer character")],
        ["1:A;5:A", new Error("Message is incomplete")],
        ["1:A;5:AB", new Error("Message is incomplete")],
        ["1:A;5:ABCDE", new Error("Message is incomplete")],
        ["1:A;5:ABCDEF", new Error("Message missing trailer character")],
    ] as [[string, Error]]).forEach(([payload, expected_error]) => {
        it(`should fail to parse '${payload}'`, () => {
            expect(() => TextMessageFormat.parse(payload)).toThrow(expected_error);
        });
    });
});
