// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

export class HttpError extends Error {
    // tslint:disable-next-line:variable-name
    private __proto__: Error;
    public statusCode: number;
    constructor(errorMessage: string, statusCode: number) {
        const trueProto = new.target.prototype;
        super(errorMessage);
        this.statusCode = statusCode;

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}

export class TimeoutError extends Error {
    // tslint:disable-next-line:variable-name
    private __proto__: Error;
    constructor(errorMessage: string = "A timeout occurred.") {
        const trueProto = new.target.prototype;
        super(errorMessage);

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}
