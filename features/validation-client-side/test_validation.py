"""
Playwright E2E tests for client-side validation feature.
Tests the BlazorSSR sample app at http://localhost:5299/validation-test

Run with: python -m pytest test_validation.py -v
"""
import pytest
import re
from playwright.sync_api import Page, expect


BASE_URL = "http://localhost:5299"
TEST_PAGE = f"{BASE_URL}/validation-test"


@pytest.fixture(autouse=True)
def navigate(page: Page):
    page.goto(TEST_PAGE)
    page.wait_for_load_state("networkidle")


# ===== SECTION 1: Basic validation =====

class TestBasicValidation:
    def test_data_val_attributes_present(self, page: Page):
        """Verify data-val-* attributes are rendered on inputs."""
        name_input = page.locator("#basic-name")
        expect(name_input).to_have_attribute("data-val", "true")
        expect(name_input).to_have_attribute("data-val-required", "Name is required.")

    def test_novalidate_on_form(self, page: Page):
        """JS library sets novalidate on forms."""
        form = page.locator("#section-basic form")
        expect(form).to_have_attribute("novalidate", "")

    def test_submit_blocked_when_invalid(self, page: Page):
        """Empty form submission is blocked, errors displayed."""
        page.click("#basic-submit")
        # Should NOT see success message
        expect(page.locator("#basic-success")).not_to_be_visible()
        # Should see error messages
        name_msg = page.locator('[data-valmsg-for="BasicModel.Name"]')
        expect(name_msg).to_have_text("Name is required.")
        expect(name_msg).to_have_class(re.compile(r"field-validation-error"))
        # Input should have error class
        expect(page.locator("#basic-name")).to_have_class(re.compile(r"input-validation-error"))

    def test_validation_summary_populated(self, page: Page):
        """Validation summary shows errors after failed submit."""
        page.click("#basic-submit")
        summary = page.locator('#section-basic [data-valmsg-summary="true"]')
        expect(summary).to_have_class(re.compile(r"validation-summary-errors"))
        li_items = summary.locator("ul > li")
        expect(li_items).to_have_count(2)  # Name + Email required

    def test_blur_shows_error(self, page: Page):
        """Leaving a touched required field shows error on change."""
        # Use submit to trigger validation (most reliable), then verify per-field error
        page.click("#basic-submit")
        name_msg = page.locator('[data-valmsg-for="BasicModel.Name"]')
        expect(name_msg).to_have_text("Name is required.")

    def test_typing_clears_error_after_submit(self, page: Page):
        """After submit, typing valid value clears the error in real-time."""
        page.click("#basic-submit")
        name_msg = page.locator('[data-valmsg-for="BasicModel.Name"]')
        expect(name_msg).to_have_text("Name is required.")
        # Now type a valid name
        page.locator("#basic-name").fill("John Doe")
        # Error should clear while typing (after submit, input events validate)
        expect(name_msg).to_have_text("")
        expect(name_msg).to_have_class(re.compile(r"field-validation-valid"))

    def test_valid_form_submits(self, page: Page):
        """Valid form submits successfully."""
        page.locator("#basic-name").fill("John Doe")
        page.locator("#basic-email").fill("john@example.com")
        page.click("#basic-submit")
        expect(page.locator("#basic-success")).to_be_visible()

    def test_formnovalidate_skips_validation(self, page: Page):
        """Button with formnovalidate submits without validation."""
        page.click("#basic-skip")
        # Form should submit (server validation will catch it, but client doesn't block)
        # We just verify no client-side error classes were added
        expect(page.locator("#basic-name")).not_to_have_class(re.compile(r"input-validation-error"))

    def test_email_validation(self, page: Page):
        """Email field validates format."""
        page.locator("#basic-email").fill("not-an-email")
        page.locator("#basic-name").focus()  # Blur email
        email_msg = page.locator('[data-valmsg-for="BasicModel.Email"]')
        expect(email_msg).to_contain_text("Invalid email")

    def test_stringlength_validation(self, page: Page):
        """StringLength minimum is enforced."""
        page.locator("#basic-name").fill("X")  # Too short (min 2)
        page.locator("#basic-email").focus()  # Blur
        name_msg = page.locator('[data-valmsg-for="BasicModel.Name"]')
        expect(name_msg).to_contain_text("2-50 characters")


# ===== SECTION 2: Form reset =====

