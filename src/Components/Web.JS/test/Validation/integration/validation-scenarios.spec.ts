// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { test, expect, Page } from '@playwright/test';

// ---------------------------------------------------------------------------
// Helpers (shared with validation.spec.ts patterns)
// ---------------------------------------------------------------------------

async function getFieldMessage(page: Page, fieldName: string, formSelector?: string): Promise<string> {
  const scope = formSelector ? page.locator(formSelector) : page;
  const span = scope.locator(`[data-valmsg-for="${fieldName}"]`);
  return (await span.textContent() ?? '').trim();
}

async function hasErrorClass(page: Page, selector: string): Promise<boolean> {
  return page.locator(selector).evaluate(el => el.classList.contains('input-validation-error'));
}

async function submitForm(page: Page, formSelector?: string): Promise<void> {
  const btnSelector = formSelector
    ? `${formSelector} button[type="submit"]`
    : 'button[type="submit"]';

  await page.evaluate((sel) =>
    new Promise<void>(resolve => {
      document.addEventListener('validationcomplete', () => resolve(), { once: true });
      (document.querySelector(sel) as HTMLElement).click();
    }), btnSelector
  );
}

async function preventFormNavigation(page: Page, formSelector: string): Promise<void> {
  await page.evaluate((sel) => {
    document.querySelector(sel)!.addEventListener('submit', e => e.preventDefault());
  }, formSelector);
}

// ---------------------------------------------------------------------------
// Scenario 4: Validation timing — lazy/eager pattern
// ---------------------------------------------------------------------------

test.describe('timing: lazy validation, eager recovery', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/timing.html');
  });

  test('no input validation on pristine form (before submit)', async ({ page }) => {
    // Type in field then clear it — should NOT show error because form is pristine
    await page.fill('#default-field', 'hello');
    await page.fill('#default-field', '');

    // Trigger input event explicitly
    await page.locator('#default-field').pressSequentially('a');
    await page.locator('#default-field').press('Backspace');

    expect(await getFieldMessage(page, 'Default')).toBe('');
  });

  test('change/blur validates on pristine form', async ({ page }) => {
    // Type a value and blur — change fires, field is valid
    await page.locator('#default-field').pressSequentially('x');
    await page.locator('#submit-only').focus();
    expect(await getFieldMessage(page, 'Default')).toBe('');

    // Come back, clear the value, and blur — change fires, field is now invalid
    await page.locator('#default-field').fill('');
    await page.locator('#submit-only').focus();
    expect(await getFieldMessage(page, 'Default')).toBe('Default is required.');
  });

  test('input validation activates after submit', async ({ page }) => {
    await submitForm(page);
    expect(await getFieldMessage(page, 'Default')).toBe('Default is required.');

    // Now type — input event should trigger validation (eager mode after submit)
    await page.locator('#default-field').pressSequentially('h');
    expect(await getFieldMessage(page, 'Default')).toBe('');

    // Clear again — error should reappear immediately while typing
    await page.locator('#default-field').press('Backspace');
    expect(await getFieldMessage(page, 'Default')).toBe('Default is required.');
  });

  test('per-field eager recovery on blur (before submit)', async ({ page }) => {
    // Type, blur (valid), come back, clear, blur (invalid) — triggers change both times
    await page.locator('#default-field').pressSequentially('x');
    await page.locator('#change-only').focus();
    expect(await getFieldMessage(page, 'Default')).toBe('');

    await page.locator('#default-field').fill('');
    await page.locator('#change-only').focus();
    expect(await getFieldMessage(page, 'Default')).toBe('Default is required.');

    // Now type — field was marked invalid, so input should validate eagerly
    await page.locator('#default-field').focus();
    await page.locator('#default-field').pressSequentially('h');
    expect(await getFieldMessage(page, 'Default')).toBe('');
  });

  test('reset returns form to pristine (no input validation)', async ({ page }) => {
    await submitForm(page);
    expect(await getFieldMessage(page, 'Default')).toBe('Default is required.');

    await page.click('button[type="reset"]');
    await page.waitForTimeout(50);

    // After reset, typing should NOT trigger validation (pristine again)
    await page.locator('#default-field').pressSequentially('a');
    await page.locator('#default-field').press('Backspace');
    expect(await getFieldMessage(page, 'Default')).toBe('');
  });

  test('reset clears both error and valid CSS classes on invalid field', async ({ page }) => {
    await submitForm(page);
    expect(await hasErrorClass(page, '#default-field')).toBe(true);

    await page.click('button[type="reset"]');
    await page.waitForTimeout(50);

    // Input should have neither error nor valid class — pristine, like initial render
    const inputClasses = await page.locator('#default-field').evaluate(el => Array.from(el.classList));
    expect(inputClasses).not.toContain('input-validation-error');
    expect(inputClasses).not.toContain('input-validation-valid');

    // Message element should have neither class and no text
    const msg = page.locator('[data-valmsg-for="Default"]');
    const msgClasses = await msg.evaluate(el => Array.from(el.classList));
    expect(msgClasses).not.toContain('field-validation-error');
    expect(msgClasses).not.toContain('field-validation-valid');
    expect((await msg.textContent() ?? '').trim()).toBe('');

    // aria-invalid should be absent
    expect(await page.locator('#default-field').getAttribute('aria-invalid')).toBeNull();
  });

  test('reset clears valid class on previously fixed field', async ({ page }) => {
    // Submit empty -> field is invalid
    await submitForm(page);
    // Fix the field -> field gets input-validation-valid class after eager recovery
    await page.fill('#default-field', 'hello');
    await page.locator('#submit-only').focus();
    const classesAfterFix = await page.locator('#default-field').evaluate(el => Array.from(el.classList));
    expect(classesAfterFix).toContain('input-validation-valid');

    // Reset -> pristine, no valid class
    await page.click('button[type="reset"]');
    await page.waitForTimeout(50);

    const classesAfterReset = await page.locator('#default-field').evaluate(el => Array.from(el.classList));
    expect(classesAfterReset).not.toContain('input-validation-valid');
    expect(classesAfterReset).not.toContain('input-validation-error');
  });
});

