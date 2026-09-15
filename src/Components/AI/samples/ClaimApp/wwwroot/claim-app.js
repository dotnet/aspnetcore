(() => {
    const themeKey = "components-ai-claim-theme";
    const supportedThemes = new Set(["light", "dark", "contrast"]);

    function getTheme() {
        const theme = localStorage.getItem(themeKey);
        return supportedThemes.has(theme) ? theme : "light";
    }

    function setTheme(theme) {
        if (supportedThemes.has(theme)) {
            localStorage.setItem(themeKey, theme);
        }
    }

    globalThis.claimApp = {
        getTheme,
        setTheme,
    };
})();
