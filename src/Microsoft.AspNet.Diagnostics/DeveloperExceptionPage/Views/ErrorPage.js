(function (window, undefined) {
    "use strict";

    function $(selector, element) {
        return new NodeCollection(selector, element);
    }

    function NodeCollection(selector, element) {
        this.items = [];
        element = element || window.document;

        var nodeList;

        if (typeof (selector) === "string") {
            nodeList = element.querySelectorAll(selector);
            for (var i = 0, l = nodeList.length; i < l; i++) {
                this.items.push(nodeList.item(i));
            }
        } else if (selector.tagName) {
            this.items.push(selector);
        } else if (selector.splice) {
            this.items = this.items.concat(selector);
        }
    }

    NodeCollection.prototype = {
        each: function (callback) {
            for (var i = 0, l = this.items.length; i < l; i++) {
                callback(this.items[i], i);
            }
            return this;
        },

        children: function (selector) {
            var children = [];

            this.each(function (el) {
                children = children.concat($(selector, el).items);
            });

            return $(children);
        },

        hide: function () {
            this.each(function (el) {
                el.style.display = "none";
            });

            return this;
        },

        toggle: function () {
            this.each(function (el) {
                el.style.display = el.style.display === "none" ? "" : "none";
            });

            return this;
        },

        show: function () {
            this.each(function (el) {
                el.style.display = "";
            });

            return this;
        },

        addClass: function (className) {
            this.each(function (el) {
                var existingClassName = el.className,
                    classNames;
                if (!existingClassName) {
                    el.className = className;
                } else {
                    classNames = existingClassName.split(" ");
                    if (classNames.indexOf(className) < 0) {
                        el.className = existingClassName + " " + className;
                    }
                }
            });

            return this;
        },

        removeClass: function (className) {
            this.each(function (el) {
                var existingClassName = el.className,
                    classNames, index;
                if (existingClassName === className) {
                    el.className = "";
                } else if (existingClassName) {
                    classNames = existingClassName.split(" ");
                    index = classNames.indexOf(className);
                    if (index > 0) {
                        classNames.splice(index, 1);
                        el.className = classNames.join(" ");
                    }
                }
            });

            return this;
        },

        toggleClass: function (className) {
            this.each(function (el) {
                var classNames = el.className.split(" ");
                if (classNames.indexOf(className) >= 0) {
                    $(el).removeClass(className);
                } else {
                    $(el).addClass(className);
                }
            });

            return this;
        },

        attr: function (name) {
            if (this.items.length === 0) {
                return null;
            }

            return this.items[0].getAttribute(name);
        },

        on: function (eventName, handler) {
            this.each(function (el, idx) {
                var callback = function (e) {
                    e = e || window.event;
                    if (!e.which && e.keyCode) {
                        e.which = e.keyCode; // Normalize IE8 key events
                    }
                    handler.apply(el, [e]);
                };

                if (el.addEventListener) { // DOM Events
                    el.addEventListener(eventName, callback, false);
                } else if (el.attachEvent) { // IE8 events
                    el.attachEvent("on" + eventName, callback);
                } else {
                    el["on" + type] = callback;
                }
            });

            return this;
        },

        click: function (handler) {
            return this.on("click", handler);
        },

        keypress: function (handler) {
            return this.on("keypress", handler);
        }
    };

    function frame(el) {
        $(el).children(".source .collapsible").toggle();
    }

    function tab(el) {
        var unselected = $("#header .selected").removeClass("selected").attr("id");
        var selected = $(el).addClass("selected").attr("id");

        $("#" + unselected + "page").hide();
        $("#" + selected + "page").show();
    }

    $(".collapsible").hide();
    $(".page").hide();
    $("#stackpage").show();

    $(".frame")
        .click(function () {
            frame(this);
        })
        .keypress(function (e) {
            if (e.which === 13) {
                frame(this);
            }
        });

    $("#header li")
        .click(function () {
            tab(this);
        })
        .keypress(function (e) {
            if (e.which === 13) {
                tab(this);
            }
        });
})(window);