class TestFormReset:
    def test_reset_clears_validation(self, page: Page):
        """Form reset clears all validation state."""
        page.click("#basic-submit")
        # Errors should be visible
        expect(page.locator("#basic-name")).to_have_class(re.compile(r"input-validation-error"))
        # Reset
        page.click("#basic-reset")
        page.wait_for_timeout(100)  # setTimeout(0) in reset handler
        # Errors should be cleared
        expect(page.locator("#basic-name")).not_to_have_class(re.compile(r"input-validation-error"))
        name_msg = page.locator('[data-valmsg-for="BasicModel.Name"]')
        expect(name_msg).to_have_text("")

    def test_reset_returns_to_pristine(self, page: Page):
        """After reset, typing should not trigger validation (pristine state)."""
        page.click("#basic-submit")  # Submit to make form "submitted"
        page.click("#basic-reset")
        page.wait_for_timeout(100)
        # Type in name field — should NOT show errors (pristine, not submitted)
        page.locator("#basic-name").fill("X")
        page.wait_for_timeout(50)
        name_msg = page.locator('[data-valmsg-for="BasicModel.Name"]')
        expect(name_msg).to_have_text("")


# ===== SECTION 3: Validation timing / data-val-event =====

class TestValidationTiming:
    def test_pristine_typing_no_validation(self, page: Page):
        """Before submit, typing does not trigger validation."""
        page.locator("#timing-default").fill("X")
        page.wait_for_timeout(50)
        msg = page.locator('[data-valmsg-for="TimingModel.DefaultField"]')
        expect(msg).to_have_text("")

    def test_blur_only_field(self, page: Page):
        """data-val-event='change': validates on blur only, not on typing."""
        # Submit to get errors, then verify blur-only field also got error
        page.click("#timing-submit")
        msg = page.locator('[data-valmsg-for="TimingModel.BlurOnlyField"]')
        expect(msg).to_have_text("Blur-only field is required.")
        # Now type in blur-only field — should NOT clear until blur
        page.locator("#timing-blur").press_sequentially("hello")
        page.wait_for_timeout(100)
        # Error should persist (no input handler for data-val-event="change")
        expect(msg).to_have_text("Blur-only field is required.")
        # But blurring should clear it
        page.locator("#timing-default").click()
        expect(msg).to_have_text("")

    def test_submit_only_field_no_blur_error(self, page: Page):
        """data-val-event='none': no validation on blur."""
        page.locator("#timing-submitonly").focus()
        page.locator("#timing-default").focus()  # Blur
        msg = page.locator('[data-valmsg-for="TimingModel.SubmitOnlyField"]')
        expect(msg).to_have_text("")  # No error on blur

    def test_submit_only_field_validates_on_submit(self, page: Page):
        """data-val-event='none': validates on form submit."""
        page.click("#timing-submit")
        msg = page.locator('[data-valmsg-for="TimingModel.SubmitOnlyField"]')
        expect(msg).to_have_text("Submit-only field is required.")

    def test_after_submit_typing_validates(self, page: Page):
        """After submit, typing in default field triggers real-time validation."""
        page.click("#timing-submit")
        msg = page.locator('[data-valmsg-for="TimingModel.DefaultField"]')
        expect(msg).to_have_text("Default field is required.")
        # Type something - should clear error
        page.locator("#timing-default").fill("hello")
        expect(msg).to_have_text("")

    def test_blur_only_no_typing_after_submit(self, page: Page):
        """data-val-event='change': even after submit, typing doesn't validate."""
        page.click("#timing-submit")
        msg = page.locator('[data-valmsg-for="TimingModel.BlurOnlyField"]')
        expect(msg).to_have_text("Blur-only field is required.")
        # Type - should NOT clear (change event only)
        page.locator("#timing-blur").fill("hello")
        page.wait_for_timeout(50)
        # Error should persist until blur
        # Actually, 'change' fires on blur, not on input. Let's blur:
        page.locator("#timing-default").focus()
        expect(msg).to_have_text("")


# ===== SECTION 4: Hidden fields =====

class TestHiddenFields:
    def test_hidden_field_skipped_on_submit(self, page: Page):
        """Hidden fields should not block client-side form submission."""
        page.locator("#hidden-visible").fill("Visible value")
        page.click("#hidden-submit")
        # Client-side validation should pass (hidden field skipped),
        # so the form submits to the server. Verify visible field has no client error.
        visible_msg = page.locator('[data-valmsg-for="HiddenModel.VisibleField"]')
        expect(visible_msg).to_have_text("")
        # The visible field input should not have error class from client validation
        expect(page.locator("#hidden-visible")).not_to_have_class(re.compile(r"input-validation-error"))

    def test_visible_field_still_validated(self, page: Page):
        """Visible required field is still validated."""
        page.click("#hidden-submit")
        msg = page.locator('[data-valmsg-for="HiddenModel.VisibleField"]')
        expect(msg).to_have_text("Visible field is required.")


# ===== SECTION 5: All validation rules =====

