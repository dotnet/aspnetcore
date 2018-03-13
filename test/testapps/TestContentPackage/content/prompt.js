Blazor.registerFunction('TestContentPackage.showPrompt', function (message) {
    return prompt(message, "Type anything here");
});
