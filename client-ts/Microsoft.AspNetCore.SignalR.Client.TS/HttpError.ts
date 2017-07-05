export class HttpError extends Error {
    statusCode: number;
    constructor(errorMessage: string, statusCode: number) {
        super(errorMessage);
        this.statusCode = statusCode;
    }
}