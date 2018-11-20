import { Page, Browser, launch } from 'puppeteer';
import { bindConsole, validateMessages } from '../testFuncs';

const serverPath = `https://localhost:6041`;

jest.setTimeout(30000);

let browser: Browser = null;
let page: Page = null;
let badMessages: string[] = [];

beforeAll(async () => {
    browser = await launch({ ignoreHTTPSErrors: true });
    page = await browser.newPage();
    badMessages = bindConsole(page);
});

afterAll(async () => {
    if (browser) {
        await browser.close();
    }
});

describe('web pages are ok', () => {
    it('index page works', async () => {
        let response = await page.goto(serverPath);

        expect(response.status()).toBe(200);
        validateMessages(badMessages);
    });
});
