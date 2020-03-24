using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;

namespace WebAssembly.Net.Debugging {

	internal class MonoCommands {
		public string expression { get; set; }
		public string objectGroup { get; set; } = "mono-debugger";
		public bool includeCommandLineAPI { get; set; } = false;
		public bool silent { get; set; } = false;
		public bool returnByValue { get; set; } = true;

		public MonoCommands (string expression)
			=> this.expression = expression;

		public static MonoCommands GetCallStack ()
			=> new MonoCommands ("MONO.mono_wasm_get_call_stack()");

		public static MonoCommands IsRuntimeReady ()
			=> new MonoCommands ("MONO.mono_wasm_runtime_is_ready");

		public static MonoCommands StartSingleStepping (StepKind kind)
			=> new MonoCommands ($"MONO.mono_wasm_start_single_stepping ({(int)kind})");

		public static MonoCommands GetLoadedFiles ()
			=> new MonoCommands ("MONO.mono_wasm_get_loaded_files()");

		public static MonoCommands ClearAllBreakpoints ()
			=> new MonoCommands ("MONO.mono_wasm_clear_all_breakpoints()");

		public static MonoCommands GetObjectProperties (int objectId)
			=> new MonoCommands ($"MONO.mono_wasm_get_object_properties({objectId})");

		public static MonoCommands GetArrayValues (int objectId)
			=> new MonoCommands ($"MONO.mono_wasm_get_array_values({objectId})");

		public static MonoCommands GetScopeVariables (int scopeId, params int[] vars)
			=> new MonoCommands ($"MONO.mono_wasm_get_variables({scopeId}, [ {string.Join (",", vars)} ])");

		public static MonoCommands SetBreakpoint (string assemblyName, int methodToken, int ilOffset)
			=> new MonoCommands ($"MONO.mono_wasm_set_breakpoint (\"{assemblyName}\", {methodToken}, {ilOffset})");

		public static MonoCommands RemoveBreakpoint (int breakpointId)
			=> new MonoCommands ($"MONO.mono_wasm_remove_breakpoint({breakpointId})");
	}

	internal enum MonoErrorCodes {
		BpNotFound = 100000,
	}

	internal class MonoConstants {
		public const string RUNTIME_IS_READY = "mono_wasm_runtime_ready";
	}

	class Frame {
		public Frame (MethodInfo method, SourceLocation location, int id)
		{
			this.Method = method;
			this.Location = location;
			this.Id = id;
		}

		public MethodInfo Method { get; private set; }
		public SourceLocation Location { get; private set; }
		public int Id { get; private set; }
	}

	class Breakpoint {
		public SourceLocation Location { get; private set; }
		public int RemoteId { get; set; }
		public BreakpointState State { get; set; }
		public string StackId { get; private set; }

		public static bool TryParseId (string stackId, out int id)
		{
			id = -1;
			if (stackId?.StartsWith ("dotnet:", StringComparison.Ordinal) != true)
				return false;

			return int.TryParse (stackId.Substring ("dotnet:".Length), out id);
		}

		public Breakpoint (string stackId, SourceLocation loc, BreakpointState state)
		{
			this.StackId = stackId;
			this.Location = loc;
			this.State = state;
		}
	}

	enum BreakpointState {
		Active,
		Disabled,
		Pending
	}

	enum StepKind {
		Into,
		Out,
		Over
	}

	internal class ExecutionContext {
		public string DebuggerId { get; set; }
		public Dictionary<string,BreakpointRequest> BreakpointRequests { get; } = new Dictionary<string,BreakpointRequest> ();

		public TaskCompletionSource<DebugStore> ready = null;
		public bool IsRuntimeReady => ready != null && ready.Task.IsCompleted;

		public int Id { get; set; }
		public object AuxData { get; set; }

		public List<Frame> CallStack { get; set; }

		internal DebugStore store;
		public TaskCompletionSource<DebugStore> Source { get; } = new TaskCompletionSource<DebugStore> ();

