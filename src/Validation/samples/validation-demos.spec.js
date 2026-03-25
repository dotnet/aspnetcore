// @ts-check
const { test, expect } = require('@playwright/test');

function trackErrors(page) {
    const e = { console: [], network: [] };
    page.on('console', msg => { if (msg.type() === 'error') e.console.push(msg.text()); });
    page.on('response', r => { if (r.status() >= 400 && !r.url().endsWith('.map')) e.network.push(`${r.status()} ${r.url()}`); });
    return e;
}

// ─────────────────────────────────────────────────────────────────────────────
// BLAZOR SERVER (port 5010)
// ─────────────────────────────────────────────────────────────────────────────

test.describe('Blazor Server Demo', () => {
    const BASE = 'http://localhost:5010';

    test('pages load without errors', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(1000);
        expect(e.console).toEqual([]);
        expect(e.network).toEqual([]);
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        expect(e.network).toEqual([]);
    });

    test('required validation on empty submit', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(3000);
        const body = await page.locator('body').textContent() ?? '';
        expect(body.toLowerCase()).toMatch(/required|obligatorio/);
        expect(e.console.filter(c => c.includes('circuit'))).toEqual([]);
    });

    test('per-field sync: invalid email format', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('#email').fill('not-an-email');
        await page.locator('#email').press('Tab');
        await page.waitForTimeout(1500);
        const body = await page.locator('body').textContent() ?? '';
        expect(body.toLowerCase()).toMatch(/valid email|email.*valid|correo/);
        expect(body).not.toMatch(/already|registered/);
        expect(e.console.filter(c => c.includes('circuit'))).toEqual([]);
    });

    test('per-field async: taken email shows error after delay', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('#email').fill('admin@example.com');
        await page.locator('#email').press('Tab');
        await page.waitForTimeout(4000);
        const body = await page.locator('body').textContent() ?? '';
        expect(body).toMatch(/already|registered/i);
        expect(e.console.filter(c => c.includes('circuit'))).toEqual([]);
    });

    test('per-field async cancellation: re-edit cancels pending', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('#email').fill('admin@example.com');
        await page.locator('#email').press('Tab');
        await page.waitForTimeout(500);
        await page.locator('#email').fill('new-value@example.com');
        await page.locator('#email').press('Tab');
        await page.waitForTimeout(4000);
        const body = await page.locator('body').textContent() ?? '';
        expect(body).not.toContain('admin@example.com');
        expect(e.console.filter(c => c.includes('circuit'))).toEqual([]);
    });

    test('submit cancels pending field async — no stale errors', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('#email').fill('admin@example.com');
        await page.locator('#email').press('Tab');
        await page.waitForTimeout(300);
        await page.locator('#email').fill('brand-new@example.com');
        await page.locator('#username').fill('brandnew');
        await page.locator('#age').fill('30');
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(6000);
        const body = await page.locator('body').textContent() ?? '';
        expect(body).not.toContain('admin@example.com');
        expect(e.console.filter(c => c.includes('circuit'))).toEqual([]);
    });

    test('valid submit succeeds and disables form', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('#email').fill('unique-new@example.com');
        await page.locator('#username').fill('uniquenew');
        await page.locator('#age').fill('25');
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(6000);
        expect(await page.locator('fieldset[disabled]').count()).toBeGreaterThanOrEqual(1);
        expect(e.console.filter(c => c.includes('circuit'))).toEqual([]);
    });

    test('submit with taken email shows async error', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('#email').fill('test@example.com');
        await page.locator('#username').fill('uniqueuser');
        await page.locator('#age').fill('20');
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(5000);
        const body = await page.locator('body').textContent() ?? '';
        expect(body).toMatch(/already|registered/i);
        expect(e.console.filter(c => c.includes('circuit'))).toEqual([]);
    });

    test('language switch to Spanish', async ({ page }) => {
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('a[href*="culture=es"]').first().click();
        await page.waitForTimeout(3000);
        if (!page.url().includes('/register')) await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        const body = await page.locator('body').textContent() ?? '';
        expect(body).toMatch(/Correo|Nombre|Registro|Validando|Edad/);
    });

    test('favicon loads', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(1000);
        expect(e.network.find(n => n.includes('favicon'))).toBeUndefined();
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// BLAZOR SSR (port 5020)
// ─────────────────────────────────────────────────────────────────────────────

test.describe('Blazor SSR Demo', () => {
    const BASE = 'http://localhost:5020';

    test('pages load without errors', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(1000);
        expect(e.console).toEqual([]);
        expect(e.network).toEqual([]);
        await page.goto(`${BASE}/contact`);
        await page.waitForTimeout(1000);
        expect(e.network).toEqual([]);
    });

    test('data-val attributes with English messages', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        expect(await page.locator('input[data-val="true"]').count()).toBeGreaterThanOrEqual(3);
        const msg = await page.locator('input[data-val-required]').first().getAttribute('data-val-required');
        expect(msg?.toLowerCase()).toContain('required');
    });

    test('data-val-range on age with min 13', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        const rangeInput = page.locator('input[data-val-range]');
        expect(await rangeInput.first().getAttribute('data-val-range-min')).toBe('13');
        expect(await rangeInput.first().getAttribute('data-val-range-max')).toBe('120');
    });

    test('client-side validation prevents empty submit', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        await page.waitForTimeout(1000);
        await page.locator('button[type="submit"]').first().click();
        await page.waitForTimeout(1500);
        const errors = await page.locator('.field-validation-error, .validation-message').allTextContents();
        expect(errors.filter(t => t.trim().length > 0).length).toBeGreaterThan(0);
    });

    test('client-side email format validation', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        await page.waitForTimeout(1000);
        await page.locator('#email').fill('not-valid');
        await page.locator('button[type="submit"]').first().click();
        await page.waitForTimeout(1000);
        const body = await page.locator('body').textContent() ?? '';
        expect(body.toLowerCase()).toMatch(/email|correo/);
    });

    test('Spanish locale changes data-val messages', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        const enMsg = await page.locator('input[data-val-required]').first().getAttribute('data-val-required');
        await page.locator('a[href*="culture=es"]').first().click();
        await page.waitForTimeout(2000);
        if (!page.url().includes('/contact')) await page.goto(`${BASE}/contact`);
        const esMsg = await page.locator('input[data-val-required]').first().getAttribute('data-val-required');
        expect(esMsg).not.toBe(enMsg);
        expect(esMsg?.toLowerCase()).toContain('obligatorio');
    });

    test('Spanish locale changes UI text', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        await page.locator('a[href*="culture=es"]').first().click();
        await page.waitForTimeout(2000);
        if (!page.url().includes('/contact')) await page.goto(`${BASE}/contact`);
        const body = await page.locator('body').textContent() ?? '';
        expect(body).toMatch(/Contáctenos|Enviar|Nombre|Correo/);
    });

    test('valid submit shows success and disables form', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        await page.waitForTimeout(500);
        await page.locator('#name').fill('John Doe');
        await page.locator('#email').fill('john@example.com');
        await page.locator('#phone').fill('555-0123');
        await page.locator('#age').fill('25');
        await page.locator('#message').fill('This is a long enough test message for the form validation.');
        await page.locator('button[type="submit"]').first().click();
        await page.waitForTimeout(2000);
        const body = await page.locator('body').textContent() ?? '';
        expect(body).toMatch(/thank you|success|gracias/i);
        expect(await page.locator('fieldset[disabled]').count()).toBeGreaterThanOrEqual(1);
    });

    test('JS scripts load without 404', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(`${BASE}/contact`);
        await page.waitForTimeout(1000);
        expect(e.network.find(n => n.includes('blazor.web'))).toBeUndefined();
        expect(e.network.find(n => n.includes('aspnet-core-validation'))).toBeUndefined();
    });

    test('favicon loads', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(1000);
        expect(e.network.find(n => n.includes('favicon'))).toBeUndefined();
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// MVC (port 5030)
// ─────────────────────────────────────────────────────────────────────────────

test.describe('MVC Demo', () => {
    const BASE = 'http://localhost:5030';

    test('pages load without errors', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(1000);
        expect(e.console).toEqual([]);
        expect(e.network).toEqual([]);
        await page.goto(`${BASE}/Home/Contact`);
        await page.waitForTimeout(1000);
        expect(e.console).toEqual([]);
        expect(e.network).toEqual([]);
    });

    test('no jQuery on the page', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        expect(await page.evaluate(() => typeof (/** @type {any} */(window)).jQuery !== 'undefined')).toBe(false);
    });

    test('data-val attributes from MVC tag helpers', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        expect(await page.locator('input[data-val="true"]').count()).toBeGreaterThanOrEqual(5);
        expect(await page.locator('input[data-val-required]').count()).toBeGreaterThanOrEqual(4);
    });

    test('data-val-range on age with min 13', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        expect(await page.locator('input[data-val-range]').first().getAttribute('data-val-range-min')).toBe('13');
    });

    test('data-val-equalto on ConfirmPassword', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        const cmp = page.locator('input[data-val-equalto]');
        expect(await cmp.count()).toBe(1);
        expect(await cmp.getAttribute('data-val-equalto-other')).toContain('Password');
    });

    test('client-side validation prevents empty submit', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        await page.waitForTimeout(1000);
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(1000);
        const errors = await page.locator('.field-validation-error').allTextContents();
        expect(errors.filter(t => t.trim().length > 0).length).toBeGreaterThan(0);
    });

    test('password mismatch triggers compare error', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        await page.waitForTimeout(500);
        await page.locator('input[type="password"]').nth(0).fill('Pass1234');
        await page.locator('input[type="password"]').nth(1).fill('Different');
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(1000);
        const body = await page.locator('body').textContent() ?? '';
        expect(body.toLowerCase()).toMatch(/match|confirm|equal/);
    });

    test('valid submit shows success and disables form', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        await page.waitForTimeout(500);
        await page.locator('#Name').fill('John Doe');
        await page.locator('#Email').fill('john@example.com');
        await page.locator('#Age').fill('25');
        await page.locator('input[type="password"]').nth(0).fill('SecurePass1');
        await page.locator('input[type="password"]').nth(1).fill('SecurePass1');
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(2000);
        const body = await page.locator('body').textContent() ?? '';
        expect(body).toMatch(/success|thank/i);
        expect(await page.locator('fieldset[disabled]').count()).toBeGreaterThanOrEqual(1);
    });

    test('favicon loads', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(1000);
        expect(e.network.find(n => n.includes('favicon'))).toBeUndefined();
    });

    test('aspnet-core-validation.js loads', async ({ page }) => {
        const e = trackErrors(page);
        await page.goto(`${BASE}/Home/Contact`);
        await page.waitForTimeout(1000);
        expect(e.network.find(n => n.includes('aspnet-core-validation'))).toBeUndefined();
    });
});
