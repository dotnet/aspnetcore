This test server is for use in testing client side implementations of the WebSocekt protocol. It is currently implemented to test 
Microsoft.AspNetCore.WebSockets.Client.WebSocketClient and System.Net.WebSockets.ClientWebSocket.

See http://autobahn.ws/ to download and install the test framework.

Usage:
Run the test server:
"C:\Program Files\Python\2.7.6\Scripts\wstest" -d -m fuzzingserver -s fuzzingserver.json
Where fuzzingserver.json contains the following:

{
   "url": "ws://127.0.0.1:9001",

   "options": {"failByDrop": false},
   "outdir": "./reports/clients",
   "webport": 8080,

   "cases": ["*"],
   "exclude-cases": [],
   "exclude-agent-cases": {}
}

Then run the client of your choice, taking care to update the serverAddress and agent fields in the client code.