		public DebugStore Store {
			get {
				if (store == null || !Source.Task.IsCompleted)
					return null;

				return store;
			}
		}
	}

	internal class MonoProxy : DevToolsProxy {
		Dictionary<SessionId, ExecutionContext> contexts = new Dictionary<SessionId, ExecutionContext> ();

		public MonoProxy (ILoggerFactory loggerFactory) : base(loggerFactory) { }

		ExecutionContext GetContext (SessionId sessionId)
		{
			if (contexts.TryGetValue (sessionId, out var context))
				return context;

			throw new ArgumentException ($"Invalid Session: \"{sessionId}\"", nameof (sessionId));
		}

		bool UpdateContext (SessionId sessionId, ExecutionContext executionContext, out ExecutionContext previousExecutionContext)
		{
			var previous = contexts.TryGetValue (sessionId, out previousExecutionContext);
			contexts[sessionId] = executionContext;
			return previous;
		}

		internal Task<Result> SendMonoCommand (SessionId id, MonoCommands cmd, CancellationToken token)
			=> SendCommand (id, "Runtime.evaluate", JObject.FromObject (cmd), token);

		protected override async Task<bool> AcceptEvent (SessionId sessionId, string method, JObject args, CancellationToken token)
		{
			switch (method) {
			case "Runtime.consoleAPICalled": {
					var type = args["type"]?.ToString ();
					if (type == "debug") {
						if (args["args"]?[0]?["value"]?.ToString () == MonoConstants.RUNTIME_IS_READY && args["args"]?[1]?["value"]?.ToString () == "fe00e07a-5519-4dfe-b35a-f867dbaf2e28")
							await RuntimeReady (sessionId, token);
					}
					break;
				}

			case "Runtime.executionContextCreated": {
					SendEvent (sessionId, method, args, token);
					var ctx = args? ["context"];
					var aux_data = ctx? ["auxData"] as JObject;
					var id = ctx ["id"].Value<int> ();
					if (aux_data != null) {
						var is_default = aux_data ["isDefault"]?.Value<bool> ();
						if (is_default == true) {
							await OnDefaultContext (sessionId, new ExecutionContext { Id = id, AuxData = aux_data }, token);
						}
					}
					return true;
				}

			case "Debugger.paused": {
					//TODO figure out how to stich out more frames and, in particular what happens when real wasm is on the stack
					var top_func = args? ["callFrames"]? [0]? ["functionName"]?.Value<string> ();

					if (top_func == "mono_wasm_fire_bp" || top_func == "_mono_wasm_fire_bp") {
						return await OnBreakpointHit (sessionId, args, token);
					}
					break;
				}

			case "Debugger.breakpointResolved": {
					break;
				}

			case "Debugger.scriptParsed":{
					var url = args? ["url"]?.Value<string> () ?? "";

					switch (url) {
					case var _ when url == "":
					case var _ when url.StartsWith ("wasm://", StringComparison.Ordinal): {
							Log ("verbose", $"ignoring wasm: Debugger.scriptParsed {url}");
							return true;
						}
					}
					Log ("verbose", $"proxying Debugger.scriptParsed ({sessionId.sessionId}) {url} {args}");
					break;
				}
			}
			return false;
		}

		async Task<bool> IsRuntimeAlreadyReadyAlready (SessionId sessionId, CancellationToken token)
		{
			var res = await SendMonoCommand (sessionId, MonoCommands.IsRuntimeReady (), token);
			return res.Value? ["result"]? ["value"]?.Value<bool> () ?? false;
		}

