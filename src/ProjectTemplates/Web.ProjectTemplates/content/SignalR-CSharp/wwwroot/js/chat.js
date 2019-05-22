var connection = new signalR.HubConnectionBuilder()
    .withUrl("/chat")
    .withAutomaticReconnect()
    .build();

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

connection.onreconnecting((error) => {
    document.getElementById("leave-group").disabled = true;
    document.getElementById("join-group").disabled = true;
    document.getElementById("groupmsg").disabled = true;
  
    const li = document.createElement("li");
    li.textContent = `Connection lost due to error "${error}". Reconnecting.`;
    document.getElementById("messagesList").appendChild(li);
});

connection.onreconnected((connectionId) => {
    document.getElementById("leave-group").disabled = false;
    document.getElementById("join-group").disabled = false;
    document.getElementById("groupmsg").disabled = false;
  
    const li = document.createElement("li");
    li.textContent = `Connection reestablished. Connected.`;
    document.getElementById("messagesList").appendChild(li);
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

start();