const puppeteer = require('puppeteer');
const os = require("os");
const hostname = os.hostname();

const corsServerPath = `http://${hostname}:9000`;

// e.g., npm test --debug
// In debug mode we show the editor, slow down operations, and increase the timeout for each test
let debug = process.env.npm_config_debug || false;
jest.setTimeout(debug ? 60000 : 30000);

let browser;
let error;

beforeAll(async () => {
    const options = debug ?
        { headless: false, slowMo: 100 } :
        { args: ['--no-sandbox'] };
    const label = 'Launch puppeteer ';

    console.log('Begin launching puppeteer');
    console.time(label);

    try {
        browser = await puppeteer.launch(options);
    } catch (ex) {
        error = ex;
    }
    console.timeEnd(label);
});

afterAll(async () => {
    if (browser) {
        await browser.close();
    }
});

describe('Browser is initialized', () => {
    // Workaround for https://github.com/jasmine/jasmine/issues/1533.
    // Jasmine will not report errors from beforeAll and instead fail all the tests that
    // expect the browser to be available. This test allows us to ensure the setup was successful
    // and if unsuccessful report the error
    test('no errors on launch', () => {
        expect(error).toBeUndefined();
        expect(browser).toBeDefined();
    });
});

describe('CORS allowed origin tests ', () => {
    const testPagePath = `http://${hostname}:9001/`;
    let page;

    beforeAll(async () => {
        page = await browser.newPage();
        await page.goto(testPagePath);
    });

    test('allows simple GET requests', async () => {
        const result = await page.evaluate(async (corsServerPath) => {
            const url = `${corsServerPath}/allow-origin`;
            const options = { method: 'GET', mode: 'cors' };

            const response = await fetch(url, options);
            return response.status;
        }, corsServerPath);

        expect(result).toBe(200);
    });

    test('allows simple PUT requests when any method is allowed', async () => {
        const result = await page.evaluate(async (corsServerPath) => {
            const url = `${corsServerPath}/allow-origin`;
            const options = { method: 'PUT', mode: 'cors' };

            const response = await fetch(url, options);
            return response.status;
        }, corsServerPath);

        expect(result).toBe(200);
    });

    // This one is weird - although the server performs a preflight request and receives a Access-Control-Allow-Methods: PUT,
    // the browser happily ignores the disallowed POST method.
    test('allows POST requests when not explicitly allowed', async () => {
        const result = await page.evaluate(async (corsServerPath) => {
            const url = `${corsServerPath}/allow-header-method`;
            const options = {
                method: 'POST',
                mode: 'cors',
                body: JSON.stringify({ hello: 'world' }),
                headers: new Headers({ "X-Test": "value", 'Content-Type': 'application/json' })
            };

            const response = await fetch(url, options);
            return response.status;
        }, corsServerPath);

        expect(result).toBe(200);
    });

    test('allows header to be sent when allowed', async () => {
        const result = await page.evaluate(async (corsServerPath) => {
            const url = `${corsServerPath}/allow-header-method`;
            const options = { method: 'PUT', mode: 'cors', headers: new Headers({ "X-Test": "value", 'Content-Type': 'application/json' }) };

            const response = await fetch(url, options);
            return response.status;
        }, corsServerPath);

        expect(result).toBe(200);
    });

    test('does not allow disallowed HTTP Methods', async () => {
        expect.assertions(1);
        try {
            await page.evaluate(async (corsServerPath) => {
                const url = `${corsServerPath}/allow-header-method`;
                const options = { method: 'DELETE', mode: 'cors', headers: new Headers({ "X-Test": "value", 'Content-Type': 'application/json' }) };

                return await fetch(url, options);
            }, corsServerPath);
        } catch (e) {
            expect(e).toBeDefined();
        }
    });

    test('does not allow disallowed header', async () => {
        expect.assertions(1);
        try {
            await page.evaluate(async (corsServerPath) => {
                const url = `${corsServerPath}/allow-header-method`;
                const options = { method: 'PUT', mode: 'cors', headers: new Headers({ "X-Not-Test": "value", 'Content-Type': 'application/json' }) };

                return await fetch(url, options);
            }, corsServerPath);
        } catch (e) {
            expect(e).toBeDefined();
        }
    });

    test('does not allow fetch with credentials in non-Preflighted request', async () => {
        expect.assertions(1);
        try {
            await page.evaluate(async (corsServerPath) => {
                const url = `${corsServerPath}/allow-origin`;
                const options = { method: 'POST', mode: 'cors', credentials: 'include' };

                return await fetch(url, options);
            }, corsServerPath);
        } catch (e) {
            expect(e).toBeDefined();
        }
    });

    test('does not allow fetch with credentials in Preflighted request', async () => {
        expect.assertions(1);
        try {
            await page.evaluate(async (corsServerPath) => {
                const url = `${corsServerPath}/allow-origin`;
                const options = { method: 'PUT', mode: 'cors', credentials: 'include' };

                return await fetch(url, options);
            }, corsServerPath);
        } catch (e) {
            expect(e).toBeDefined();
        }
    });

    test('allows request with credentials', async () => {
        const result = await page.evaluate(async (corsServerPath) => {
            const url = `${corsServerPath}/allow-credentials`;
            const options = { method: 'GET', mode: 'cors', credentials: 'include' };

            const response = await fetch(url, options);
            return response.status;
        }, corsServerPath);

        expect(result).toBe(200);
    });

    test('allows Preflighted request with credentials', async () => {
        const result = await page.evaluate(async (corsServerPath) => {
            const url = `${corsServerPath}/allow-credentials`;
            const options = {
                method: 'PUT', mode: 'cors', credentials: 'include', headers: new Headers({
                    'X-Custom-Header': 'X-Custom-Value'
                })
            };

            const response = await fetch(url, options);
            return response.status;
        }, corsServerPath);

        expect(result).toBe(200);
    });

    test('disallows accessing header when not included in exposed-header', async () => {
        const result = await page.evaluate(async (corsServerPath) => {
            const url = `${corsServerPath}/exposed-header`;
            const options = { method: 'GET', mode: 'cors' };

            const response = await fetch(url, options);
            try {
                return response.headers.get('x-disallowedheader');
            } catch (e) {
                return null;
            }
        }, corsServerPath);

        expect(result).toBeNull();
    });

    test('allows accessing header when included in exposed-header', async () => {
        const result = await page.evaluate(async (corsServerPath) => {
            const url = `${corsServerPath}/exposed-header`;
            const options = { method: 'GET', mode: 'cors' };

            const response = await fetch(url, options);
            try {
                return response.headers.get('x-allowedheader');
            } catch (e) {
                return e;
            }
        }, corsServerPath);

        expect(result).toBe("Test-Value");
    });
});

