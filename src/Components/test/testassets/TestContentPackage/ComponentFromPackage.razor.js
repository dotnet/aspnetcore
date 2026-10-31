export function displayMessage(message) {
    const element = document.createElement("p");
    element.innerText = message;
    document.querySelector('.js-module-message').appendChild(element);
}
