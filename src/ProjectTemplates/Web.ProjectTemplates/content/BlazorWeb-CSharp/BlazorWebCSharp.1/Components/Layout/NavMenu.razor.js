// Handle navigation menu toggle
const navScrollable = document.getElementById("nav-scrollable");
const navToggler = document.querySelector(".navbar-toggler");

if (navScrollable && navToggler) {
    navScrollable.addEventListener("click", function() {
        navToggler.click();
    });
}
