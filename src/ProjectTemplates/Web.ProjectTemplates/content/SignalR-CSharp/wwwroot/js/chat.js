var connection = new signalR.HubConnectionBuilder().withUrl("/chat").build();

connection.on("Send", function (message) {
    var li = document.createElement("li");
    li.textContent = message;
    document.getElementById("messagesList").appendChild(li);
});

document.getElementById("groupmsg").addEventListener("click", async (event) => {
    var userName = document.getElementById("user-name").value;
    var groupName = document.getElementById("group-name").value;
    var groupMsg = document.getElementById("group-message-text").value;
    try {
        await connection.invoke("SendMessageToGroup", userName, groupName, groupMsg);
    }
    catch (e) {
        console.error(e.toString());
    }
    event.preventDefault();
});

document.getElementById("join-group").addEventListener("click", async (event) => {
    var userName = document.getElementById("user-name").value;
    var groupName = document.getElementById("group-name").value;
    try {
        await connection.invoke("AddToGroup", userName, groupName);
    }
    catch (e) {
        console.error(e.toString());
    }
    event.preventDefault();
});

document.getElementById("leave-group").addEventListener("click", async (event) => {
    var userName = document.getElementById("user-name").value;
    var groupName = document.getElementById("group-name").value;
    try {
        await connection.invoke("RemoveFromGroup", userName, groupName);
    }
    catch (e) {
        console.error(e.toString());
    }
    event.preventDefault();
});

async function start() {
    try {
        await connection.start();
        console.log("connected");
    } catch (err) {
        console.log(err);
        setTimeout(() => start(), 5000);
    }
};

connection.onclose(async () => {
    await start();
});

start();