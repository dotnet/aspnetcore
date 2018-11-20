import { Page, Browser, launch } from 'puppeteer';
import { bindConsole, validateMessages } from '../testFuncs';

const serverPath = `https://localhost:5101`;

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

describe('razor pages are ok', () => {
    it('index page works', async () => {
        await page.goto(serverPath);
        await page.waitFor('h1');

        let heading = await page.$eval('h1', heading => heading.textContent);
        expect(heading).toBe('Welcome');
        validateMessages(badMessages);
    });

    it('privacy page works', async () => {
        await page.goto(`${serverPath}/Privacy`);
        await page.waitFor('h1');

        let heading = await page.$eval('h1', heading => heading.textContent);
        expect(heading).toBe('Privacy Policy');
        validateMessages(badMessages);
    });
});
