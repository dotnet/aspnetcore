import { Page, Browser, launch } from 'puppeteer';
import { bindConsole, validateMessages } from '../testFuncs';

const serverPath = `https://localhost:6051`;

jest.setTimeout(30000);

let browser: Browser = null;
let page: Page = null;
let badMessages: string[] = [];

beforeAll(async () => {
    browser = await launch({ ignoreHTTPSErrors: true });
    page = await browser.newPage();
    badMessages = bindConsole(page);
});

afterEach(async () => {
    validateMessages(badMessages);
});


afterAll(async () => {
    if (browser) {
        await browser.close();
    }
});

describe('webapi pages are ok', () => {
    it('index page fails', async () => {
        let response = await page.goto(serverPath);

        expect(response.status()).toBe(404);

        //This puts a 404 message on the console, we need to remove it.
        badMessages.pop();
    });

    it('api/values works', async () => {
        let response = await page.goto(`${serverPath}/api/values`);

        expect(response.status()).toBe(200);
    });
});
