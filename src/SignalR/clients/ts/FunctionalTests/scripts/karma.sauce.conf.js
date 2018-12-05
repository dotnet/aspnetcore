try {
    // Karma configuration for a SauceLabs-based CI run.
    const createKarmaConfig = require("./karma.base.conf");

    // "Evergreen" Desktop Browsers
    var evergreenBrowsers = {
        // Microsoft Edge Latest, Windows 10
        sl_edge_win10: {
            base: "SauceLabs",
            browserName: "microsoftedge",
            version: "latest",
        },

        // Apple Safari Latest, macOS 10.13 (High Sierra)
        sl_safari_macOS1013: {
            base: "SauceLabs",
            browserName: "safari",
            version: "latest",
            platform: "macOS 10.13",
        },

        // Google Chrome Latest, any OS.
        sl_chrome: {
            base: "SauceLabs",
            browserName: "chrome",
            version: "latest",
        },

        // Mozilla Firefox Latest, any OS
        sl_firefox: {
            base: "SauceLabs",
            browserName: "firefox",
            version: "latest",
        },
    }

    // Legacy Browsers
    var legacyBrowsers = {
        // Microsoft Internet Explorer 11, Windows 7
        sl_ie11_win7: {
            base: "SauceLabs",
            browserName: "internet explorer",
            version: "11",
            platform: "Windows 7",
        },
    };

    // Mobile Browsers
    // These are a bit too slow and cause Karma to time out trying to "capture" the browser.
    var mobileBrowsers = {
        // // Latest iOS
        // sl_ios_safari: {
        //     base: "SauceLabs",
        //     browserName: "Safari",
        //     deviceName: "iPhone XS Simulator",
        //     platformName: "iOS",
        //     platformVersion: "12.0",
        // },
        // // Latest Android Chrome
        // sl_android_chrome: {
        //     base: "SauceLabs",
        //     browserName: "Chrome",
        //     platformName: "Android",
        //     platformVersion: "6.0",
        //     deviceName: "Android Emulator"
        // }
    };

    var customLaunchers = {
        ...evergreenBrowsers,
        ...legacyBrowsers,
        ...mobileBrowsers,
    };

    module.exports = createKarmaConfig({
        customLaunchers,
        browsers: Object.keys(customLaunchers),
        reporters: ["saucelabs"],
        sauceLabs: {
            testName: "SignalR Functional Tests",
            connectOptions: {
                // Required to enable WebSockets through the Sauce Connect proxy.
                noSslBumpDomains: ["all"]
            },
            build: process.env.BUILD_BUILDNUMBER,
            tags: ["aspnet-SignalR", "daily-tests"],
            username: process.env.SAUCE_USERNAME,
            accessKey: process.env.SAUCE_ACCESS_KEY
        },
    });
} catch (e) {
    console.error(e);
}