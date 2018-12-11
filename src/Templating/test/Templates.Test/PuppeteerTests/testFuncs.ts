import { Page } from 'puppeteer';

export function bindConsole(page: Page): string[] {
    let badTypes = ['error', 'warning'];
    let messages: string[] = [];
    page.on('console', msg => {
        if (badTypes.indexOf(msg.type()) > -1) {
            messages.push(msg.text());
        }
    });

    return messages;
}

const escapeXpathString = str => {
    const splitedQuotes = str.replace(/'/g, `', "'", '`);
    return `concat('${splitedQuotes}', '')`;
};

export async function clickByText(page: Page, text: string, tag: string = 'a') {
    const escapedText = escapeXpathString(text);
    const linkHandlers = await page.$x(`//${tag}[contains(text(), ${escapedText})]`);

    if (linkHandlers.length > 0) {
        await linkHandlers[0].click();
    } else {
        throw new Error(`Link not found: ${text}`);
    }
};

export function maybeValidateIdentity(serverPath: string): void {
    // TODO: validate identity here in the future
}

export function validateMessages(messages: string[]): void {
    if (messages.length > 0) {
        fail(messages);
    }
}