// ---------------------------------------------------------------------------
// Scenario 4: data-valevent overrides
// ---------------------------------------------------------------------------

test.describe('timing: data-valevent overrides', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/timing.html');
  });

  test('data-valevent="submit" — no validation on change or input', async ({ page }) => {
    await page.fill('#submit-only', 'x');
    await page.fill('#submit-only', '');
    await page.locator('#default-field').focus(); // blur away
    expect(await getFieldMessage(page, 'SubmitOnly')).toBe('');

    // Only validates on submit
    await submitForm(page);
    expect(await getFieldMessage(page, 'SubmitOnly')).toBe('SubmitOnly is required.');
  });

  test('data-valevent="change" — validates on blur, not on input after submit', async ({ page }) => {
    // Submit first to enable eager mode on default fields
    await submitForm(page);
    expect(await getFieldMessage(page, 'ChangeOnly')).toBe('ChangeOnly is required.');

    // Fix the field
    await page.fill('#change-only', 'hello');
    await page.locator('#default-field').focus(); // blur away to trigger change
    expect(await getFieldMessage(page, 'ChangeOnly')).toBe('');

    // Clear and type — should NOT revalidate on input (change-only mode)
    await page.fill('#change-only', '');
    await page.locator('#change-only').focus();
    await page.locator('#change-only').pressSequentially('a');
    await page.locator('#change-only').press('Backspace');
    // Error shouldn't appear until blur/change
    expect(await getFieldMessage(page, 'ChangeOnly')).toBe('');
  });

  test('data-valevent="input" — validates on every keystroke from start', async ({ page }) => {
    // Type and delete without submitting — should validate eagerly from the start
    await page.locator('#input-eager').pressSequentially('a');
    expect(await getFieldMessage(page, 'InputEager')).toBe('');

    await page.locator('#input-eager').press('Backspace');
    expect(await getFieldMessage(page, 'InputEager')).toBe('InputEager is required.');

    await page.locator('#input-eager').pressSequentially('b');
    expect(await getFieldMessage(page, 'InputEager')).toBe('');
  });
});

