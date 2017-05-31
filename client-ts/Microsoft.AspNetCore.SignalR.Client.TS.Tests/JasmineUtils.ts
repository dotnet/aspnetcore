export function asyncit(expectation: string, assertion?: () => Promise<any>, timeout?: number): void {
    let testFunction: (done: DoneFn) => void;
    if (assertion) {
        testFunction = done => {
            assertion()
                .then(() => done())
                .catch(() => fail());
        };
    }

    it(expectation, testFunction, timeout);
}

export async function captureException(fn: () => Promise<any>): Promise<Error> {
    try {
        await fn();
        return null;
    } catch (e) {
        return e;
    }
}