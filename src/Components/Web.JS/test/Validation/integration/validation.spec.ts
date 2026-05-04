// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { test, expect, Page } from '@playwright/test';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Returns the trimmed text content of the validation message span for a field. */
async function getFieldMessage(page: Page, fieldName: string): Promise<string> {
  const span = page.locator(`[data-valmsg-for="${fieldName}"]`);
  return (await span.textContent() ?? '').trim();
}

/** Returns whether the input has the error CSS class. */
async function hasErrorClass(page: Page, selector: string): Promise<boolean> {
  return page.locator(selector).evaluate(
    el => el.classList.contains('input-validation-error')
  );
}

/** Returns whether the input has the valid CSS class. */
async function hasValidClass(page: Page, selector: string): Promise<boolean> {
  return page.locator(selector).evaluate(
    el => el.classList.contains('input-validation-valid')
  );
}

/** Submits the form by clicking the submit button and waits for validation to run. */
async function submitForm(page: Page): Promise<void> {
  // Set up the event listener in the page first, then click.
  // Using Promise.all ensures the listener is registered before the click triggers the event.
  await page.evaluate(() =>
    new Promise<void>(resolve => {
      document.addEventListener('validationcomplete', () => resolve(), { once: true });
      (document.querySelector('button[type="submit"]') as HTMLElement).click();
    })
  );
}

/** Checks a checkbox by setting its checked property and firing a change event. */
async function checkCheckbox(page: Page, selector: string): Promise<void> {
  await page.evaluate((sel) => {
    const el = document.querySelector(sel) as HTMLInputElement;
    el.checked = true;
    el.dispatchEvent(new Event('change', { bubbles: true }));
  }, selector);
}

/** Prevents the form from actually navigating on valid submit. */
async function preventFormNavigation(page: Page, formSelector: string): Promise<void> {
  await page.evaluate((sel) => {
    document.querySelector(sel)!.addEventListener('submit', e => e.preventDefault());
  }, formSelector);
}

/** Fills all required fields in the basic validation form. */
async function fillAllRequiredFields(page: Page): Promise<void> {
  await page.fill('#name', 'Alice');
  await page.fill('#email', 'alice@example.com');
  await page.fill('#password', 'password123');
  await checkCheckbox(page, '#agree');
  await page.selectOption('#category', 'A');
}

// ---------------------------------------------------------------------------
// Basic required validation
// ---------------------------------------------------------------------------

test.describe('basic required validation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/basic-validation.html');
  });

  test('submitting empty form shows required errors', async ({ page }) => {
    await submitForm(page);

    expect(await getFieldMessage(page, 'Name')).toBe('The Name field is required.');
    expect(await getFieldMessage(page, 'Email')).toBe('The Email field is required.');
    expect(await getFieldMessage(page, 'Password')).toBe('Password is required.');
    expect(await hasErrorClass(page, '#name')).toBe(true);
    expect(await hasErrorClass(page, '#email')).toBe(true);
  });

  test('filling required fields and submitting clears errors', async ({ page }) => {
    await submitForm(page);

    // Verify errors are present first
    expect(await getFieldMessage(page, 'Name')).not.toBe('');

    await fillAllRequiredFields(page);
    await preventFormNavigation(page, '#test-form');

    await submitForm(page);

    // All required field messages should be cleared
    expect(await getFieldMessage(page, 'Name')).toBe('');
    expect(await getFieldMessage(page, 'Email')).toBe('');
    expect(await getFieldMessage(page, 'Password')).toBe('');
    expect(await hasValidClass(page, '#name')).toBe(true);
    expect(await hasValidClass(page, '#email')).toBe(true);
  });

  test('checkbox required validation', async ({ page }) => {
    await submitForm(page);
    expect(await getFieldMessage(page, 'Agree')).toBe('You must agree to the terms.');

    await checkCheckbox(page, '#agree');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Agree')).toBe('');
  });

  test('select required validation', async ({ page }) => {
    await submitForm(page);
    expect(await getFieldMessage(page, 'Category')).toBe('Please select a category.');

    await page.selectOption('#category', 'B');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Category')).toBe('');
  });
});

// ---------------------------------------------------------------------------
// String length validation
// ---------------------------------------------------------------------------

