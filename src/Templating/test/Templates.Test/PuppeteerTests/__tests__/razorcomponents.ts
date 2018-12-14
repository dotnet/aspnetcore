import { Page, Browser, launch } from 'puppeteer';
import { bindConsole, clickByText, validateMessages } from '../testFuncs';

const serverPath = `https://localhost:8001`;

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

describe('razorcomponents are ok', () => {
    it('navigation works', async () => {
        await page.goto(serverPath, {
            waitUntil: "networkidle0",
            timeout: 120000
        });
        await page.waitFor('ul');

        let heading = page.$eval('h1', heading => heading.textContent);
        expect(heading).toBe('Hello, world!');

        await clickByText(page, 'Counter');
        await page.waitFor('h1');
        heading = await page.$eval('h1', heading => heading.textContent);
        expect(heading).toBe('Counter');

        // Counter was initialized to 0
        let counterDisplay = await page.$eval('h1 + p', heading => heading.textContent);
        expect(counterDisplay).toBe('Current count: 0');

        await clickByText(page, 'Click me', 'button');
        counterDisplay = await page.$eval('h1 + p', heading => heading.textContent);
        expect(counterDisplay).toBe('Current count: 1');

        await clickByText(page, 'Fetch data');
        await page.waitFor("h1");
        await page.waitFor("table>tbody>tr");
        let rowCount = await page.$eval("tbody tr").count();
        expect(rowCount).toBe(5);

        validateMessages(badMessages);
    });
});