		protected override async Task<bool> AcceptCommand (MessageId id, string method, JObject args, CancellationToken token)
		{
			switch (method) {
			case "Debugger.enable": {
					var resp = await SendCommand (id, method, args, token);

					GetContext (id).DebuggerId = resp.Value ["debuggerId"]?.ToString ();

					if (await IsRuntimeAlreadyReadyAlready (id, token))
						await RuntimeReady (id, token);

					SendResponse (id,resp,token);
					return true;
				}

			case "Debugger.getScriptSource": {
					var script = args? ["scriptId"]?.Value<string> ();
					return await OnGetScriptSource (id, script, token);
				}

			case "Runtime.compileScript": {
					var exp = args? ["expression"]?.Value<string> ();
					if (exp.StartsWith ("//dotnet:", StringComparison.Ordinal)) {
						OnCompileDotnetScript (id, token);
						return true;
					}
					break;
				}

			case "Debugger.getPossibleBreakpoints": {
					var resp = await SendCommand (id, method, args, token);
					if (resp.IsOk && resp.Value["locations"].HasValues) {
						SendResponse (id, resp, token);
						return true;
					}

					var start = SourceLocation.Parse (args? ["start"] as JObject);
					//FIXME support variant where restrictToFunction=true and end is omitted
					var end = SourceLocation.Parse (args? ["end"] as JObject);
					if (start != null && end != null && await GetPossibleBreakpoints (id, start, end, token))
							return true;

					SendResponse (id, resp, token);
					return true;
				}

			case "Debugger.setBreakpoint": {
					break;
				}

			case "Debugger.setBreakpointByUrl": {
					var context = GetContext (id);
					var resp = await SendCommand (id, method, args, token);
					if (!resp.IsOk) {
						SendResponse (id, resp, token);
						return true;
					}

					var bpid = resp.Value["breakpointId"]?.ToString ();
					var request = BreakpointRequest.Parse (bpid, args);
					context.BreakpointRequests[bpid] = request;
					if (await IsRuntimeAlreadyReadyAlready (id, token)) {
						var store = await RuntimeReady (id, token);

						Log ("verbose", $"BP req {args}");
						await SetBreakpoint (id, store, request, token);
					}

					SendResponse (id, Result.OkFromObject (request.AsSetBreakpointByUrlResponse()), token);
					return true;
				}

			case "Debugger.removeBreakpoint": {
					return await RemoveBreakpoint (id, args, token);
				}

			case "Debugger.resume": {
					await OnResume (id, token);
					break;
				}

			case "Debugger.stepInto": {
					return await Step (id, StepKind.Into, token);
				}

			case "Debugger.stepOut": {
					return await Step (id, StepKind.Out, token);
				}

			case "Debugger.stepOver": {
					return await Step (id, StepKind.Over, token);
				}

			case "Runtime.getProperties": {
					var objId = args? ["objectId"]?.Value<string> ();
					if (objId.StartsWith ("dotnet:", StringComparison.Ordinal)) {
						var parts = objId.Split (new char [] { ':' });
						if (parts.Length < 3)
							return true;
						switch (parts[1]) {
						case "scope": {
							await GetScopeProperties (id, int.Parse (parts[2]), token);
							break;
							}
						case "object": {
							await GetDetails (id, MonoCommands.GetObjectProperties (int.Parse (parts[2])), token);
							break;
							}
						case "array": {
							await GetDetails (id, MonoCommands.GetArrayValues (int.Parse (parts [2])), token);
							break;
							}
						}
						return true;
					}
					break;
				}
			}

			return false;
		}

