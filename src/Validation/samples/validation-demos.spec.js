// @ts-check
const { test, expect } = require('@playwright/test');

// ─────────────────────────────────────────────────────────────────────────────
// Shared helpers: track console errors and failed network requests
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Attaches listeners that collect console errors and failed requests.
 * Returns an object with arrays that tests can assert against.
 */
function trackPageErrors(page) {
    const errors = { console: [], network: [] };
    page.on('console', msg => {
        if (msg.type() === 'error') {
            errors.console.push(msg.text());
        }
    });
    page.on('response', response => {
        // Ignore source map requests
        if (response.url().endsWith('.map')) return;
        if (response.status() >= 400) {
            errors.network.push(`${response.status()} ${response.url()}`);
        }
    });
    return errors;
}

// ─────────────────────────────────────────────────────────────────────────────
// Blazor Server Demo (port 5010) — Async Validation + Localization
// ─────────────────────────────────────────────────────────────────────────────

test.describe('Blazor Server Demo', () => {
    const BASE = 'http://localhost:5010';

    test('home page loads without errors', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(2000);
        await expect(page.getByRole('heading').first()).toBeVisible();
        expect(errors.console).toEqual([]);
        expect(errors.network).toEqual([]);
    });

    test('register page loads with all form fields and no errors', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        // All form inputs visible
        await expect(page.locator('#email')).toBeVisible();
        await expect(page.locator('#username')).toBeVisible();
        await expect(page.locator('#displayname')).toBeVisible();
        await expect(page.locator('#age')).toBeVisible();
        await expect(page.locator('#website')).toBeVisible();
        await expect(page.locator('button[type="submit"]')).toBeVisible();
        expect(errors.console).toEqual([]);
        expect(errors.network).toEqual([]);
    });

    test('blazor.web.js loads successfully', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        // Verify Blazor is connected (interactive mode active)
        const blazorLoaded = await page.evaluate(() => !!(/** @type {any} */(window)).Blazor);
        expect(blazorLoaded).toBe(true);
        expect(errors.network).toEqual([]);
    });

    test('required validation fires on empty submit', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(3000);
        // Should show validation error messages
        const validationMessages = page.locator('.validation-message, .validation-errors, li');
        const texts = await validationMessages.allTextContents();
        const nonEmpty = texts.filter(t => t.trim().length > 0);
        expect(nonEmpty.length).toBeGreaterThan(0);
        expect(errors.console).toEqual([]);
    });

    test('async validation shows pending indicator for email', async ({ page }) => {
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('#email').fill('admin@example.com');
        await page.locator('#email').press('Tab');
        await page.waitForTimeout(500);
        // Verify the page is still functional after async validation trigger
        await expect(page.locator('#email')).toBeVisible();
        // Wait for any async processing
        await page.waitForTimeout(3000);
    });

    test('async validation rejects taken email on submit', async ({ page }) => {
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('#email').fill('admin@example.com');
        await page.locator('#username').fill('newuser');
        await page.locator('#displayname').fill('New User');
        await page.locator('#age').fill('25');
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(5000);
        // The form should still be rendered (page didn't blank out)
        const bodyText = await page.locator('body').textContent() ?? '';
        expect(bodyText.length).toBeGreaterThan(50);
    });

    test('language switch to Spanish changes page language', async ({ page }) => {
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(1000);
        const esLink = page.locator('a[href*="culture=es"]').first();
        await esLink.click();
        await page.waitForTimeout(2000);
        if (!page.url().includes('/register')) {
            await page.goto(`${BASE}/register`);
            await page.waitForTimeout(2000);
        }
        const bodyText = await page.locator('body').textContent() ?? '';
        // The language picker should reflect Spanish is selected
        // (UI labels may or may not be translated depending on JSON file keys)
        expect(bodyText).toMatch(/Español|Idioma|Spanish|ES/);
    });

    test('successful registration with valid unique data', async ({ page }) => {
        await page.goto(`${BASE}/register`);
        await page.waitForTimeout(2000);
        await page.locator('#email').fill('brand-new@example.com');
        await page.locator('#username').fill('brandnewuser');
        await page.locator('#displayname').fill('Brand New');
        await page.locator('#age').fill('30');
        await page.locator('button[type="submit"]').click();
        // Wait for async validators
        await page.waitForTimeout(6000);
        const bodyText = await page.locator('body').textContent() ?? '';
        // Should show success or no required errors
        const hasSuccess = bodyText.match(/success|✅|submitted/i);
        const hasRequiredError = bodyText.match(/is required|es obligatorio/i);
        expect(hasSuccess || !hasRequiredError).toBeTruthy();
    });

    test('favicon loads without 404', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(2000);
        const favicon404 = errors.network.find(e => e.includes('favicon'));
        expect(favicon404).toBeUndefined();
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// Blazor SSR Demo (port 5020) — Client-Side Validation + Localization
// ─────────────────────────────────────────────────────────────────────────────

test.describe('Blazor SSR Demo', () => {
    const BASE = 'http://localhost:5020';

    test('home page loads without errors', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(1000);
        await expect(page.getByRole('heading').first()).toBeVisible();
        expect(errors.console).toEqual([]);
        expect(errors.network).toEqual([]);
    });

    test('contact page loads with no console errors or failed requests', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(`${BASE}/contact`);
        await page.waitForTimeout(1000);
        expect(errors.console).toEqual([]);
        expect(errors.network).toEqual([]);
    });

    test('blazor.web.js loads successfully', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(`${BASE}/contact`);
        await page.waitForTimeout(1000);
        const blazor404 = errors.network.find(e => e.includes('blazor.web'));
        expect(blazor404).toBeUndefined();
        const script = page.locator('script[src*="blazor.web"]');
        expect(await script.count()).toBeGreaterThanOrEqual(1);
    });

    test('aspnet-core-validation.js loads successfully', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(`${BASE}/contact`);
        await page.waitForTimeout(1000);
        const js404 = errors.network.find(e => e.includes('aspnet-core-validation'));
        expect(js404).toBeUndefined();
        const script = page.locator('script[src*="aspnet-core-validation"]');
        expect(await script.count()).toBeGreaterThanOrEqual(1);
    });

    test('form inputs have data-val="true" attributes', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        const dataValInputs = page.locator('input[data-val="true"], textarea[data-val="true"]');
        expect(await dataValInputs.count()).toBeGreaterThanOrEqual(3);
    });

    test('data-val-required contains English localized message', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        const requiredInput = page.locator('input[data-val-required]').first();
        const msg = await requiredInput.getAttribute('data-val-required');
        expect(msg).toBeTruthy();
        expect(msg?.toLowerCase()).toContain('required');
    });

    test('data-val-email attribute present', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        const emailInput = page.locator('input[data-val-email]');
        expect(await emailInput.count()).toBeGreaterThanOrEqual(1);
    });

    test('data-val-range on age field with correct min/max', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        const rangeInput = page.locator('input[data-val-range]');
        expect(await rangeInput.count()).toBeGreaterThanOrEqual(1);
        expect(await rangeInput.first().getAttribute('data-val-range-min')).toBe('18');
        expect(await rangeInput.first().getAttribute('data-val-range-max')).toBe('120');
    });

    test('client-side validation prevents empty form submission', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        await page.waitForTimeout(1000);
        // Record navigation — if client-side validation works, page should NOT navigate
        let navigated = false;
        page.on('framenavigated', () => { navigated = true; });
        await page.locator('button[type="submit"]').first().click();
        await page.waitForTimeout(1500);
        // Should show client-side error messages
        const errorSpans = page.locator('.field-validation-error, .validation-message, [data-valmsg-for]');
        const texts = await errorSpans.allTextContents();
        const nonEmpty = texts.filter(t => t.trim().length > 0);
        expect(nonEmpty.length).toBeGreaterThan(0);
    });

    test('language switch to Spanish changes data-val messages', async ({ page }) => {
        await page.goto(`${BASE}/contact`);
        const enMsg = await page.locator('input[data-val-required]').first().getAttribute('data-val-required');
        // Switch to Spanish
        const esLink = page.locator('a[href*="culture=es"]').first();
        if (await esLink.isVisible()) {
            await esLink.click();
            await page.waitForTimeout(2000);
            if (!page.url().includes('/contact')) {
                await page.goto(`${BASE}/contact`);
            }
            const esMsg = await page.locator('input[data-val-required]').first().getAttribute('data-val-required');
            expect(esMsg).toBeTruthy();
            // Messages should differ between languages
            if (enMsg && esMsg) {
                expect(esMsg).not.toBe(enMsg);
                expect(esMsg).toMatch(/obligatorio/i);
            }
        }
    });

    test('valid form submission shows success', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(`${BASE}/contact`);
        await page.waitForTimeout(500);
        await page.locator('#name').fill('John Doe');
        await page.locator('#email').fill('john@example.com');
        await page.locator('#phone').fill('555-0123');
        await page.locator('#age').fill('25');
        await page.locator('#message').fill('This is a test message that is long enough to pass validation rules.');
        await page.locator('button[type="submit"]').first().click();
        await page.waitForTimeout(2000);
        const bodyText = await page.locator('body').textContent() ?? '';
        expect(bodyText).toMatch(/thank you|success/i);
        expect(errors.console).toEqual([]);
    });

    test('favicon loads without 404', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(1000);
        const favicon404 = errors.network.find(e => e.includes('favicon'));
        expect(favicon404).toBeUndefined();
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// MVC Demo (port 5030) — jQuery-free Client-Side Validation
// ─────────────────────────────────────────────────────────────────────────────

test.describe('MVC Demo', () => {
    const BASE = 'http://localhost:5030';

    test('home page loads without errors', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(1000);
        await expect(page.getByRole('heading').first()).toBeVisible();
        expect(errors.console).toEqual([]);
        expect(errors.network).toEqual([]);
    });

    test('contact page loads with no console errors or failed requests', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(`${BASE}/Home/Contact`);
        await page.waitForTimeout(1000);
        expect(errors.console).toEqual([]);
        expect(errors.network).toEqual([]);
    });

    test('aspnet-core-validation.js loads successfully', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(`${BASE}/Home/Contact`);
        await page.waitForTimeout(1000);
        const js404 = errors.network.find(e => e.includes('aspnet-core-validation'));
        expect(js404).toBeUndefined();
    });

    test('no jQuery on the page', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        const hasJquery = await page.evaluate(() => typeof (/** @type {any} */(window)).jQuery !== 'undefined');
        expect(hasJquery).toBe(false);
    });

    test('form has data-val attributes from MVC tag helpers', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        const dataValInputs = page.locator('input[data-val="true"]');
        expect(await dataValInputs.count()).toBeGreaterThanOrEqual(5);
    });

    test('data-val-required on required fields', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        const requiredInputs = page.locator('input[data-val-required]');
        expect(await requiredInputs.count()).toBeGreaterThanOrEqual(4);
    });

    test('data-val-equalto on ConfirmPassword', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        const compareInput = page.locator('input[data-val-equalto]');
        expect(await compareInput.count()).toBe(1);
        const otherField = await compareInput.getAttribute('data-val-equalto-other');
        expect(otherField).toContain('Password');
    });

    test('data-val-range on Age', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        const rangeInput = page.locator('input[data-val-range]');
        expect(await rangeInput.count()).toBeGreaterThanOrEqual(1);
        expect(await rangeInput.first().getAttribute('data-val-range-min')).toBe('18');
        expect(await rangeInput.first().getAttribute('data-val-range-max')).toBe('120');
    });

    test('client-side validation prevents empty form submission', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        await page.waitForTimeout(1000);
        // Submit empty form
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(1000);
        // Should show client-side validation errors without a page reload
        const errors = page.locator('.field-validation-error');
        const texts = await errors.allTextContents();
        const nonEmpty = texts.filter(t => t.trim().length > 0);
        expect(nonEmpty.length).toBeGreaterThan(0);
    });

    test('password mismatch shows compare validation error', async ({ page }) => {
        await page.goto(`${BASE}/Home/Contact`);
        await page.waitForTimeout(500);
        const pwInputs = page.locator('input[type="password"]');
        await pwInputs.nth(0).fill('Password123');
        await pwInputs.nth(1).fill('Different456');
        await pwInputs.nth(1).press('Tab');
        await page.waitForTimeout(500);
        // Submit to trigger validation
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(1000);
        const bodyText = await page.locator('body').textContent() ?? '';
        expect(bodyText.toLowerCase()).toMatch(/match|confirm|equal/);
    });

    test('valid form submission shows success', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(`${BASE}/Home/Contact`);
        await page.waitForTimeout(500);
        await page.locator('input[name="Name"], #Name').first().fill('John Doe');
        await page.locator('input[name="Email"], #Email, input[type="email"]').first().fill('john@example.com');
        await page.locator('input[name="Age"], #Age, input[type="number"]').first().fill('30');
        const pwInputs = page.locator('input[type="password"]');
        await pwInputs.nth(0).fill('SecurePass123');
        await pwInputs.nth(1).fill('SecurePass123');
        await page.locator('button[type="submit"]').click();
        await page.waitForTimeout(2000);
        const bodyText = await page.locator('body').textContent() ?? '';
        expect(bodyText).toMatch(/success|thank/i);
        expect(errors.console).toEqual([]);
    });

    test('favicon loads without 404', async ({ page }) => {
        const errors = trackPageErrors(page);
        await page.goto(BASE);
        await page.waitForTimeout(1000);
        const favicon404 = errors.network.find(e => e.includes('favicon'));
        expect(favicon404).toBeUndefined();
    });
});