test.describe('string length validation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/basic-validation.html');
  });

  test('maxlength violation shows error', async ({ page }) => {
    const longText = 'a'.repeat(101);
    await page.fill('#bio', longText);

    // Also fill a required field to ensure we get a validationcomplete event
    // (if all fields were valid, the form would navigate away)
    await submitForm(page);

    expect(await getFieldMessage(page, 'Bio')).toBe('Bio must be at most 100 characters.');
    expect(await hasErrorClass(page, '#bio')).toBe(true);
  });

  test('maxlength within limit is valid', async ({ page }) => {
    await page.fill('#bio', 'a'.repeat(100));

    // Submit will fail on required fields but Bio should be valid
    await submitForm(page);
    expect(await getFieldMessage(page, 'Bio')).toBe('');
  });

  test('minlength+maxlength combined validation (password)', async ({ page }) => {
    // Too short — submit will also fail on other required fields
    await page.fill('#password', 'short');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Password')).toBe('Password must be between 8 and 50 characters.');

    // Just right — password length error should clear (other required fields still fail)
    await page.fill('#password', 'longEnough');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Password')).toBe('');
  });
});

// ---------------------------------------------------------------------------
// ARIA attributes
// ---------------------------------------------------------------------------

test.describe('ARIA attributes', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/basic-validation.html');
  });

  test('invalid field gets aria-invalid and aria-describedby', async ({ page }) => {
    await submitForm(page);

    const ariaInvalid = await page.locator('#name').getAttribute('aria-invalid');
    expect(ariaInvalid).toBe('true');

    const ariaDescribedBy = await page.locator('#name').getAttribute('aria-describedby');
    expect(ariaDescribedBy).toBeTruthy();

    // The described-by ID should point to the message element
    const messageElement = page.locator(`#${ariaDescribedBy}`);
    await expect(messageElement).toHaveText('The Name field is required.');
  });

  test('valid field has no aria-invalid', async ({ page }) => {
    await page.fill('#name', 'Alice');
    await submitForm(page);

    const ariaInvalid = await page.locator('#name').getAttribute('aria-invalid');
    expect(ariaInvalid).toBeNull();
  });
});

// ---------------------------------------------------------------------------
// Validation summary
// ---------------------------------------------------------------------------

test.describe('validation summary', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/basic-validation.html');
  });

  test('summary shows errors after submit', async ({ page }) => {
    await submitForm(page);

    const summary = page.locator('[data-valmsg-summary]');
    await expect(summary).toHaveClass(/validation-summary-errors/);

    const items = summary.locator('li');
    const count = await items.count();
    expect(count).toBeGreaterThan(0);
  });

  test('summary clears when all fields are valid', async ({ page }) => {
    await submitForm(page);

    await fillAllRequiredFields(page);
    await preventFormNavigation(page, '#test-form');

    await submitForm(page);

    const summary = page.locator('[data-valmsg-summary]');
    await expect(summary).toHaveClass(/validation-summary-valid/);
  });
});

// ---------------------------------------------------------------------------
// Form submission prevention
// ---------------------------------------------------------------------------

