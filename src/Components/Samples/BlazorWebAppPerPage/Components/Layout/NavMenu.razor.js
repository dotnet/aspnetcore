// Handle navigation menu toggle
document.addEventListener("click", function (event) {
    if (event.target.closest?.("#nav-scrollable")) {
        document.querySelector(".navbar-toggler")?.click();
    }
});