		//static int frame_id=0;
		async Task<bool> OnBreakpointHit (SessionId sessionId, JObject args, CancellationToken token)
		{
			//FIXME we should send release objects every now and then? Or intercept those we inject and deal in the runtime
			var res = await SendMonoCommand (sessionId, MonoCommands.GetCallStack(), token);
			var orig_callframes = args? ["callFrames"]?.Values<JObject> ();
			var context = GetContext (sessionId);

			if (res.IsErr) {
				//Give up and send the original call stack
				return false;
			}

			//step one, figure out where did we hit
			var res_value = res.Value? ["result"]? ["value"];
			if (res_value == null || res_value is JValue) {
				//Give up and send the original call stack
				return false;
			}

			Log ("verbose", $"call stack (err is {res.Error} value is:\n{res.Value}");
			var bp_id = res_value? ["breakpoint_id"]?.Value<int> ();
			Log ("verbose", $"We just hit bp {bp_id}");
			if (!bp_id.HasValue) {
				//Give up and send the original call stack
				return false;
			}

			var bp = context.BreakpointRequests.Values.SelectMany (v => v.Locations).FirstOrDefault (b => b.RemoteId == bp_id.Value);

			var callFrames = new List<object> ();
			foreach (var frame in orig_callframes) {
				var function_name = frame ["functionName"]?.Value<string> ();
				var url = frame ["url"]?.Value<string> ();
				if ("mono_wasm_fire_bp" == function_name || "_mono_wasm_fire_bp" == function_name) {
					var frames = new List<Frame> ();
					int frame_id = 0;
					var the_mono_frames = res.Value? ["result"]? ["value"]? ["frames"]?.Values<JObject> ();

					foreach (var mono_frame in the_mono_frames) {
						var il_pos = mono_frame ["il_pos"].Value<int> ();
						var method_token = mono_frame ["method_token"].Value<int> ();
						var assembly_name = mono_frame ["assembly_name"].Value<string> ();

						var store = await LoadStore (sessionId, token);
						var asm = store.GetAssemblyByName (assembly_name);
						if (asm == null) {
							Log ("info",$"Unable to find assembly: {assembly_name}");
							continue;
						}

						var method = asm.GetMethodByToken (method_token);

						if (method == null) {
							Log ("info", $"Unable to find il offset: {il_pos} in method token: {method_token} assembly name: {assembly_name}");
							continue;
						}

						var location = method?.GetLocationByIl (il_pos);

						// When hitting a breakpoint on the "IncrementCount" method in the standard
						// Blazor project template, one of the stack frames is inside mscorlib.dll
						// and we get location==null for it. It will trigger a NullReferenceException
						// if we don't skip over that stack frame.
						if (location == null) {
							continue;
						}

						Log ("info", $"frame il offset: {il_pos} method token: {method_token} assembly name: {assembly_name}");
						Log ("info", $"\tmethod {method.Name} location: {location}");
						frames.Add (new Frame (method, location, frame_id));

						callFrames.Add (new {
							functionName = method.Name,
							callFrameId = $"dotnet:scope:{frame_id}",
							functionLocation = method.StartLocation.AsLocation (),

							location = location.AsLocation (),

							url = store.ToUrl (location),

							scopeChain = new [] {
								new {
									type = "local",
									@object = new {
										@type = "object",
										className = "Object",
										description = "Object",
										objectId = $"dotnet:scope:{frame_id}",
									},
									name = method.Name,
									startLocation = method.StartLocation.AsLocation (),
									endLocation = method.EndLocation.AsLocation (),
								}}
						});

						++frame_id;
						context.CallStack = frames;

					}
				} else if (!(function_name.StartsWith ("wasm-function", StringComparison.Ordinal)
					|| url.StartsWith ("wasm://wasm/", StringComparison.Ordinal))) {
					callFrames.Add (frame);
				}
			}

			var bp_list = new string [bp == null ? 0 : 1];
			if (bp != null)
				bp_list [0] = bp.StackId;

			var o = JObject.FromObject (new {
				callFrames,
				reason = "other", //other means breakpoint
				hitBreakpoints = bp_list,
			});

			SendEvent (sessionId, "Debugger.paused", o, token);
			return true;
		}

		async Task OnDefaultContext (SessionId sessionId, ExecutionContext context, CancellationToken token)
		{
			Log ("verbose", "Default context created, clearing state and sending events");
			if (UpdateContext (sessionId, context, out var previousContext)) {
				foreach (var kvp in previousContext.BreakpointRequests) {
					context.BreakpointRequests[kvp.Key] = kvp.Value.Clone();
				}
			}

			if (await IsRuntimeAlreadyReadyAlready (sessionId, token))
				await RuntimeReady (sessionId, token);
		}

