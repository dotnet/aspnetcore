import * as os from "os";

import { By, logging, WebDriver, WebElement } from "selenium-webdriver";

import * as _debug from "debug";
const debug = _debug("webdriver-tap-runner:utils");

export function delay(ms: number): Promise<void> {
    return new Promise<void>((resolve, reject) => {
        setTimeout(resolve, ms);
    });
}

export async function waitForElement(driver: WebDriver, id: string): Promise<WebElement> {
    debug(`Waiting for '${id}' element`);
    for (let attempts = 0; attempts < 2; attempts += 1) {
        const elements = await driver.findElements(By.id(id));
        if (elements && elements.length > 0) {
            debug(`Found '${id}' element`);
            return elements[0];
        }

        debug(`Waiting 5 sec for '${id}' element to appear...`);
        await delay(5 * 1000);
    }

    // We failed to find the item
    // Collect page source
    const source = await driver.getPageSource();
    const logs = await driver.manage().logs().get(logging.Type.BROWSER);
    const messages = logs.map((l) => `[${l.level}] ${l.message}`)
        .join(os.EOL);
    throw new Error(
        `Failed to find element '${id}'. Page Source:${os.EOL}${source}${os.EOL}` +
        `Browser Logs (${logs.length} messages):${os.EOL}${messages}${os.EOL}`);
}

export async function isComplete(element: WebElement): Promise<boolean> {
    return (await element.getAttribute("data-done")) === "1";
}

export async function getLogEntry(index: number, element: WebElement): Promise<WebElement> {
    const elements = await element.findElements(By.id(`__tap_item_${index}`));
    if (elements && elements.length > 0) {
        return elements[0];
    }
    return null;
}

export async function getEntryContent(element: WebElement): Promise<string> {
    return await element.getAttribute("value");
}

export async function flushEntries(index: number, element: WebElement, cb: (entry: string) => void): Promise<void> {
    let entry = await getLogEntry(index, element);
    while (entry) {
        index += 1;
        cb(await getEntryContent(entry));
        entry = await getLogEntry(index, element);
    }
}