using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Net.WebSockets;
using System.Threading;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace WebAssembly.Net.Debugging {

	class DevToolsQueue {
		Task current_send;
		List<byte []> pending;

		public WebSocket Ws { get; private set; }
		public Task CurrentSend { get { return current_send; } }
		public DevToolsQueue (WebSocket sock)
		{
			this.Ws = sock;
			pending = new List<byte []> ();
		}

		public Task Send (byte [] bytes, CancellationToken token)
		{
			pending.Add (bytes);
			if (pending.Count == 1) {
				if (current_send != null)
					throw new Exception ("current_send MUST BE NULL IF THERE'S no pending send");
				//logger.LogTrace ("sending {0} bytes", bytes.Length);
				current_send = Ws.SendAsync (new ArraySegment<byte> (bytes), WebSocketMessageType.Text, true, token);
				return current_send;
			}
			return null;
		}

		public Task Pump (CancellationToken token)
		{
			current_send = null;
			pending.RemoveAt (0);

			if (pending.Count > 0) {
				if (current_send != null)
					throw new Exception ("current_send MUST BE NULL IF THERE'S no pending send");

				current_send = Ws.SendAsync (new ArraySegment<byte> (pending [0]), WebSocketMessageType.Text, true, token);
				return current_send;
			}
			return null;
		}
	}

	internal class DevToolsProxy {
		TaskCompletionSource<bool> side_exception = new TaskCompletionSource<bool> ();
		TaskCompletionSource<bool> client_initiated_close = new TaskCompletionSource<bool> ();
		Dictionary<MessageId, TaskCompletionSource<Result>> pending_cmds = new Dictionary<MessageId, TaskCompletionSource<Result>> ();
		ClientWebSocket browser;
		WebSocket ide;
		int next_cmd_id;
		List<Task> pending_ops = new List<Task> ();
		List<DevToolsQueue> queues = new List<DevToolsQueue> ();

		protected readonly ILogger logger;

		public DevToolsProxy (ILoggerFactory loggerFactory)
		{
			logger = loggerFactory.CreateLogger<DevToolsProxy>();
		}

		protected virtual Task<bool> AcceptEvent (SessionId sessionId, string method, JObject args, CancellationToken token)
		{
			return Task.FromResult (false);
		}

		protected virtual Task<bool> AcceptCommand (MessageId id, string method, JObject args, CancellationToken token)
		{
			return Task.FromResult (false);
		}

		async Task<string> ReadOne (WebSocket socket, CancellationToken token)
		{
			byte [] buff = new byte [4000];
			var mem = new MemoryStream ();
			while (true) {

				if (socket.State != WebSocketState.Open) {
					Log ("error", $"DevToolsProxy: Socket is no longer open.");
					client_initiated_close.TrySetResult (true);
					return null;
				}

				var result = await socket.ReceiveAsync (new ArraySegment<byte> (buff), token);
				if (result.MessageType == WebSocketMessageType.Close) {
					client_initiated_close.TrySetResult (true);
					return null;
				}

				mem.Write (buff, 0, result.Count);

				if (result.EndOfMessage)
					return Encoding.UTF8.GetString (mem.GetBuffer (), 0, (int)mem.Length);
			}
		}

		DevToolsQueue GetQueueForSocket (WebSocket ws)
		{
			return queues.FirstOrDefault (q => q.Ws == ws);
		}

		DevToolsQueue GetQueueForTask (Task task)
		{
			return queues.FirstOrDefault (q => q.CurrentSend == task);
		}

		void Send (WebSocket to, JObject o, CancellationToken token)
		{
			var sender = browser == to ? "Send-browser" : "Send-ide";

			var method = o ["method"]?.ToString ();
			//if (method != "Debugger.scriptParsed" && method != "Runtime.consoleAPICalled")
				Log ("protocol", $"{sender}: " + JsonConvert.SerializeObject (o));
			var bytes = Encoding.UTF8.GetBytes (o.ToString ());

			var queue = GetQueueForSocket (to);

			var task = queue.Send (bytes, token);
			if (task != null)
				pending_ops.Add (task);
		}

		async Task OnEvent (SessionId sessionId, string method, JObject args, CancellationToken token)
		{
			try {
				if (!await AcceptEvent (sessionId, method, args, token)) {
					//logger.LogDebug ("proxy browser: {0}::{1}",method, args);
					SendEventInternal (sessionId, method, args, token);
				}
			} catch (Exception e) {
				side_exception.TrySetException (e);
			}
		}

		async Task OnCommand (MessageId id, string method, JObject args, CancellationToken token)
		{
			try {
				if (!await AcceptCommand (id, method, args, token)) {
					var res = await SendCommandInternal (id, method, args, token);
					SendResponseInternal (id, res, token);
				}
			} catch (Exception e) {
				side_exception.TrySetException (e);
			}
		}

		void OnResponse (MessageId id, Result result)
		{
			//logger.LogTrace ("got id {0} res {1}", id, result);
			// Fixme
			if (pending_cmds.Remove (id, out var task)) {
				task.SetResult (result);
				return;
			}
			logger.LogError ("Cannot respond to command: {id} with result: {result} - command is not pending", id, result);
		}

		void ProcessBrowserMessage (string msg, CancellationToken token)
		{
			var res = JObject.Parse (msg);

			var method = res ["method"]?.ToString ();
			//if (method != "Debugger.scriptParsed" && method != "Runtime.consoleAPICalled")
				Log ("protocol", $"browser: {msg}");

			if (res ["id"] == null)
				pending_ops.Add (OnEvent (new SessionId (res ["sessionId"]?.Value<string> ()), res ["method"].Value<string> (), res ["params"] as JObject, token));
			else
				OnResponse (new MessageId (res ["sessionId"]?.Value<string> (), res ["id"].Value<int> ()), Result.FromJson (res));
		}

		void ProcessIdeMessage (string msg, CancellationToken token)
		{
			Log ("protocol", $"ide: {msg}");
			if (!string.IsNullOrEmpty (msg)) {
				var res = JObject.Parse (msg);
				pending_ops.Add (OnCommand (
						new MessageId (res ["sessionId"]?.Value<string> (), res ["id"].Value<int> ()),
						res ["method"].Value<string> (),
						res ["params"] as JObject, token));
			}
		}

		internal async Task<Result> SendCommand (SessionId id, string method, JObject args, CancellationToken token) {
			//Log ("verbose", $"sending command {method}: {args}");
			return await SendCommandInternal (id, method, args, token);
		}

		Task<Result> SendCommandInternal (SessionId sessionId, string method, JObject args, CancellationToken token)
		{
			int id = Interlocked.Increment (ref next_cmd_id);

			var o = JObject.FromObject (new {
				id,
				method,
				@params = args
			});
			if (sessionId.sessionId != null)
				o["sessionId"] = sessionId.sessionId;
			var tcs = new TaskCompletionSource<Result> ();

			var msgId = new MessageId (sessionId.sessionId, id);
			//Log ("verbose", $"add cmd id {sessionId}-{id}");
			pending_cmds[msgId] = tcs;

			Send (this.browser, o, token);
			return tcs.Task;
		}

		public void SendEvent (SessionId sessionId, string method, JObject args, CancellationToken token)
		{
			//Log ("verbose", $"sending event {method}: {args}");
			SendEventInternal (sessionId, method, args, token);
		}

		void SendEventInternal (SessionId sessionId, string method, JObject args, CancellationToken token)
		{
			var o = JObject.FromObject (new {
				method,
				@params = args
			});
			if (sessionId.sessionId != null)
				o["sessionId"] = sessionId.sessionId;

			Send (this.ide, o, token);
		}

		internal void SendResponse (MessageId id, Result result, CancellationToken token)
		{
			SendResponseInternal (id, result, token);
		}

		void SendResponseInternal (MessageId id, Result result, CancellationToken token)
		{
			JObject o = result.ToJObject (id);
			if (result.IsErr)
				logger.LogError ($"sending error response for id: {id} -> {result}");

			Send (this.ide, o, token);
		}

		// , HttpContext context)
		public async Task Run (Uri browserUri, WebSocket ideSocket)
		{
			Log ("info", $"DevToolsProxy: Starting on {browserUri}");
			using (this.ide = ideSocket) {
				Log ("verbose", $"DevToolsProxy: IDE waiting for connection on {browserUri}");
				queues.Add (new DevToolsQueue (this.ide));
				using (this.browser = new ClientWebSocket ()) {
					this.browser.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;
					await this.browser.ConnectAsync (browserUri, CancellationToken.None);
					queues.Add (new DevToolsQueue (this.browser));

					Log ("verbose", $"DevToolsProxy: Client connected on {browserUri}");
					var x = new CancellationTokenSource ();

					pending_ops.Add (ReadOne (browser, x.Token));
					pending_ops.Add (ReadOne (ide, x.Token));
					pending_ops.Add (side_exception.Task);
					pending_ops.Add (client_initiated_close.Task);

					try {
						while (!x.IsCancellationRequested) {
							var task = await Task.WhenAny (pending_ops.ToArray ());
							//logger.LogTrace ("pump {0} {1}", task, pending_ops.IndexOf (task));
							if (task == pending_ops [0]) {
								var msg = ((Task<string>)task).Result;
								if (msg != null) {
									pending_ops [0] = ReadOne (browser, x.Token); //queue next read
									ProcessBrowserMessage (msg, x.Token);
								}
							} else if (task == pending_ops [1]) {
								var msg = ((Task<string>)task).Result;
								if (msg != null) {
									pending_ops [1] = ReadOne (ide, x.Token); //queue next read
									ProcessIdeMessage (msg, x.Token);
								}
							} else if (task == pending_ops [2]) {
								var res = ((Task<bool>)task).Result;
								throw new Exception ("side task must always complete with an exception, what's going on???");
							} else if (task == pending_ops [3]) {
								var res = ((Task<bool>)task).Result;
								Log ("verbose", $"DevToolsProxy: Client initiated close from {browserUri}");
								x.Cancel ();
							} else {
								//must be a background task
								pending_ops.Remove (task);
								var queue = GetQueueForTask (task);
								if (queue != null) {
									var tsk = queue.Pump (x.Token);
									if (tsk != null)
										pending_ops.Add (tsk);
								}
							}
						}
					} catch (Exception e) {
						Log ("error", $"DevToolsProxy::Run: Exception {e}");
						//throw;
					} finally {
						if (!x.IsCancellationRequested)
							x.Cancel ();
					}
				}
			}
		}

		protected void Log (string priority, string msg)
		{
			switch (priority) {
			case "protocol":
				//logger.LogTrace (msg);
				break;
			case "verbose":
				//logger.LogDebug (msg);
				break;
			case "info":
			case "warning":
			case "error":
			default:
				logger.LogDebug (msg);
				break;
			}
		}
	}
}
