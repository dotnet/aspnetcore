window.customElementFunctions = {
    updateComplexProperties: function (element, count) {
        element.complexTypeParam = {
            property: `Clicked ${count} times`,
        };
        element.callbackParam = () => {
            document.getElementById('message').innerText = `Callback with count = ${count}`;
        };
    }
};
