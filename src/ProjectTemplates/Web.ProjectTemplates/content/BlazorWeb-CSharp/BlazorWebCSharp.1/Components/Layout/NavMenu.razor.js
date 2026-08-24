// The menu elements can be replaced during hydration, so resolve them at click time.
document.addEventListener("click", function(event) {
    if (event.target instanceof Element && event.target.closest("#nav-scrollable")) {
        document.querySelector(".navbar-toggler")?.click();
    }
});
