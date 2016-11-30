const ECHOENDPOINT_URL = `http://${document.location.host}/echo`;

function eachTransport(action) {
   let transportNames = ["webSockets", "serverSentEvents", "longPolling"];
   transportNames.forEach(t => action(t));
}

function fail() {
    it.expect(true).toBe(false);
}