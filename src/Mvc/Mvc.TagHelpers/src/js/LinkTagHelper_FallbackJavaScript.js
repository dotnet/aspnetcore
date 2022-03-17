// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
(
 /**
  * This function finds the previous element (assumed to be meta) and tests its current CSS style using the passed
  * values, to determine if a stylesheet was loaded. If not, this function loads the fallback stylesheet via
  * document.write.
  *
  * @param {string} cssTestPropertyName - The name of the CSS property to test.
  * @param {string} cssTestPropertyValue - The value to test the specified CSS property for.
  * @param {string[]} fallbackHrefs - The URLs to the stylesheets to load in the case the test fails.
  * @param {string} extraAttributes - The extra attributes string that should be included on the generated link tags.
  */
 function loadFallbackStylesheet(cssTestPropertyName, cssTestPropertyValue, fallbackHrefs, extraAttributes) {
    var doc = document,
        // Find the last script tag on the page which will be this one, as JS executes as it loads
        scriptElements = doc.getElementsByTagName("SCRIPT"),
        // Find the meta tag before this script tag, that's the element we're going to test the CSS property on
        meta = scriptElements[scriptElements.length - 1].previousElementSibling,
        // Get the current style of the meta tag starting with standards-based API and falling back to <=IE8 API
        metaStyle = (doc.defaultView && doc.defaultView.getComputedStyle) ? doc.defaultView.getComputedStyle(meta)
            : meta.currentStyle,
        i;

    if (metaStyle && metaStyle[cssTestPropertyName] !== cssTestPropertyValue) {
        for (i = 0; i < fallbackHrefs.length; i++) {
            doc.write('<link href="' + fallbackHrefs[i] + '" ' + extraAttributes + '/>');
        }
    }
})();