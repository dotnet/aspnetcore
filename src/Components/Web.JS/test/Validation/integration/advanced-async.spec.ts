// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests for advanced async scenarios: custom debounce, submit-skips-debounce,
// user-registered async validators, and the number validator.

import { test, expect, Page } from '@playwright/test';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

async function getFieldMessage(page: Page, fieldName: string): Promise<string> {
  const span = page.locator(`[data-valmsg-for="${fieldName}"]`);
  return (await span.textContent() ?? '').trim();
}

async function hasClass(page: Page, selector: string, className: string): Promise<boolean> {
  return page.locator(selector).evaluate((el, cls) => el.classList.contains(cls), className);
}

async function fillAndBlur(page: Page, selector: string, value: string): Promise<void> {
  await page.locator(selector).focus();
  await page.locator(selector).fill(value);
  await page.locator('#name').focus(); // blur away
}

// ---------------------------------------------------------------------------
// Custom debounce (data-val-remote-debounce)
// ---------------------------------------------------------------------------

test.describe('custom debounce', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/advanced-async.html');
  });

  test('field with debounce=50 validates faster than default', async ({ page }) => {
    await fillAndBlur(page, '#fast-field', 'bad');

    // With 50ms debounce + ~0ms server response, validation should complete well before 200ms
    // (Default debounce is 200ms, so this proves the custom value is respected)
    await page.waitForTimeout(200);

    expect(await getFieldMessage(page, 'FastField')).toBe('Invalid.');
    expect(await hasClass(page, '#fast-field', 'input-validation-error')).toBe(true);
  });

  test('field with debounce=50 shows valid result quickly', async ({ page }) => {
    await fillAndBlur(page, '#fast-field', 'good');

    await page.waitForTimeout(200);

    expect(await getFieldMessage(page, 'FastField')).toBe('');
    expect(await hasClass(page, '#fast-field', 'input-validation-error')).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// Submit skips debounce (context.immediate)
// ---------------------------------------------------------------------------

test.describe('submit skips debounce', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/advanced-async.html');
  });

  test('submitting form with pending remote completes faster than debounce would', async ({ page }) => {
    // Fill all required fields
    await page.fill('#name', 'Test');
    await page.fill('#fast-field', 'good');
    await page.fill('#slow-remote', 'validvalue');

    // Wait for field-level async to complete
    await page.waitForTimeout(1000);

    // Now clear the slow-remote cache by typing a new value
    await page.fill('#slow-remote', 'newvalue');

    // Immediately submit — this should bypass the 200ms default debounce
    // Track the timing of validationcomplete
    const submitTime = await page.evaluate(() => {
      return new Promise<number>(resolve => {
        const start = Date.now();
        document.querySelector('#test-form')!.addEventListener('submit', e => e.preventDefault());
        document.addEventListener('validationcomplete', () => {
          resolve(Date.now() - start);
        }, { once: true });
        (document.querySelector('#submit-btn') as HTMLElement).click();
      });
    });

    // The slow endpoint takes 500ms. Without debounce skip, it would be 200+500=700ms.
    // With immediate=true, it should be ~500ms (just server time, no debounce).
    // We check it's under 650ms to have margin but prove debounce was skipped.
    expect(submitTime).toBeLessThan(650);
  });
});

// ---------------------------------------------------------------------------
// User-registered deferred validator (addValidator with { deferred: true })
// ---------------------------------------------------------------------------

test.describe('custom async validator', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/advanced-async.html');
  });

  test('custom async validator rejects invalid value', async ({ page }) => {
    await fillAndBlur(page, '#custom-async', 'NOTOK');

    // Wait for debounce (200ms default) + server (100ms) + margin
    await page.waitForTimeout(500);

    expect(await getFieldMessage(page, 'CustomField')).toBe('Value must start with OK.');
    expect(await hasClass(page, '#custom-async', 'input-validation-error')).toBe(true);
  });

  test('custom async validator accepts valid value', async ({ page }) => {
    await fillAndBlur(page, '#custom-async', 'OK-valid');

    await page.waitForTimeout(500);

    expect(await getFieldMessage(page, 'CustomField')).toBe('');
    expect(await hasClass(page, '#custom-async', 'input-validation-error')).toBe(false);
  });

  test('custom async validator shows pending class during fetch', async ({ page }) => {
    await fillAndBlur(page, '#custom-async', 'testing');

    // Pending should appear almost immediately (tracker adds it when Promise is returned)
    await page.waitForTimeout(50);
    expect(await hasClass(page, '#custom-async', 'validation-pending')).toBe(true);

    // After server responds, pending should clear
    await page.waitForTimeout(500);
    expect(await hasClass(page, '#custom-async', 'validation-pending')).toBe(false);
  });

  test('custom async validator uses cached result on re-validation', async ({ page }) => {
    // First validation: triggers fetch
    await fillAndBlur(page, '#custom-async', 'OK-cached');
    await page.waitForTimeout(500);
    expect(await getFieldMessage(page, 'CustomField')).toBe('');

    // Change to something else, then back
    await fillAndBlur(page, '#custom-async', 'other');
    await page.waitForTimeout(500);

    // Back to cached value — should return sync (no pending state)
    await fillAndBlur(page, '#custom-async', 'OK-cached');
    await page.waitForTimeout(50);
    // No pending because cache hit returns sync
    expect(await hasClass(page, '#custom-async', 'validation-pending')).toBe(false);
    expect(await getFieldMessage(page, 'CustomField')).toBe('');
  });

  test('custom async validator signal aborts fetch on value change', async ({ page }) => {
    // Type invalid value, then quickly change to valid
    await page.locator('#custom-async').focus();
    await page.locator('#custom-async').fill('NOTOK');
    await page.locator('#custom-async').fill('OK-changed');
    await page.locator('#name').focus(); // blur

    await page.waitForTimeout(500);

    // Should show valid — the NOTOK request was aborted via signal
    expect(await getFieldMessage(page, 'CustomField')).toBe('');
  });
});

// ---------------------------------------------------------------------------
// Number validator (MVC-only, standalone bundle)
// ---------------------------------------------------------------------------

test.describe('number validator', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/advanced-async.html');
  });

  test('rejects non-numeric value', async ({ page }) => {
    await fillAndBlur(page, '#quantity', 'abc');

    expect(await getFieldMessage(page, 'Quantity')).toBe('The field must be a number.');
    expect(await hasClass(page, '#quantity', 'input-validation-error')).toBe(true);
  });

  test('accepts integer', async ({ page }) => {
    await fillAndBlur(page, '#quantity', '42');

    expect(await getFieldMessage(page, 'Quantity')).toBe('');
    expect(await hasClass(page, '#quantity', 'input-validation-error')).toBe(false);
  });

  test('accepts decimal', async ({ page }) => {
    await fillAndBlur(page, '#quantity', '3.14');

    expect(await getFieldMessage(page, 'Quantity')).toBe('');
  });

  test('accepts negative number', async ({ page }) => {
    await fillAndBlur(page, '#quantity', '-5');

    expect(await getFieldMessage(page, 'Quantity')).toBe('');
  });

  test('rejects value with letters mixed in', async ({ page }) => {
    await fillAndBlur(page, '#quantity', '12abc');

    expect(await getFieldMessage(page, 'Quantity')).toBe('The field must be a number.');
  });

  test('empty value passes (not required)', async ({ page }) => {
    await fillAndBlur(page, '#quantity', '');

    expect(await getFieldMessage(page, 'Quantity')).toBe('');
  });
});