		async Task OnResume (MessageId msd_id, CancellationToken token)
		{
			//discard managed frames
			GetContext (msd_id).CallStack = null;
			await Task.CompletedTask;
		}

		async Task<bool> Step (MessageId msg_id, StepKind kind, CancellationToken token)
		{
			var context = GetContext (msg_id);
			if (context.CallStack == null)
				return false;

			if (context.CallStack.Count <= 1 && kind == StepKind.Out)
				return false;

			var res = await SendMonoCommand (msg_id, MonoCommands.StartSingleStepping (kind), token);

			var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

			if (ret_code.HasValue && ret_code.Value == 0) {
				context.CallStack = null;
				await SendCommand (msg_id, "Debugger.stepOut", new JObject (), token);
				return false;
			} 

			SendResponse (msg_id, Result.Ok (new JObject ()), token);

			context.CallStack = null;

			await SendCommand (msg_id, "Debugger.resume", new JObject (), token);
			return true;
		}

		static string FormatFieldName (string name)
		{
			if (name.Contains("k__BackingField", StringComparison.Ordinal)) {
				return name.Replace("k__BackingField", "", StringComparison.Ordinal)
					.Replace("<", "", StringComparison.Ordinal)
					.Replace(">", "", StringComparison.Ordinal);
			}
			return name;
		}

		async Task GetDetails(MessageId msg_id, MonoCommands cmd, CancellationToken token)
		{
			var res = await SendMonoCommand(msg_id, cmd, token);

			//if we fail we just buble that to the IDE (and let it panic over it)
			if (res.IsErr) {
				SendResponse(msg_id, res, token);
				return;
			}

			try {
				var values = res.Value?["result"]?["value"]?.Values<JObject>().ToArray() ?? Array.Empty<JObject>();
				var var_list = new List<JObject>();

				// Trying to inspect the stack frame for DotNetDispatcher::InvokeSynchronously
				// results in a "Memory access out of bounds", causing 'values' to be null,
				// so skip returning variable values in that case.
				for (int i = 0; i < values.Length; i+=2)
				{
					string fieldName = FormatFieldName ((string)values[i]["name"]);
					var value = values [i + 1]? ["value"];
					if (((string)value ["description"]) == null)
						value ["description"] = value ["value"]?.ToString ();

					var_list.Add(JObject.FromObject(new {
						name = fieldName,
						value
					}));

				}
				var response = JObject.FromObject(new
				{
					result = var_list
				});

				SendResponse(msg_id, Result.Ok(response), token);
			} catch (Exception e) {
				Log ("verbose", $"failed to parse {res.Value} - {e.Message}");
				SendResponse(msg_id, Result.Exception(e), token);
			}

		}

		async Task GetScopeProperties (MessageId msg_id, int scope_id, CancellationToken token)
		{

			try {
				var scope = GetContext (msg_id).CallStack.FirstOrDefault (s => s.Id == scope_id);
				var vars = scope.Method.GetLiveVarsAt (scope.Location.CliLocation.Offset);

				var var_ids = vars.Select (v => v.Index).ToArray ();
				var res = await SendMonoCommand (msg_id, MonoCommands.GetScopeVariables (scope.Id, var_ids), token);

				//if we fail we just buble that to the IDE (and let it panic over it)
				if (res.IsErr) {
					SendResponse (msg_id, res, token);
					return;
				}

				var values = res.Value? ["result"]? ["value"]?.Values<JObject> ().ToArray ();

				if(values == null) {
					SendResponse (msg_id, Result.OkFromObject (new {result = Array.Empty<object> ()}), token);
					return;
				}

				var var_list = new List<object> ();
				int i = 0;
				// Trying to inspect the stack frame for DotNetDispatcher::InvokeSynchronously
				// results in a "Memory access out of bounds", causing 'values' to be null,
				// so skip returning variable values in that case.
				while (i < vars.Length && i < values.Length) {
					var value = values [i] ["value"];
					if (((string)value ["description"]) == null)
						value ["description"] = value ["value"]?.ToString ();

					var_list.Add (new {
						name = vars [i].Name,
						value
					});
					i++;
				}
				//Async methods are special in the way that local variables can be lifted to generated class fields
				//value of "this" comes here either
				while (i < values.Length) {
					String name = values [i] ["name"].ToString ();

					if (name.IndexOf (">", StringComparison.Ordinal) > 0)
						name = name.Substring (1, name.IndexOf (">", StringComparison.Ordinal) - 1);

					var value = values [i + 1] ["value"];
					if (((string)value ["description"]) == null)
						value ["description"] = value ["value"]?.ToString ();

					var_list.Add (new {
						name,
						value
					});
					i = i + 2;
				}

				SendResponse (msg_id, Result.OkFromObject (new { result = var_list }), token);
			} catch (Exception exception) {
				Log ("verbose", $"Error resolving scope properties {exception.Message}");
				SendResponse (msg_id, Result.Exception (exception), token);
			}
		}

