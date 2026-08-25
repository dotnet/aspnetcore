const customElementName = "blazor-nav-menu";

if (!customElements.get(customElementName)) {
    customElements.define(customElementName, class BlazorNavMenuElement extends HTMLElement {
        connectedCallback() {
            this.addEventListener("click", this.closeMenu);
        }

        disconnectedCallback() {
            this.removeEventListener("click", this.closeMenu);
        }

        closeMenu(event) {
            if (!(event.target instanceof Element) || !event.target.closest(".nav-scrollable")) {
                return;
            }

            const navToggler = this.querySelector(".navbar-toggler");
            if (navToggler instanceof HTMLInputElement) {
                navToggler.checked = false;
            }
        }
    });
}