test.describe('form submission prevention', () => {
  test('invalid form submit is prevented', async ({ page }) => {
    await page.goto('/basic-validation.html');

    // Track whether form actually submitted (navigation would occur)
    let navigated = false;
    page.on('request', req => {
      if (req.isNavigationRequest() && req.method() === 'POST') {
        navigated = true;
      }
    });

    await submitForm(page);
    expect(navigated).toBe(false);
  });

  test('form without validation attributes submits normally', async ({ page }) => {
    await page.goto('/no-validation.html');

    // This form has no data-val attributes, so submit should not be intercepted.
    // Verify by listening for validationcomplete (should NOT fire on untracked forms).
    const validationFired = await page.evaluate(() => {
      return new Promise<boolean>((resolve) => {
        document.addEventListener('validationcomplete', () => resolve(true), { once: true });
        // Submit the form programmatically
        const form = document.querySelector('form');
        if (form) {
          // Prevent actual navigation but let the event propagate
          form.addEventListener('submit', (e) => { e.preventDefault(); }, { once: true });
          form.requestSubmit();
        }
        // If validationcomplete doesn't fire within 100ms, it wasn't intercepted
        setTimeout(() => resolve(false), 100);
      });
    });

    expect(validationFired).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// Change event triggers revalidation
// ---------------------------------------------------------------------------

test.describe('field-level revalidation on change', () => {
  test('typing into a field after submit error clears it on change', async ({ page }) => {
    await page.goto('/basic-validation.html');

    await submitForm(page);
    expect(await getFieldMessage(page, 'Name')).toBe('The Name field is required.');

    // Fill the field — this triggers a 'change' event when the field loses focus
    await page.fill('#name', 'Alice');
    // Trigger change by moving focus away
    await page.locator('#email').focus();

    // After change event fires, the error should be cleared
    expect(await getFieldMessage(page, 'Name')).toBe('');
    expect(await hasValidClass(page, '#name')).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// Form reset
// ---------------------------------------------------------------------------

test.describe('form reset', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/basic-validation.html');
  });

  test('reset clears validation errors and CSS classes', async ({ page }) => {
    // Trigger validation errors
    await submitForm(page);
    expect(await getFieldMessage(page, 'Name')).toBe('The Name field is required.');
    expect(await hasErrorClass(page, '#name')).toBe(true);

    // Click the reset button
    await page.click('button[type="reset"]');

    // Wait for the setTimeout(0) in the reset handler
    await page.waitForTimeout(50);

    expect(await getFieldMessage(page, 'Name')).toBe('');
    expect(await hasErrorClass(page, '#name')).toBe(false);
  });

  test('reset clears validation summary', async ({ page }) => {
    await submitForm(page);

    const summary = page.locator('[data-valmsg-summary]');
    await expect(summary).toHaveClass(/validation-summary-errors/);

    await page.click('button[type="reset"]');
    await page.waitForTimeout(50);

    await expect(summary).toHaveClass(/validation-summary-valid/);
  });
});

// ---------------------------------------------------------------------------
// Custom validator
// ---------------------------------------------------------------------------

test.describe('custom validator', () => {
  test('custom validator rejects invalid input', async ({ page }) => {
    await page.goto('/custom-validator.html');

    await page.fill('#code', 'XYZ-123');
    await submitForm(page);

    expect(await getFieldMessage(page, 'Code')).toBe("Code must start with 'ABC-'.");
  });

  test('custom validator accepts valid input', async ({ page }) => {
    await page.goto('/custom-validator.html');

    await page.fill('#code', 'ABC-123');
    await submitForm(page);

    expect(await getFieldMessage(page, 'Code')).toBe('');
  });

  test('custom validator skips empty value', async ({ page }) => {
    await page.goto('/custom-validator.html');

    // Empty value should be valid (custom validator returns true for empty)
    await submitForm(page);
    expect(await getFieldMessage(page, 'Code')).toBe('');
  });
});

// ---------------------------------------------------------------------------
// validationcomplete event
// ---------------------------------------------------------------------------

test.describe('validationcomplete event', () => {
  test('dispatches with valid:false when form is invalid', async ({ page }) => {
    await page.goto('/basic-validation.html');

    const result = await page.evaluate(() =>
      new Promise<boolean>(resolve => {
        document.addEventListener('validationcomplete', (e: Event) => {
          resolve((e as CustomEvent).detail.valid);
        }, { once: true });
        (document.querySelector('button[type="submit"]') as HTMLElement).click();
      })
    );

    expect(result).toBe(false);
  });

  test('dispatches with valid:true when form is valid', async ({ page }) => {
    await page.goto('/basic-validation.html');

    await fillAllRequiredFields(page);
    await preventFormNavigation(page, '#test-form');

    const result = await page.evaluate(() =>
      new Promise<boolean>(resolve => {
        document.addEventListener('validationcomplete', (e: Event) => {
          resolve((e as CustomEvent).detail.valid);
        }, { once: true });
        (document.querySelector('button[type="submit"]') as HTMLElement).click();
      })
    );

    expect(result).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// setCustomValidity integration (Constraint Validation API)
// ---------------------------------------------------------------------------

test.describe('Constraint Validation API integration', () => {
  test('invalid field has non-empty validationMessage', async ({ page }) => {
    await page.goto('/basic-validation.html');

    await submitForm(page);

    const msg = await page.locator('#name').evaluate(
      (el: HTMLInputElement) => el.validationMessage
    );
    expect(msg).toBe('The Name field is required.');
  });

  test('valid field has empty validationMessage', async ({ page }) => {
    await page.goto('/basic-validation.html');

    await page.fill('#name', 'Alice');
    await submitForm(page);

    const msg = await page.locator('#name').evaluate(
      (el: HTMLInputElement) => el.validationMessage
    );
    expect(msg).toBe('');
  });
});

// ---------------------------------------------------------------------------
// novalidate attribute
// ---------------------------------------------------------------------------

test.describe('novalidate attribute', () => {
  test('form gets novalidate attribute after scan', async ({ page }) => {
    await page.goto('/basic-validation.html');

    const hasNovalidate = await page.locator('#test-form').evaluate(
      (el: HTMLFormElement) => el.hasAttribute('novalidate')
    );
    expect(hasNovalidate).toBe(true);
  });
});