		async Task<Breakpoint> SetMonoBreakpoint (SessionId sessionId, BreakpointRequest req, SourceLocation location, CancellationToken token)
		{
			var bp = new Breakpoint (req.Id, location, BreakpointState.Pending);
			var asm_name = bp.Location.CliLocation.Method.Assembly.Name;
			var method_token = bp.Location.CliLocation.Method.Token;
			var il_offset = bp.Location.CliLocation.Offset;

			var res = await SendMonoCommand (sessionId, MonoCommands.SetBreakpoint (asm_name, method_token, il_offset), token);
			var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

			if (ret_code.HasValue) {
				bp.RemoteId = ret_code.Value;
				bp.State = BreakpointState.Active;
				//Log ("verbose", $"BP local id {bp.LocalId} enabled with remote id {bp.RemoteId}");
			}

			return bp;
		}

		async Task<DebugStore> LoadStore (SessionId sessionId, CancellationToken token)
		{
			var context = GetContext (sessionId);

			if (Interlocked.CompareExchange (ref context.store, new DebugStore (logger), null) != null)
				return await context.Source.Task;

			try {
				var loaded_pdbs = await SendMonoCommand (sessionId, MonoCommands.GetLoadedFiles(), token);
				var the_value = loaded_pdbs.Value? ["result"]? ["value"];
				var the_pdbs = the_value?.ToObject<string[]> ();

				await foreach (var source in context.store.Load(sessionId, the_pdbs, token).WithCancellation (token)) {
					var scriptSource = JObject.FromObject (source.ToScriptSource (context.Id, context.AuxData));
					Log ("verbose", $"\tsending {source.Url} {context.Id} {sessionId.sessionId}");

					SendEvent (sessionId, "Debugger.scriptParsed", scriptSource, token);

					foreach (var req in context.BreakpointRequests.Values) {
						if (req.TryResolve (source)) {
							await SetBreakpoint (sessionId, context.store, req, token);
						}
					}
				}
			} catch (Exception e) {
				context.Source.SetException (e);
			}

			if (!context.Source.Task.IsCompleted)
				context.Source.SetResult (context.store);
			return context.store;
		}

		async Task<DebugStore> RuntimeReady (SessionId sessionId, CancellationToken token)
		{
			var context = GetContext (sessionId);
			if (Interlocked.CompareExchange (ref context.ready, new TaskCompletionSource<DebugStore> (), null) != null)
				return await context.ready.Task;

			var clear_result = await SendMonoCommand (sessionId, MonoCommands.ClearAllBreakpoints (), token);
			if (clear_result.IsErr) {
				Log ("verbose", $"Failed to clear breakpoints due to {clear_result}");
			}

			var store = await LoadStore (sessionId, token);

			context.ready.SetResult (store);
			SendEvent (sessionId, "Mono.runtimeReady", new JObject (), token);
			return store;
		}