// ---------------------------------------------------------------------------
// Scenario 4/6: Hidden/disabled fields and formnovalidate
// ---------------------------------------------------------------------------

test.describe('hidden fields and formnovalidate', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/formnovalidate.html');
  });

  test('hidden input[type=hidden] is skipped', async ({ page }) => {
    await submitForm(page);
    expect(await getFieldMessage(page, 'HiddenField')).toBe('');
    expect(await getFieldMessage(page, 'Name')).toBe('Name is required.');
  });

  test('disabled field is skipped', async ({ page }) => {
    await submitForm(page);
    expect(await getFieldMessage(page, 'DisabledField')).toBe('');
  });

  test('display:none field is skipped', async ({ page }) => {
    await submitForm(page);
    expect(await getFieldMessage(page, 'DisplayNoneField')).toBe('');
  });

  test('formnovalidate button bypasses validation', async ({ page }) => {
    // The draft button has formnovalidate — validation should not run
    const result = await page.evaluate(() => {
      let eventFired = false;
      document.addEventListener('validationcomplete', () => { eventFired = true; }, { once: true });
      (document.getElementById('btn-draft') as HTMLElement).click();
      return eventFired;
    });
    expect(result).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// Scenario 8: ARIA advanced
// ---------------------------------------------------------------------------

test.describe('ARIA advanced', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/aria-advanced.html');
  });

  test('message elements get role="alert" and aria-live="assertive"', async ({ page }) => {
    await submitForm(page);

    const role = await page.locator('#name-msg').getAttribute('role');
    const ariaLive = await page.locator('#name-msg').getAttribute('aria-live');
    expect(role).toBe('alert');
    expect(ariaLive).toBe('assertive');
  });

  test('developer-specified role is preserved', async ({ page }) => {
    await submitForm(page);

    // The Email message span has role="status" set by the developer
    const emailMsg = page.locator('[data-valmsg-for="Email"]');
    const role = await emailMsg.getAttribute('role');
    expect(role).toBe('status'); // should NOT be overwritten to "alert"
  });

  test('data-valmsg-replace="false" preserves original content', async ({ page }) => {
    const codeMsg = page.locator('[data-valmsg-for="Code"]');

    // Before submit — original content
    expect(await codeMsg.textContent()).toBe('Enter a valid code');

    await submitForm(page);

    // After submit — content should be preserved (not replaced with error)
    expect(await codeMsg.textContent()).toBe('Enter a valid code');

    // But CSS class should toggle to error
    await expect(codeMsg).toHaveClass(/field-validation-error/);
  });

  test('reset clears aria-invalid and aria-describedby', async ({ page }) => {
    await submitForm(page);

    expect(await page.locator('#name').getAttribute('aria-invalid')).toBe('true');
    expect(await page.locator('#name').getAttribute('aria-describedby')).toBeTruthy();

    await page.click('button[type="reset"]');
    await page.waitForTimeout(50);

    expect(await page.locator('#name').getAttribute('aria-invalid')).toBeNull();
    expect(await page.locator('#name').getAttribute('aria-describedby')).toBeNull();
  });
});

// ---------------------------------------------------------------------------
// Scenario 5: Multiple forms
// ---------------------------------------------------------------------------

test.describe('multiple forms', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/multiple-forms.html');
  });

  test('submitting one form does not validate the other', async ({ page }) => {
    // Submit form A
    await submitForm(page, '#form-a');

    // Form A should have error
    expect(await getFieldMessage(page, 'Name', '#form-a')).toBe('Name A is required.');

    // Form B should be unaffected
    expect(await getFieldMessage(page, 'Name', '#form-b')).toBe('');
  });

  test('each form has independent summary', async ({ page }) => {
    await submitForm(page, '#form-a');

    const summaryA = page.locator('#form-a [data-valmsg-summary]');
    const summaryB = page.locator('#form-b [data-valmsg-summary]');

    await expect(summaryA).toHaveClass(/validation-summary-errors/);
    await expect(summaryB).toHaveClass(/validation-summary-valid/);
  });
});

