// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

var MVC = (function () {
    // Takes the data which needs to be converted to form-url encoded format understadable by MVC.
    // This does not depend on jQuery. Can be used independently.
    var _stringify = function (data) {
        // This holds the stringified result.
        var result = "";

        if (typeof data !== "object")
        {
            return result;
        }

        for (var element in data) {
            if (data.hasOwnProperty(element)) {
                result += process(element, data[element]);
            }
        }

        // An '&' is appended at the end. Removing it.
        return result.substring(0, result.length - 1);
    }

    function process(key, value, prefix) {
        // Ignore functions.
        if (typeof value === "function") {
            return;
        }

        if (Object.prototype.toString.call(value) === '[object Array]') {
            var result = "";
            for (var i = 0; i < value.length; i++) {
                var tempPrefix = (prefix || key) + "[" + i + "]";
                result += process(key, value[i], tempPrefix);
            }

            return result;
        }
        else if (typeof value === "object") {
            var result = "";
            for (var prop in value) {
                // This is to prevent looping through inherited proeprties.
                if (value.hasOwnProperty(prop)) {
                    var tempPrefix = (prefix || key) + "." + prop;
                    result += process(prop, value[prop], tempPrefix);
                }
            }

            return result;
        }
        else {
            return encodeURIComponent(prefix || key) + "=" + encodeURIComponent(value) + "&";
        }
    }

    return {
        // Converts a Json object into MVC understandable format
        // when submitted as form-url-encoded data.
        stringify: _stringify
    };
})()