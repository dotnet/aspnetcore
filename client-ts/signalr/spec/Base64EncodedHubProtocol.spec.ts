// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { Base64EncodedHubProtocol } from "../src/Base64EncodedHubProtocol";
import { HubMessage, IHubProtocol, ProtocolType } from "../src/IHubProtocol";

class FakeHubProtocol implements IHubProtocol {
    public name: "fakehubprotocol";
    public type: ProtocolType;

    public parseMessages(input: any): HubMessage[] {
        let s = "";

        new Uint8Array(input).forEach((item: any) => {
            s += String.fromCharCode(item);
        });

        return JSON.parse(s);
    }

    public writeMessage(message: HubMessage): any {
        const s = JSON.stringify(message);
        const payload = new Uint8Array(s.length);
        for (let i = 0; i < payload.length; i++) {
            payload[i] = s.charCodeAt(i);
        }
        return payload;
    }
}

describe("Base64EncodedHubProtocol", () => {
    ([
        ["ABC", new Error("Invalid payload.")],
        ["3:ABC", new Error("Invalid payload.")],
        [":;", new Error("Invalid length: ''")],
        ["1.0:A;", new Error("Invalid length: '1.0'")],
        ["2:A;", new Error("Invalid message size.")],
        ["2:ABC;", new Error("Invalid message size.")],
    ] as Array<[string, Error]>).forEach(([payload, expectedError]) => {
        it(`should fail to parse '${payload}'`, () => {
            expect(() => new Base64EncodedHubProtocol(new FakeHubProtocol()).parseMessages(payload)).toThrow(expectedError);
        });
    });

    ([
        ["2:{};", {}],
    ] as [[string, any]]).forEach(([payload, message]) => {
        it(`should be able to parse '${payload}'`, () => {

            const globalAny: any = global;
            globalAny.atob = (input: any) => input;

            const result = new Base64EncodedHubProtocol(new FakeHubProtocol()).parseMessages(payload);
            expect(result).toEqual(message);

            delete globalAny.atob;
        });
    });

    ([
        [{}, "2:{};"],
    ] as Array<[any, string]>).forEach(([message, payload]) => {
        it(`should be able to write '${JSON.stringify(message)}'`, () => {

            const globalAny: any = global;
            globalAny.btoa = (input: any) => input;

            const result = new Base64EncodedHubProtocol(new FakeHubProtocol()).writeMessage(message);
            expect(result).toEqual(payload);

            delete globalAny.btoa;
        });
    });
});
