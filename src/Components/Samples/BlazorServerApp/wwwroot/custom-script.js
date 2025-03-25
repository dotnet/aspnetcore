"use strict";

window.getDocumentTitle = function () {
    return document.title;
}

window.setDocumentTitle = function (title) {
    document.title = title;
};

window.logDefault = function () {
    console.log("This is a default log message");
}

window.logMessage = function (message) {
    console.log(message);
}

window.testObject = {
    num: 10,
    text: "Hello World",
    log: function () {
        console.log(this.text);
    },
    get getOnlyProperty() {
        return this.num;
    },
    set setOnlyProperty(value) {
        this.num = value;
    }
}

window.invalidAccess = function () {
    window.testObject.getOnlyProperty = 20;
}

window.getTestObject = function () {
    return window.testObject;
}

window.Cat = class {
    constructor(name) {
        this.name = name;
    }

    meow() {
        const text = `${this.name} says Meow!`;
        console.log(text);
        return text;
    }
}

window.Dog = function (name) {
    this.name = name;
}

window.Dog.prototype.bark = function () {
    const text = `${this.name} says Woof!`;
    console.log(text);
    return text;
}
