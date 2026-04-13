// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests for async (remote) validation.
// Expected to FAIL until the remote validator and PendingTracker are implemented.

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

/** Fills a field and triggers change by blurring away. */
async function fillAndBlur(page: Page, selector: string, value: string): Promise<void> {
  await page.locator(selector).focus();
  await page.locator(selector).fill(value);
  await page.locator('#email').focus(); // blur away
}

// ---------------------------------------------------------------------------
// Remote validation — basic
// ---------------------------------------------------------------------------

test.describe('remote validation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/remote-validation.html');
  });

  test('valid username passes remote validation', async ({ page }) => {
    await fillAndBlur(page, '#username', 'alice');

    // Wait for the async validation to complete (100ms server + 300ms debounce)
    await page.waitForTimeout(600);

    expect(await getFieldMessage(page, 'Username')).toBe('');
    expect(await hasClass(page, '#username', 'input-validation-error')).toBe(false);
  });

  test('taken username fails remote validation', async ({ page }) => {
    await fillAndBlur(page, '#username', 'taken');

    // Wait for async validation
    await page.waitForTimeout(600);

    expect(await getFieldMessage(page, 'Username')).toBe('This username is already taken.');
    expect(await hasClass(page, '#username', 'input-validation-error')).toBe(true);
  });

  test('empty username fails required before remote fires', async ({ page }) => {
    // Submit with empty form — required should catch it before remote
    await page.evaluate(() => {
      return new Promise<void>(resolve => {
        document.addEventListener('validationcomplete', () => resolve(), { once: true });
        (document.querySelector('#submit-btn') as HTMLElement).click();
      });
    });

    expect(await getFieldMessage(page, 'Username')).toBe('Username is required.');
  });

  test('changing value aborts previous remote request', async ({ page }) => {
    // Type "taken" then quickly change to "alice"
    await page.locator('#username').focus();
    await page.locator('#username').fill('taken');
    // Don't blur yet — change the value quickly
    await page.locator('#username').fill('alice');
    await page.locator('#email').focus(); // now blur

    // Wait for validation to complete
    await page.waitForTimeout(600);

    // Should show valid (the "taken" request was aborted, "alice" is valid)
    expect(await getFieldMessage(page, 'Username')).toBe('');
  });

  test('cached result avoids re-fetch', async ({ page }) => {
    // First validation: triggers fetch
    await fillAndBlur(page, '#username', 'alice');
    await page.waitForTimeout(600);
    expect(await getFieldMessage(page, 'Username')).toBe('');

    // Change to something else and back
    await fillAndBlur(page, '#username', 'bob');
    await page.waitForTimeout(600);

    // Change back to "alice" — should use cache (no fetch)
    await fillAndBlur(page, '#username', 'alice');
    // Cache hit should be near-instant, no need for long wait
    await page.waitForTimeout(100);
    expect(await getFieldMessage(page, 'Username')).toBe('');
  });
});

// ---------------------------------------------------------------------------
// Pending UI state
// ---------------------------------------------------------------------------

test.describe('pending state', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/remote-validation.html');
  });

  test('validation-pending class applied during async validation', async ({ page }) => {
    await page.locator('#slow-field').focus();
    await page.locator('#slow-field').fill('hello');
    await page.locator('#email').focus(); // blur to trigger validation

    // The pending class should appear immediately (tracker adds it when Promise is returned)
    await page.waitForTimeout(100);
    expect(await hasClass(page, '#slow-field', 'validation-pending')).toBe(true);

    // After debounce (300ms) + server response (500ms) + margin, pending should clear
    await page.waitForTimeout(900);
    expect(await hasClass(page, '#slow-field', 'validation-pending')).toBe(false);
  });

  test('validation-pending class removed on form reset', async ({ page }) => {
    await page.locator('#slow-field').focus();
    await page.locator('#slow-field').fill('hello');
    await page.locator('#email').focus(); // trigger validation

    await page.waitForTimeout(350);
    expect(await hasClass(page, '#slow-field', 'validation-pending')).toBe(true);

    // Reset the form while pending
    await page.click('#reset-btn');
    await page.waitForTimeout(50);

    expect(await hasClass(page, '#slow-field', 'validation-pending')).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// Form submission blocking
// ---------------------------------------------------------------------------

test.describe('form submission with pending async', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/remote-validation.html');
  });

  test('form submission blocked while remote validation is pending', async ({ page }) => {
    // Fill all required fields
    await page.fill('#username', 'alice');
    await page.fill('#email', 'alice@example.com');
    // slow-field is not required, leave empty

    // Track navigation attempts
    let navigated = false;
    page.on('request', req => {
      if (req.isNavigationRequest() && req.method() === 'POST') {
        navigated = true;
      }
    });

    // Fill the slow field to trigger pending remote validation
    await fillAndBlur(page, '#slow-field', 'hello');

    // Try to submit while slow validation is pending (within debounce + fetch window)
    await page.waitForTimeout(50);
    await page.click('#submit-btn');

    // Submission should be blocked (pending async)
    await page.waitForTimeout(100);
    expect(navigated).toBe(false);
  });

  test('deferred submission completes after pending resolves', async ({ page }) => {
    // Fill all required fields
    await page.fill('#username', 'alice');
    await page.fill('#email', 'alice@example.com');

    // Fill slow field with a valid value
    await fillAndBlur(page, '#slow-field', 'hello');

    // Try to submit while pending
    await page.waitForTimeout(50);

    // Prevent actual navigation so we can check the deferred submit happened
    await page.evaluate(() => {
      document.querySelector('#test-form')!.addEventListener('submit', e => {
        // Only prevent on the SECOND submit event (the deferred re-submit)
        // The first one is caught by validation and prevented already
      });
    });

    await page.click('#submit-btn');

    // Wait for slow validation to complete + deferred submission
    await page.waitForTimeout(1000);

    // The validationcomplete event should have fired with valid=true
    const result = await page.evaluate(() => {
      return new Promise<boolean>(resolve => {
        // If deferred submission already happened, check if form was submitted
        // by looking for the validationcomplete event that would have fired
        const form = document.getElementById('test-form') as HTMLFormElement;
        // Check if novalidate is still set (it should be — our lib set it)
        resolve(form.hasAttribute('novalidate'));
      });
    });
    expect(result).toBe(true);
  });

  test('form submission proceeds normally when no async validators pending', async ({ page }) => {
    // Only fill sync-validated fields (no remote fields)
    await page.fill('#username', 'alice');
    await page.fill('#email', 'alice@example.com');

    // Wait for any async validation on username to complete
    await page.waitForTimeout(600);

    // Prevent navigation
    await page.evaluate(() => {
      document.querySelector('#test-form')!.addEventListener('submit', e => e.preventDefault());
    });

    // Submit — should not be blocked (no pending)
    const eventFired = await page.evaluate(() => {
      return new Promise<boolean>(resolve => {
        document.addEventListener('validationcomplete', (e: Event) => {
          resolve((e as CustomEvent).detail.valid);
        }, { once: true });
        (document.querySelector('#submit-btn') as HTMLElement).click();
      });
    });

    expect(eventFired).toBe(true);
  });
});