// ---------------------------------------------------------------------------
// Scenario 5/10: Dynamic content and re-scan
// ---------------------------------------------------------------------------

test.describe('dynamic content and re-scan', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/dynamic-content.html');
  });

  test('dynamically added fields are validated after scanRules()', async ({ page }) => {
    // Inject a new field
    await page.evaluate(() => {
      const container = document.getElementById('dynamic-container')!;
      container.innerHTML = `
        <input id="dynamic" name="Dynamic" type="text"
               data-val="true"
               data-val-required="Dynamic is required." />
        <span data-valmsg-for="Dynamic" data-valmsg-replace="true"></span>
      `;
      (window as any).__aspnetValidation.scanRules('#dynamic-container');
    });

    await submitForm(page);
    expect(await getFieldMessage(page, 'Dynamic')).toBe('Dynamic is required.');
  });

  test('removed fields are cleaned up on re-scan', async ({ page }) => {
    // First submit — Name is required
    await submitForm(page);
    expect(await getFieldMessage(page, 'Name')).toBe('Name is required.');

    // Remove the name field and re-scan
    await page.evaluate(() => {
      const input = document.getElementById('name')!;
      input.parentElement!.removeChild(input);
      (window as any).__aspnetValidation.scanRules();
    });

    // Submit again — Name error should be gone (field no longer tracked)
    await submitForm(page);
    expect(await getFieldMessage(page, 'Name')).toBe('');
  });

  test('scanRules() with selector scopes to subtree', async ({ page }) => {
    // Add fields to two separate containers
    await page.evaluate(() => {
      const container = document.getElementById('dynamic-container')!;
      container.innerHTML = `
        <input name="InScope" type="text"
               data-val="true"
               data-val-required="InScope required." />
        <span data-valmsg-for="InScope" data-valmsg-replace="true"></span>
      `;

      // Add outside the container (directly in the form, but after initial scan)
      const form = document.getElementById('test-form')!;
      const outsideInput = document.createElement('input');
      outsideInput.name = 'OutOfScope';
      outsideInput.type = 'text';
      outsideInput.setAttribute('data-val', 'true');
      outsideInput.setAttribute('data-val-required', 'OutOfScope required.');
      form.insertBefore(outsideInput, form.querySelector('button'));

      const outsideSpan = document.createElement('span');
      outsideSpan.setAttribute('data-valmsg-for', 'OutOfScope');
      outsideSpan.setAttribute('data-valmsg-replace', 'true');
      form.insertBefore(outsideSpan, form.querySelector('button'));

      // Only scan the dynamic container
      (window as any).__aspnetValidation.scanRules('#dynamic-container');
    });

    await submitForm(page);

    // InScope was scanned — should show error
    expect(await getFieldMessage(page, 'InScope')).toBe('InScope required.');

    // OutOfScope was NOT scanned — should not show error
    expect(await getFieldMessage(page, 'OutOfScope')).toBe('');
  });
});

// ---------------------------------------------------------------------------
// Scenario 9: Custom validator advanced
// ---------------------------------------------------------------------------

test.describe('custom validator advanced', () => {
  test('addValidator overrides built-in validator', async ({ page }) => {
    await page.goto('/basic-validation.html');

    // Override the required validator to always pass
    await page.evaluate(() => {
      (window as any).__aspnetValidation.addValidator('required', () => true);
    });

    await submitForm(page);

    // Required validation should now pass (overridden)
    expect(await getFieldMessage(page, 'Name')).toBe('');
  });

  test('custom validator returning string overrides default message', async ({ page }) => {
    await page.goto('/custom-validator.html');

    // Override the customformat validator to return a custom string message
    await page.evaluate(() => {
      (window as any).__aspnetValidation.addValidator('customformat', (ctx: any) => {
        if (!ctx.value) return true;
        if (!ctx.value.startsWith(ctx.params.prefix || '')) return 'Custom override message!';
        return true;
      });
    });

    await page.fill('#code', 'XYZ-123');
    await submitForm(page);

    expect(await getFieldMessage(page, 'Code')).toBe('Custom override message!');
  });
});