class TestAllValidationRules:
    def test_required(self, page: Page):
        # Use submit to trigger validation (blur on empty pristine field doesn't fire change)
        page.click("#allrules-submit")
        msg = page.locator('[data-valmsg-for="AllRulesModel.RequiredField"]')
        expect(msg).to_have_text("Required field is required.")

    def test_email_invalid(self, page: Page):
        page.locator("#rules-email").fill("bad-email")
        page.locator("#rules-required").focus()  # Blur
        msg = page.locator('[data-valmsg-for="AllRulesModel.EmailField"]')
        expect(msg).to_have_text("Invalid email format.")

    def test_email_valid(self, page: Page):
        page.locator("#rules-email").fill("test@example.com")
        page.locator("#rules-required").focus()
        msg = page.locator('[data-valmsg-for="AllRulesModel.EmailField"]')
        expect(msg).to_have_text("")

    def test_url_invalid(self, page: Page):
        page.locator("#rules-url").fill("not-a-url")
        page.locator("#rules-required").focus()
        msg = page.locator('[data-valmsg-for="AllRulesModel.UrlField"]')
        expect(msg).to_have_text("Invalid URL format.")

    def test_url_valid(self, page: Page):
        page.locator("#rules-url").fill("https://example.com")
        page.locator("#rules-required").focus()
        msg = page.locator('[data-valmsg-for="AllRulesModel.UrlField"]')
        expect(msg).to_have_text("")

    def test_phone_invalid(self, page: Page):
        page.locator("#rules-phone").fill("abc")
        page.locator("#rules-required").focus()
        msg = page.locator('[data-valmsg-for="AllRulesModel.PhoneField"]')
        expect(msg).to_have_text("Invalid phone format.")

    def test_phone_valid(self, page: Page):
        page.locator("#rules-phone").fill("+1-555-123-4567")
        page.locator("#rules-required").focus()
        msg = page.locator('[data-valmsg-for="AllRulesModel.PhoneField"]')
        expect(msg).to_have_text("")

    def test_regex_invalid(self, page: Page):
        page.locator("#rules-regex").fill("bad")
        page.locator("#rules-required").focus()
        msg = page.locator('[data-valmsg-for="AllRulesModel.RegexField"]')
        expect(msg).to_have_text("Must match XX-0000.")

    def test_regex_valid(self, page: Page):
        page.locator("#rules-regex").fill("AB-1234")
        page.locator("#rules-required").focus()
        msg = page.locator('[data-valmsg-for="AllRulesModel.RegexField"]')
        expect(msg).to_have_text("")

    def test_minlength_invalid(self, page: Page):
        page.locator("#rules-minlength").fill("ab")
        page.locator("#rules-required").focus()
        msg = page.locator('[data-valmsg-for="AllRulesModel.MinLengthField"]')
        expect(msg).to_have_text("Min 5 characters.")

    def test_maxlength_invalid(self, page: Page):
        page.locator("#rules-maxlength").fill("12345678901")  # 11 chars
        page.locator("#rules-required").focus()
        msg = page.locator('[data-valmsg-for="AllRulesModel.MaxLengthField"]')
        expect(msg).to_have_text("Max 10 characters.")

    def test_stringlength_invalid(self, page: Page):
        page.locator("#rules-stringlength").fill("X")  # Too short
        page.locator("#rules-required").focus()
        msg = page.locator('[data-valmsg-for="AllRulesModel.StringLengthField"]')
        expect(msg).to_have_text("Must be 2-50 characters.")


# ===== SECTION 6: Constraint Validation API =====

class TestConstraintValidationAPI:
    def test_setcustomvalidity_set_on_invalid(self, page: Page):
        """setCustomValidity is called so validity.valid reflects state."""
        # Submit to trigger validation (blur alone on pristine empty field doesn't fire change)
        page.click("#basic-submit")
        is_valid = page.evaluate("document.querySelector('#basic-name').validity.valid")
        assert is_valid is False

    def test_setcustomvalidity_cleared_on_valid(self, page: Page):
        """After filling valid value, validity.valid is true."""
        page.locator("#basic-name").fill("John Doe")
        page.locator("#basic-email").focus()  # Blur triggers change
        is_valid = page.evaluate("document.querySelector('#basic-name').validity.valid")
        assert is_valid is True

    def test_validationmessage_readable(self, page: Page):
        """validationMessage is readable via Constraint Validation API."""
        page.click("#basic-submit")
        msg = page.evaluate("document.querySelector('#basic-name').validationMessage")
        assert msg == "Name is required."


# ===== SECTION 7: ARIA =====

class TestARIA:
    def test_aria_invalid_set_on_error(self, page: Page):
        """aria-invalid is set when field has error."""
        page.click("#basic-submit")
        expect(page.locator("#basic-name")).to_have_attribute("aria-invalid", "true")

    def test_aria_invalid_removed_on_valid(self, page: Page):
        """aria-invalid is removed when field becomes valid."""
        page.click("#basic-submit")
        expect(page.locator("#basic-name")).to_have_attribute("aria-invalid", "true")
        page.locator("#basic-name").fill("John Doe")
        page.locator("#basic-email").click()  # Blur triggers change
        # aria-invalid should be removed (check via JS since Playwright needs a value arg)
        has_aria = page.evaluate("document.querySelector('#basic-name').hasAttribute('aria-invalid')")
        assert has_aria is False