		async Task<bool> RemoveBreakpoint(MessageId msg_id, JObject args, CancellationToken token) {
			var bpid = args? ["breakpointId"]?.Value<string> ();

			var context = GetContext (msg_id);
			if (!context.BreakpointRequests.TryGetValue (bpid, out var breakpointRequest))
				return false;

			foreach (var bp in breakpointRequest.Locations) {
				var res = await SendMonoCommand (msg_id, MonoCommands.RemoveBreakpoint (bp.RemoteId), token);
				var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

				if (ret_code.HasValue) {
					bp.RemoteId = -1;
					bp.State = BreakpointState.Disabled;
				}
			}
			breakpointRequest.Locations.Clear ();
			return false;
		}

		async Task SetBreakpoint (SessionId sessionId, DebugStore store, BreakpointRequest req, CancellationToken token)
		{
			var context = GetContext (sessionId);
			if (req.Locations.Any ()) {
				Log ("debug", $"locations already loaded for {req.Id}");
				return;
			}

			var locations = store.FindBreakpointLocations (req).ToList ();
			logger.LogDebug ("BP request for '{req}' runtime ready {context.RuntimeReady}", req, GetContext (sessionId).IsRuntimeReady);

			var breakpoints = new List<Breakpoint> ();
			foreach (var loc in locations) {
				var bp = await SetMonoBreakpoint (sessionId, req, loc, token);

				// If we didn't successfully enable the breakpoint
				// don't add it to the list of locations for this id
				if (bp.State != BreakpointState.Active)
					continue;

				breakpoints.Add (bp);

				var resolvedLocation = new {
					breakpointId = req.Id,
					location = loc.AsLocation ()
				};

				SendEvent (sessionId, "Debugger.breakpointResolved", JObject.FromObject (resolvedLocation), token);
			}

			req.Locations.AddRange (breakpoints);
			return;
		}

		async Task<bool> GetPossibleBreakpoints (MessageId msg, SourceLocation start, SourceLocation end, CancellationToken token)
		{
			var bps = (await RuntimeReady (msg, token)).FindPossibleBreakpoints (start, end);

			if (bps == null)
				return false;

			var response = new { locations = bps.Select (b => b.AsLocation ()) };

			SendResponse (msg, Result.OkFromObject (response), token);
			return true;
		}

		void OnCompileDotnetScript (MessageId msg_id, CancellationToken token)
		{
			SendResponse (msg_id, Result.OkFromObject (new { }), token);
		}

		async Task<bool> OnGetScriptSource (MessageId msg_id, string script_id, CancellationToken token)
		{
			if (!SourceId.TryParse (script_id, out var id))
				return false;

			var src_file = (await LoadStore (msg_id, token)).GetFileById (id);
			var res = new StringWriter ();

			try {
				var uri = new Uri (src_file.Url);
				string source = $"// Unable to find document {src_file.SourceUri}";

				if (uri.IsFile && File.Exists(uri.LocalPath)) {
					using (var f = new StreamReader (File.Open (uri.LocalPath, FileMode.Open))) {
						await res.WriteAsync (await f.ReadToEndAsync ());
					}

					source = res.ToString ();
				} else if (src_file.SourceUri.IsFile && File.Exists(src_file.SourceUri.LocalPath)) {
					using (var f = new StreamReader (File.Open (src_file.SourceUri.LocalPath, FileMode.Open))) {
						await res.WriteAsync (await f.ReadToEndAsync ());
					}

					source = res.ToString ();
				} else if(src_file.SourceLinkUri != null) {
					var doc = await new WebClient ().DownloadStringTaskAsync (src_file.SourceLinkUri);
					await res.WriteAsync (doc);

					source = res.ToString ();
				}

				SendResponse (msg_id, Result.OkFromObject (new { scriptSource = source }), token);
			} catch (Exception e) {
				var o = new {
					scriptSource = $"// Unable to read document ({e.Message})\n" +
								$"Local path: {src_file?.SourceUri}\n" +
								$"SourceLink path: {src_file?.SourceLinkUri}\n"
				};

				SendResponse (msg_id, Result.OkFromObject (o), token);
			}
			return true;
		}
	}
}