// ---------------------------------------------------------------------------
// Scenario 8: Focus first invalid element
// ---------------------------------------------------------------------------

test.describe('focus behavior', () => {
  test('first invalid field is focused on submit', async ({ page }) => {
    await page.goto('/basic-validation.html');

    await submitForm(page);

    const focusedId = await page.evaluate(() => document.activeElement?.id);
    expect(focusedId).toBe('name');
  });
});

// ---------------------------------------------------------------------------
// Scenario 2: Built-in validators (integration)
// ---------------------------------------------------------------------------

test.describe('built-in validators integration', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/all-validators.html');
  });

  test('range: rejects out-of-range value', async ({ page }) => {
    await page.fill('#age', '200');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Age')).toBe('Age must be between 1 and 150.');
  });

  test('range: accepts in-range value', async ({ page }) => {
    await page.fill('#age', '25');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Age')).toBe('');
  });

  test('regex: rejects non-matching value', async ({ page }) => {
    await page.fill('#zipcode', 'abc');
    await submitForm(page);
    expect(await getFieldMessage(page, 'ZipCode')).toBe('Invalid zip code.');
  });

  test('regex: accepts matching value', async ({ page }) => {
    await page.fill('#zipcode', '12345');
    await submitForm(page);
    expect(await getFieldMessage(page, 'ZipCode')).toBe('');
  });

  test('email: rejects invalid email', async ({ page }) => {
    await page.fill('#email', 'not-an-email');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Email')).toBe('Invalid email address.');
  });

  test('email: accepts valid email', async ({ page }) => {
    await page.fill('#email', 'user@example.com');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Email')).toBe('');
  });

  test('url: rejects invalid URL', async ({ page }) => {
    await page.fill('#website', 'not-a-url');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Website')).toBe('Invalid URL.');
  });

  test('url: accepts valid URL', async ({ page }) => {
    await page.fill('#website', 'https://example.com');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Website')).toBe('');
  });

  test('phone: rejects invalid phone', async ({ page }) => {
    await page.fill('#phone', 'abc-not-phone');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Phone')).toBe('Invalid phone number.');
  });

  test('phone: accepts valid phone', async ({ page }) => {
    await page.fill('#phone', '(555) 123-4567');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Phone')).toBe('');
  });

  test('creditcard: rejects invalid number', async ({ page }) => {
    await page.fill('#card', '1234567890123456');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Card')).toBe('Invalid credit card number.');
  });

  test('creditcard: accepts valid Visa', async ({ page }) => {
    await page.fill('#card', '4111111111111111');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Card')).toBe('');
  });

  test('equalto: rejects mismatched passwords', async ({ page }) => {
    await page.fill('#password', 'secret123');
    await page.fill('#confirm', 'different');
    await submitForm(page);
    expect(await getFieldMessage(page, 'ConfirmPassword')).toBe('Passwords must match.');
  });

  test('equalto: accepts matching passwords', async ({ page }) => {
    await page.fill('#password', 'secret123');
    await page.fill('#confirm', 'secret123');
    await submitForm(page);
    expect(await getFieldMessage(page, 'ConfirmPassword')).toBe('');
  });

  test('fileextensions: rejects disallowed extension', async ({ page }) => {
    await page.fill('#avatar', 'script.exe');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Avatar')).toBe('Only image files are allowed.');
  });

  test('fileextensions: accepts allowed extension', async ({ page }) => {
    await page.fill('#avatar', 'photo.png');
    await submitForm(page);
    expect(await getFieldMessage(page, 'Avatar')).toBe('');
  });
});