describe('CORS disallowed origin tests ', () => {
    const testPagePath = `http://${hostname}:9002/`;
    let page;

    beforeAll(async () => {
        page = await browser.newPage();
        await page.goto(testPagePath);
    });

    test('allow opaque requests without CORS', async () => {
        const result = await page.evaluate(async (corsServerPath) => {
            const url = `${corsServerPath}/allow-origin`;
            const options = { method: 'GET', mode: 'no-cors' };

            // The request will succeed, but we get an opaque filtered response (https://fetch.spec.whatwg.org/#concept-filtered-response).
            const response = await fetch(url, options);
            return response.type;
        }, corsServerPath);

        expect(result).toBe("opaque");
    });

    test('does not allow requests when origin is disallowed', async () => {
        expect.assertions(1);
        try {
            await page.evaluate(async (corsServerPath) => {
                const url = `${corsServerPath}/allow-origin`;
                const options = { method: 'GET', mode: 'cors' };

                return await fetch(url, options);
            }, corsServerPath);
        } catch (e) {
            expect(e).toBeDefined();
        }
    });

    test('does not allow preflight requests when origin is disallowed', async () => {
        expect.assertions(1);
        try {
            await page.evaluate(async (corsServerPath) => {
                const url = `${corsServerPath}/allow-origin`;
                const options = { method: 'PUT', mode: 'cors' };

                return await fetch(url, options);
            }, corsServerPath);
        } catch (e) {
            expect(e).toBeDefined();
        }
    });

    test('allow requests to any origin endpoint', async () => {
        const result = await page.evaluate(async (corsServerPath) => {
            const url = `${corsServerPath}/allow-all`;
            const options = { method: 'PUT', mode: 'cors' };

            const response = await fetch(url, options);
            return response.status;
        }, corsServerPath);

        expect(result).toBe(200);
    });

    test('does not allow requests to any origin endpoint with credentials', async () => {
        expect.assertions(1);

        try {
            await page.evaluate(async (corsServerPath) => {
                const url = `${corsServerPath}/allow-all`;
                const options = { method: 'PUT', mode: 'cors', headers: new Headers({ "X-Test": "value", 'Content-Type': 'application/json' }), credentials: 'include' };

                const response = await fetch(url, options);
                return response.status;
            }, corsServerPath);
        } catch (e) {
            expect(e).toBeDefined();
        }
    });
});
