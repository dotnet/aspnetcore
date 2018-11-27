function getParameterByName(name, url) {
    if (!url) {
        url = window.location.href;
    }
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

function click(id, callback) {
    document.getElementById(id).addEventListener('click', function (event) {
        callback(event);
        event.preventDefault();
    });
}

function addLine(listId, line, color) {
    var child = document.createElement('li');
    if (color) {
        child.style.color = color;
    }
    child.innerText = line;
    document.getElementById(listId).appendChild(child);
}