// ---------------------------------------------------------------------------
// Server-rendered sibling cleanup (Blazor SSR compatibility)
// ---------------------------------------------------------------------------

test.describe('server-rendered message siblings', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/server-rendered-messages.html');
  });

  test('sibling server errors are visible on initial load', async ({ page }) => {
    // Both server-rendered error divs should be present
    const nameMessages = page.locator('div.validation-message').filter({
      has: page.locator('text=Name'),
    });
    // "Name is required." and "Name must be at least 2 characters."
    const allNameDivs = page.locator('#test-form > div:first-child > div.validation-message');
    expect(await allNameDivs.count()).toBe(2);
  });

  test('JS clears sibling server errors when field becomes valid', async ({ page }) => {
    // Fill the name field to make it valid
    await page.fill('#name', 'Alice');
    // Trigger change to revalidate
    await page.locator('#email').focus();

    // The data-valmsg-for div should be cleared (empty text)
    const msgForDiv = page.locator('[data-valmsg-for="Name"]');
    expect(await msgForDiv.textContent()).toBe('');

    // The sibling div (without data-valmsg-for) should be removed
    const allNameDivs = page.locator('#test-form > div:first-child > div.validation-message');
    expect(await allNameDivs.count()).toBe(1);
  });

  test('JS replaces server error with client error on submit', async ({ page }) => {
    // Clear the pre-filled server content and submit to get client-side error
    await page.fill('#name', '');

    await page.evaluate(() =>
      new Promise<void>(resolve => {
        document.addEventListener('validationcomplete', () => resolve(), { once: true });
        (document.querySelector('button[type="submit"]') as HTMLElement).click();
      })
    );

    // The data-valmsg-for div should now have the client error
    const msgForDiv = page.locator('[data-valmsg-for="Name"]');
    expect(await msgForDiv.textContent()).toBe('Name is required.');

    // The sibling div should be removed
    const allNameDivs = page.locator('#test-form > div:first-child > div.validation-message');
    expect(await allNameDivs.count()).toBe(1);
  });

  test('field with single server error has no siblings to remove', async ({ page }) => {
    // Email has only one server error — no sibling cleanup needed
    const emailDivs = page.locator('#test-form > div:nth-child(2) > div.validation-message');
    expect(await emailDivs.count()).toBe(1);

    // Fill to clear, then check div is still there (just empty)
    await page.fill('#email', 'user@example.com');
    await page.locator('#name').focus();

    expect(await emailDivs.count()).toBe(1);
    const msgForDiv = page.locator('[data-valmsg-for="Email"]');
    expect(await msgForDiv.textContent()).toBe('');
  });
});

// ---------------------------------------------------------------------------
// Radio group validation
// ---------------------------------------------------------------------------

test.describe('radio group validation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/radio-group.html');
  });

  test('required radio group shows error when none selected', async ({ page }) => {
    await submitForm(page);
    expect(await getFieldMessage(page, 'Color')).toBe('Please select a color.');
  });

  test('required radio group clears error when one is selected', async ({ page }) => {
    await submitForm(page);
    expect(await getFieldMessage(page, 'Color')).toBe('Please select a color.');

    // Select a radio button
    await page.locator('input[name="Color"][value="green"]').click();
    await submitForm(page);
    expect(await getFieldMessage(page, 'Color')).toBe('');
  });

  test('all radios have data-val but validation works correctly', async ({ page }) => {
    const radioCount = await page.evaluate(() =>
      document.querySelectorAll('input[type="radio"][name="Color"][data-val="true"]').length
    );
    expect(radioCount).toBe(3);

    // Validation still works correctly (group treated as one unit)
    await submitForm(page);
    expect(await getFieldMessage(page, 'Color')).toBe('Please select a color.');

    await page.locator('input[name="Color"][value="red"]').click();
    await submitForm(page);
    expect(await getFieldMessage(page, 'Color')).toBe('');
  });
});
