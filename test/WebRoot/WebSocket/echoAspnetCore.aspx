<%@ Page Language="C#" AutoEventWireUp="true" %>
<html>
<head>
<h1>#active websocket connection <%= "foo" %></h1>
<h1> Web Socket Echo Demo (run from Minefield!) </h1>
<script type="text/javascript">

var socket;
var receive_counter = 0; 
function initializeWebSocket() {

  var host = "ws://localhost:8080/websocketecho";

	try {
		socket = new WebSocket(host, "mywebsocketsubprotocol");

		socket.onopen = function(msg){
			var s = 'WebSocket Status:: Socket Open';
			document.getElementById("serverStatus").innerHTML = s;  
		};

		socket.onmessage = function(msg){
      receive_counter++;
			var s = 'Server Reply:: ' + receive_counter + ' : ' + msg.data.length + ' : ' + msg.data;
			document.getElementById("serverData").innerHTML = s; 
		};

		socket.onclose = function(msg){ 
			var s = 'WebSocket Status:: Socket Closed';
			document.getElementById("serverStatus").innerHTML = s;  
		};
	} 
	catch(ex)
  { 
    console.log(ex); 
  }
}

function send() {
	var e = document.getElementById("msgText");
  //var i = 0;
  //while (true)
  //{
   // i++;
    //if (i > 100) break;
    socket.send(e.value);    
  //}
}

initializeWebSocket();

</script>
<head>
<body>

<p id="serverStatus"> </p>
This text will be sent on the socket:  <input id="msgText" type=text size=30>

<input type=button value="Send" onclick="send()">
<p id="serverData" />
</body>
</html>