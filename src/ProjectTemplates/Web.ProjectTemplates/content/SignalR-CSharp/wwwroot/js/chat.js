var chatConnection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

chatConnection.on("Send", function (message) {
    var li = document.createElement("li");
    li.textContent = message;
    document.getElementById("messagesList").appendChild(li);
});

document.getElementById("groupmsg").addEventListener("click", async (event) => {
    var groupName = document.getElementById("group-name").value;
    var groupMsg = document.getElementById("group-message-text").value;
    try {
        await chatConnection.invoke("SendMessageToGroup", groupName, groupMsg);
    }
    catch (e) {
        console.error(e.toString());
    }
    event.preventDefault();
});

document.getElementById("join-group").addEventListener("click", async (event) => {
    var groupName = document.getElementById("group-name").value;
    try {
        await chatConnection.invoke("AddToGroup", groupName);
    }
    catch (e) {
        console.error(e.toString());
    }
    event.preventDefault();
});

document.getElementById("leave-group").addEventListener("click", async (event) => {
    var groupName = document.getElementById("group-name").value;
    try {
        await chatConnection.invoke("RemoveFromGroup", groupName);
    }
    catch (e) {
        console.error(e.toString());
    }
    event.preventDefault();
});

chatConnection.start().then(function () {
    // do your own post-connection-started work here
}).catch(function (err) {
    return console.error(err.toString());
});