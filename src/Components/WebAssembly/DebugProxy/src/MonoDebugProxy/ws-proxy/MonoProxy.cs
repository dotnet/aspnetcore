using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis;


namespace WebAssembly.Net.Debugging {

	internal class MonoProxy : DevToolsProxy {
		Dictionary<SessionId, ExecutionContext> contexts = new Dictionary<SessionId, ExecutionContext> ();

		public MonoProxy (ILoggerFactory loggerFactory) : base(loggerFactory) { }

		internal ExecutionContext GetContext (SessionId sessionId)
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
			if (!contexts.TryGetValue (id, out var context))
				return false;

			switch (method) {
			case "Debugger.enable": {
					var resp = await SendCommand (id, method, args, token);

					context.DebuggerId = resp.Value ["debuggerId"]?.ToString ();

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

			case "Debugger.evaluateOnCallFrame": {
					var objId = args? ["callFrameId"]?.Value<string> ();
					if (objId.StartsWith ("dotnet:", StringComparison.Ordinal)) {
						var parts = objId.Split (new char [] { ':' });
						if (parts.Length < 3)
							return true;
						switch (parts [1]) {
						case "scope": {
								await GetEvaluateOnCallFrame (id, int.Parse (parts [2]), args? ["expression"]?.Value<string> (), token);
								break;
							}
						}
						return true;
					}
					return false;
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
							await GetDetails (id, MonoCommands.GetObjectProperties (int.Parse (parts[2]), expandValueTypes: false), token);
							break;
							}
						case "array": {
							await GetArrayDetails (id, objId, parts, token);
							break;
							}
						case "valuetype": {
							await GetDetailsForValueType (id, objId,
									get_props_cmd_fn: () => {
										if (parts.Length < 4)
											return null;

										var containerObjId = int.Parse (parts[2]);
										return MonoCommands.GetObjectProperties (containerObjId, expandValueTypes: true);
									}, token);
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

		async Task GetArrayDetails (MessageId id, string objId, string[] objIdParts, CancellationToken token)
		{
			switch (objIdParts.Length)
			{
				case 3: {
					await GetDetails (id, MonoCommands.GetArrayValues (int.Parse (objIdParts [2])), token);
					break;
					}
				case 4: {
					// This form of the id is being used only for valuetypes right now
					await GetDetailsForValueType(id, objId,
							get_props_cmd_fn: () => {
								var arrayObjectId = int.Parse (objIdParts [2]);
								var idx = int.Parse (objIdParts [3]);
								return MonoCommands.GetArrayValueExpanded (arrayObjectId, idx);
							}, token);
					break;
					}
				default:
					SendResponse (id, Result.Exception (new ArgumentException ($"Unknown objectId format for array: {objId}")), token);
					break;
			}
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
						++frame_id;
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
						frames.Add (new Frame (method, location, frame_id-1));

						callFrames.Add (new {
							functionName = method.Name,
							callFrameId = $"dotnet:scope:{frame_id-1}",
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
										objectId = $"dotnet:scope:{frame_id-1}",
									},
									name = method.Name,
									startLocation = method.StartLocation.AsLocation (),
									endLocation = method.EndLocation.AsLocation (),
								}}
						});

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
			GetContext (msd_id).ClearState ();
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
				context.ClearState ();
				await SendCommand (msg_id, "Debugger.stepOut", new JObject (), token);
				return false;
			}

			SendResponse (msg_id, Result.Ok (new JObject ()), token);

			context.ClearState ();

			await SendCommand (msg_id, "Debugger.resume", new JObject (), token);
			return true;
		}

		async Task GetDetails(MessageId msg_id, MonoCommands cmd, CancellationToken token, bool send_response = true)
		{
			var res = await SendMonoCommand(msg_id, cmd, token);

			//if we fail we just buble that to the IDE (and let it panic over it)
			if (res.IsErr) {
				SendResponse(msg_id, res, token);
				return;
			}

			try {
				var var_list = res.Value?["result"]?["value"]?.Values<JObject>().ToArray() ?? Array.Empty<JObject>();
				if (var_list.Length > 0)
					ExtractAndCacheValueTypes (GetContext (msg_id), var_list);

				if (!send_response)
					return;

				var response = JObject.FromObject(new
				{
					result = var_list
				});

				SendResponse(msg_id, Result.Ok(response), token);
			} catch (Exception e) when (send_response) {
				Log ("verbose", $"failed to parse {res.Value} - {e.Message}");
				SendResponse(msg_id, Result.Exception(e), token);
			}

		}

		internal bool TryFindVariableValueInCache(ExecutionContext ctx, string expression, bool only_search_on_this, out JToken obj)
		{
			if (ctx.LocalsCache.TryGetValue (expression, out obj)) {
				if (only_search_on_this && obj["fromThis"] == null)
					return false;
				return true;
			}
			return false;
		}

