import { Page, Browser, launch } from 'puppeteer';
import { bindConsole, clickByText, validateMessages } from '../testFuncs';

const serverPath = `https://localhost:6011`;

jest.setTimeout(120000);

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

afterEach(async () => {
    // TODO: We have WS exceptions that keep us from validating this.
    //validateMessages(badMessages);
});

describe('angular pages are ok', () => {
    it('all pages work', async () => {
        await page.goto(serverPath, {
            waitUntil: "networkidle0",
            timeout: 120000
        });
        await page.waitFor('ul');

        let heading = await page.$eval('h1', heading => heading.textContent);
        expect(heading).toBe('Hello, world!');

        await clickByText(page, 'Counter');
        await page.waitFor('p');
        expect(page.url()).toBe(`${serverPath}/counter`);

        heading = await page.$eval('h1', heading => heading.textContent);
        expect(heading).toBe('Counter');

        let strong = await page.$eval('strong', strong => strong.textContent);
        expect(strong).toBe('0');

        await clickByText(page, 'Increment', 'button');
        strong = await page.$eval('strong', strong => strong.textContent);
        expect(strong).toBe('1');

        await clickByText(page, 'Fetch data');
        await page.waitFor('table');
        expect(page.url()).toBe(`${serverPath}/fetch-data`);

        heading = await page.$eval('h1', heading => heading.textContent);
        expect(heading).toBe('Weather forecast');
        await page.waitFor(200);
        let trs = await page.$x('//tbody//tr');

        expect(trs.length).toBe(5);
    });
});
