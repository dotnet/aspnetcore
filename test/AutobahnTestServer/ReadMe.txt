This test server is for use in testing server side implementations of the WebSocekt protocol. It should work with Helios, WebListener (native),
and the managed implementation of WebSockets in this project (running on WebListener, Kestrel, etc.). The tests only require that the server implement
a basic WebSocket accept and then echo any content received.

See http://autobahn.ws/ to download and install the test framework.

Usage:
Configure and start the server of your choice.
Run the test client:
"C:\Program Files\Python\2.7.6\Scripts\wstest.exe" -d -m fuzzingclient -s fuzzingclient.json
Where fussingclient.json contains:
{
   "options": {"failByDrop": false},
   "outdir": "./reports/servers",

   "servers": [
                {"agent": "NameOfImplementationBeingTested",
                 "url": "ws://localhost:12345",
                 "options": {"version": 18}}
              ],

   "cases": ["*"],
   "exclude-cases": [],
   "exclude-agent-cases": {}
}