		internal async Task<JToken> TryGetVariableValue (MessageId msg_id, int scope_id, string expression, bool only_search_on_this, CancellationToken token)
		{
			JToken thisValue = null;
			var context = GetContext (msg_id);
			if (context.CallStack == null)
				return null;
			
			if (TryFindVariableValueInCache(context, expression, only_search_on_this, out JToken obj))
				return obj;				

			var scope = context.CallStack.FirstOrDefault (s => s.Id == scope_id);
			var vars = scope.Method.GetLiveVarsAt (scope.Location.CliLocation.Offset);
			//get_this
			int [] var_ids = { };
			var res = await SendMonoCommand (msg_id, MonoCommands.GetScopeVariables (scope.Id, var_ids), token);
			var values = res.Value? ["result"]? ["value"]?.Values<JObject> ().ToArray ();
			thisValue = values.FirstOrDefault (v => v ["name"].Value<string> () == "this");
			
			if (!only_search_on_this) {
				if (thisValue != null && expression == "this") {
					return thisValue;
				}
				//search in locals
				var var_id = vars.SingleOrDefault (v => v.Name == expression);
				if (var_id != null) {
					res = await SendMonoCommand (msg_id, MonoCommands.GetScopeVariables (scope.Id, new int [] { var_id.Index }), token);
					values = res.Value? ["result"]? ["value"]?.Values<JObject> ().ToArray ();
					return values [0];
				}
			}

			//search in scope
			if (thisValue != null) {
				var objectId = thisValue ["value"] ["objectId"].Value<string> ();
				var parts = objectId.Split (new char [] { ':' });
				res = await SendMonoCommand (msg_id, MonoCommands.GetObjectProperties (int.Parse (parts [2]), expandValueTypes: false), token);
				values = res.Value? ["result"]? ["value"]?.Values<JObject> ().ToArray ();
				var foundValue = values.FirstOrDefault (v => v ["name"].Value<string> () == expression);
				if (foundValue != null) {
					foundValue["fromThis"] = true;
					context.LocalsCache[foundValue ["name"].Value<string> ()] = foundValue;
					return foundValue;
				}
			}
			return null;
		}

		async Task GetEvaluateOnCallFrame (MessageId msg_id, int scope_id, string expression, CancellationToken token)
		{
			try {
				var context = GetContext (msg_id);
				if (context.CallStack == null)
					return;

				var varValue = await TryGetVariableValue (msg_id, scope_id, expression, false, token);

				if (varValue != null) {
					varValue ["value"] ["description"] = varValue ["value"] ["className"];
					SendResponse (msg_id, Result.OkFromObject (new {
						result = varValue ["value"]
					}), token);
					return;
				}

				string retValue = await EvaluateExpression.CompileAndRunTheExpression (this, msg_id, scope_id, expression, token);
				SendResponse (msg_id, Result.OkFromObject (new {
					result = new {
						value = retValue
					}
				}), token);
			} catch (Exception e) {
				logger.LogTrace (e.Message, expression);
				SendResponse (msg_id, Result.OkFromObject (new {}), token);
			}
		}

		async Task GetScopeProperties (MessageId msg_id, int scope_id, CancellationToken token)
		{

			try {
				var ctx = GetContext (msg_id);
				var scope = ctx.CallStack.FirstOrDefault (s => s.Id == scope_id);
				if (scope == null) {
					SendResponse (msg_id,
							Result.Err (JObject.FromObject (new { message = $"Could not find scope with id #{scope_id}" })),
							token);
					return;
				}
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

				ExtractAndCacheValueTypes (ctx, values);

				var var_list = new List<object> ();
				int i = 0;
				for (; i < vars.Length && i < values.Length; i ++) {
					ctx.LocalsCache[vars [i].Name] = values [i];
					var_list.Add (new { name = vars [i].Name, value = values [i]["value"] });
				}
				for (; i < values.Length; i ++) {
					ctx.LocalsCache[values [i]["name"].ToString()] = values [i]["value"];
					var_list.Add (values [i]);
				}

				SendResponse (msg_id, Result.OkFromObject (new { result = var_list }), token);
			} catch (Exception exception) {
				Log ("verbose", $"Error resolving scope properties {exception.Message}");
				SendResponse (msg_id, Result.Exception (exception), token);
			}
		}

		IEnumerable<JObject> ExtractAndCacheValueTypes (ExecutionContext ctx, IEnumerable<JObject> var_list)
		{
			foreach (var jo in var_list) {
				var value = jo["value"]?.Value<JObject> ();
				if (value ["type"]?.Value<string> () != "object")
					continue;

				if (!(value ["isValueType"]?.Value<bool> () ?? false) || // not a valuetype
					!(value ["expanded"]?.Value<bool> () ?? false))  // not expanded
					continue;

				// Expanded ValueType
				var members = value ["members"]?.Values<JObject>().ToArray() ?? Array.Empty<JObject>();
				var objectId = value ["objectId"]?.Value<string> () ?? $"dotnet:valuetype:{ctx.NextValueTypeId ()}";

				value ["objectId"] = objectId;

				ExtractAndCacheValueTypes (ctx, members);

				ctx.ValueTypesCache [objectId] = JArray.FromObject (members);
				value.Remove ("members");
			}

			return var_list;
		}

		async Task<bool> GetDetailsForValueType (MessageId msg_id, string object_id, Func<MonoCommands> get_props_cmd_fn, CancellationToken token)
		{
			var ctx = GetContext (msg_id);

			if (!ctx.ValueTypesCache.ContainsKey (object_id)) {
				var cmd = get_props_cmd_fn ();
				if (cmd == null) {
					SendResponse (msg_id, Result.Exception (new ArgumentException (
									"Could not find a cached value for {object_id}, and cant' expand it.")),
									token);

					return false;
				} else {
					await GetDetails (msg_id, cmd, token, send_response: false);
				}
			}

			if (ctx.ValueTypesCache.TryGetValue (object_id, out var var_list)) {
				var response = JObject.FromObject(new
				{
					result = var_list
				});

				SendResponse(msg_id, Result.Ok(response), token);
				return true;
			} else {
				var response = JObject.FromObject(new
				{
					result = $"Unable to get details for {object_id}"
				});

				SendResponse(msg_id, Result.Err(response), token);
				return false;
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
