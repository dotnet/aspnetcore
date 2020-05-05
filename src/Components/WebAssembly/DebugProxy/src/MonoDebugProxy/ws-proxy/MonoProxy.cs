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
		HashSet<SessionId> sessions = new HashSet<SessionId> ();
		Dictionary<SessionId, ExecutionContext> contexts = new Dictionary<SessionId, ExecutionContext> ();

		public MonoProxy (ILoggerFactory loggerFactory, bool hideWebDriver = true) : base(loggerFactory) { this.hideWebDriver = hideWebDriver; }

		readonly bool hideWebDriver;

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

			case "Debugger.scriptParsed": {
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

			case "Target.attachedToTarget": {
					if (args["targetInfo"]["type"]?.ToString() == "page")
						await DeleteWebDriver (new SessionId (args["sessionId"]?.ToString ()), token);
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

		static int bpIdGenerator;

		protected override async Task<bool> AcceptCommand (MessageId id, string method, JObject args, CancellationToken token)
		{
			// Inspector doesn't use the Target domain or sessions
			// so we try to init immediately
			if (hideWebDriver && id == SessionId.Null)
				await DeleteWebDriver (id, token);

			if (!contexts.TryGetValue (id, out var context))
				return false;

			switch (method) {
			case "Target.attachToTarget": {
					var resp = await SendCommand (id, method, args, token);
					await DeleteWebDriver (new SessionId (resp.Value ["sessionId"]?.ToString ()), token);
					break;
				}

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
					var locations = resp.Value["locations"]?.Values<object>();
					var request = BreakpointRequest.Parse (bpid, args);
					context.BreakpointRequests[bpid] = request;
					if (await IsRuntimeAlreadyReadyAlready (id, token)) {
						var store = await RuntimeReady (id, token);

						Log ("verbose", $"BP req {args}");
						await SetBreakpoint (id, store, request, token);
					}

					var result = Result.OkFromObject (request.AsSetBreakpointByUrlResponse (locations));
					SendResponse (id, result, token);
					return true;
				}

			case "Debugger.removeBreakpoint": {
					await RemoveBreakpoint (id, args, token);
					break;
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
					if (!DotnetObjectId.TryParse (args? ["callFrameId"], out var objectId))
						return false;

					switch (objectId.Scheme) {
					case "scope":
						return await OnEvaluateOnCallFrame (id,
								int.Parse (objectId.Value),
								args? ["expression"]?.Value<string> (), token);
					default:
						return false;
					}
				}

			case "Runtime.getProperties": {
					if (!DotnetObjectId.TryParse (args? ["objectId"], out var objectId))
						break;

					var result = await RuntimeGetProperties (id, objectId, args, token);
					SendResponse (id, result, token);
					return true;
				}

			case "Runtime.releaseObject": {
					if (!(DotnetObjectId.TryParse (args ["objectId"], out var objectId) && objectId.Scheme == "cfo_res"))
						break;

					await SendMonoCommand (id, MonoCommands.ReleaseObject (objectId), token);
					SendResponse (id, Result.OkFromObject (new{}), token);
					return true;
				}

				// Protocol extensions
			case "Dotnet-test.setBreakpointByMethod": {
				Console.WriteLine ("set-breakpoint-by-method: " + id + " " + args);

				var store = await RuntimeReady (id, token);
				string aname = args ["assemblyName"]?.Value<string> ();
				string typeName = args ["typeName"]?.Value<string> ();
				string methodName = args ["methodName"]?.Value<string> ();
				if (aname == null || typeName == null || methodName == null) {
					SendResponse (id, Result.Err ("Invalid protocol message '" + args + "'."), token);
					return true;
				}

				// GetAssemblyByName seems to work on file names
				var assembly = store.GetAssemblyByName (aname);
				if (assembly == null)
					assembly = store.GetAssemblyByName (aname + ".exe");
				if (assembly == null)
					assembly = store.GetAssemblyByName (aname + ".dll");
				if (assembly == null) {
					SendResponse (id, Result.Err ("Assembly '" + aname + "' not found."), token);
					return true;
				}

				var type = assembly.GetTypeByName (typeName);
				if (type == null) {
					SendResponse (id, Result.Err ($"Type '{typeName}' not found."), token);
					return true;
				}

				var methodInfo = type.Methods.FirstOrDefault (m => m.Name == methodName);
				if (methodInfo == null) {
					SendResponse (id, Result.Err ($"Method '{typeName}:{methodName}' not found."), token);
					return true;
				}

				bpIdGenerator ++;
				string bpid = "by-method-" + bpIdGenerator.ToString ();
				var request = new BreakpointRequest (bpid, methodInfo);
				context.BreakpointRequests[bpid] = request;

				var loc = methodInfo.StartLocation;
				var bp = await SetMonoBreakpoint (id, bpid, loc, token);
				if (bp.State != BreakpointState.Active) {
					// FIXME:
					throw new NotImplementedException ();
				}

				var resolvedLocation = new {
					breakpointId = bpid,
					location = loc.AsLocation ()
				};

				SendEvent (id, "Debugger.breakpointResolved", JObject.FromObject (resolvedLocation), token);

				SendResponse (id, Result.OkFromObject (new {
						result = new { breakpointId = bpid, locations = new object [] { loc.AsLocation () }}
					}), token);

				return true;
			}
			case "Runtime.callFunctionOn": {
					if (!DotnetObjectId.TryParse (args ["objectId"], out var objectId))
						return false;

					var silent = args ["silent"]?.Value<bool> () ?? false;
					if (objectId.Scheme == "scope") {
						var fail = silent ? Result.OkFromObject (new { result = new { } }) : Result.Exception (new ArgumentException ($"Runtime.callFunctionOn not supported with scope ({objectId})."));

						SendResponse (id, fail, token);
						return true;
					}

					var returnByValue = args ["returnByValue"]?.Value<bool> () ?? false;
					var res = await SendMonoCommand (id, MonoCommands.CallFunctionOn (args), token);

					if (!returnByValue &&
						DotnetObjectId.TryParse (res.Value?["result"]?["value"]?["objectId"], out var resultObjectId) &&
						resultObjectId.Scheme == "cfo_res")
						res = Result.OkFromObject (new { result = res.Value ["result"]["value"] });

					if (res.IsErr && silent)
						res = Result.OkFromObject (new { result = new { } });

					SendResponse (id, res, token);
					return true;
				}
			}

			return false;
		}

		async Task<Result> RuntimeGetProperties (MessageId id, DotnetObjectId objectId, JToken args, CancellationToken token)
		{
			if (objectId.Scheme == "scope")
				return await GetScopeProperties (id, int.Parse (objectId.Value), token);

			var res = await SendMonoCommand (id, MonoCommands.GetDetails (objectId, args), token);
			if (res.IsErr)
				return res;

			if (objectId.Scheme == "cfo_res") {
				// Runtime.callFunctionOn result object
				var value_json_str = res.Value ["result"]?["value"]?["__value_as_json_string__"]?.Value<string> ();
				if (value_json_str != null) {
					res = Result.OkFromObject (new {
							result = JArray.Parse (value_json_str.Replace (@"\""", "\""))
					});
				} else {
					res = Result.OkFromObject (new { result = new {} });
				}
			} else {
				res = Result.Ok (JObject.FromObject (new { result = res.Value ["result"] ["value"] }));
			}

			return res;
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
						var method_token = mono_frame ["method_token"].Value<uint> ();
						var assembly_name = mono_frame ["assembly_name"].Value<string> ();

						// This can be different than `method.Name`, like in case of generic methods
						var method_name = mono_frame ["method_name"]?.Value<string> ();

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
						Log ("info", $"\tmethod {method_name} location: {location}");
						frames.Add (new Frame (method, location, frame_id-1));

						callFrames.Add (new {
							functionName = method_name,
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
									name = method_name,
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
			var live_vars = scope.Method.GetLiveVarsAt (scope.Location.CliLocation.Offset);
			//get_this
			var res = await SendMonoCommand (msg_id, MonoCommands.GetScopeVariables (scope.Id, live_vars.Select (lv => lv.Index).ToArray ()), token);

			var scope_values = res.Value? ["result"]? ["value"]?.Values<JObject> ()?.ToArray ();
			thisValue = scope_values?.FirstOrDefault (v => v ["name"]?.Value<string> () == "this");

			if (!only_search_on_this) {
				if (thisValue != null && expression == "this")
					return thisValue;

				var value = scope_values.SingleOrDefault (sv => sv ["name"]?.Value<string> () == expression);
				if (value != null)
					return value;
			}

			//search in scope
			if (thisValue != null) {
				if (!DotnetObjectId.TryParse (thisValue ["value"] ["objectId"], out var objectId))
					return null;

				res = await SendMonoCommand (msg_id, MonoCommands.GetDetails (objectId), token);
				scope_values = res.Value? ["result"]? ["value"]?.Values<JObject> ().ToArray ();
				var foundValue = scope_values.FirstOrDefault (v => v ["name"].Value<string> () == expression);
				if (foundValue != null) {
					foundValue["fromThis"] = true;
					context.LocalsCache[foundValue ["name"].Value<string> ()] = foundValue;
					return foundValue;
				}
			}
			return null;
		}

		async Task<bool> OnEvaluateOnCallFrame (MessageId msg_id, int scope_id, string expression, CancellationToken token)
		{
			try {
				var context = GetContext (msg_id);
				if (context.CallStack == null)
					return false;

				var varValue = await TryGetVariableValue (msg_id, scope_id, expression, false, token);

				if (varValue != null) {
					SendResponse (msg_id, Result.OkFromObject (new {
						result = varValue ["value"]
					}), token);
					return true;
				}

				string retValue = await EvaluateExpression.CompileAndRunTheExpression (this, msg_id, scope_id, expression, token);
				SendResponse (msg_id, Result.OkFromObject (new {
					result = new {
						value = retValue
					}
				}), token);
				return true;
			} catch (Exception e) {
				logger.LogDebug (e, $"Error in EvaluateOnCallFrame for expression '{expression}.");
			}
			return false;
		}

		async Task<Result> GetScopeProperties (MessageId msg_id, int scope_id, CancellationToken token)
		{
			try {
				var ctx = GetContext (msg_id);
				var scope = ctx.CallStack.FirstOrDefault (s => s.Id == scope_id);
				if (scope == null)
					return Result.Err (JObject.FromObject (new { message = $"Could not find scope with id #{scope_id}" }));

				var vars = scope.Method.GetLiveVarsAt (scope.Location.CliLocation.Offset);

				var var_ids = vars.Select (v => v.Index).ToArray ();
				var res = await SendMonoCommand (msg_id, MonoCommands.GetScopeVariables (scope.Id, var_ids), token);

				//if we fail we just buble that to the IDE (and let it panic over it)
				if (res.IsErr)
					return res;

				var values = res.Value? ["result"]? ["value"]?.Values<JObject> ().ToArray ();

				if(values == null)
					return Result.OkFromObject (new { result = Array.Empty<object> () });

				var var_list = new List<object> ();
				int i = 0;
				for (; i < vars.Length && i < values.Length; i ++) {
					// For async methods, we get locals with names, unlike non-async methods
					// and the order may not match the var_ids, so, use the names that they
					// come with
					if (values [i]["name"] != null)
						continue;

					ctx.LocalsCache[vars [i].Name] = values [i];
					var_list.Add (new { name = vars [i].Name, value = values [i]["value"] });
				}
				for (; i < values.Length; i ++) {
					ctx.LocalsCache[values [i]["name"].ToString()] = values [i];
					var_list.Add (values [i]);
				}

				return Result.OkFromObject (new { result = var_list });
			} catch (Exception exception) {
				Log ("verbose", $"Error resolving scope properties {exception.Message}");
				return Result.Exception (exception);
			}
		}

		async Task<Breakpoint> SetMonoBreakpoint (SessionId sessionId, string reqId, SourceLocation location, CancellationToken token)
		{
			var bp = new Breakpoint (reqId, location, BreakpointState.Pending);
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

		async Task RemoveBreakpoint(MessageId msg_id, JObject args, CancellationToken token) {
			var bpid = args? ["breakpointId"]?.Value<string> ();

			var context = GetContext (msg_id);
			if (!context.BreakpointRequests.TryGetValue (bpid, out var breakpointRequest))
				return;

			foreach (var bp in breakpointRequest.Locations) {
				var res = await SendMonoCommand (msg_id, MonoCommands.RemoveBreakpoint (bp.RemoteId), token);
				var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

				if (ret_code.HasValue) {
					bp.RemoteId = -1;
					bp.State = BreakpointState.Disabled;
				}
			}
			breakpointRequest.Locations.Clear ();
		}

		async Task SetBreakpoint (SessionId sessionId, DebugStore store, BreakpointRequest req, CancellationToken token)
		{
			var context = GetContext (sessionId);
			if (req.Locations.Any ()) {
				Log ("debug", $"locations already loaded for {req.Id}");
				return;
			}

			var comparer = new SourceLocation.LocationComparer ();
			// if column is specified the frontend wants the exact matches
			// and will clear the bp if it isn't close enoug
			var locations = store.FindBreakpointLocations (req)
				.Distinct (comparer)
				.Where (l => l.Line == req.Line && (req.Column == 0 || l.Column == req.Column))
				.OrderBy (l => l.Column)
				.GroupBy (l => l.Id);

			logger.LogDebug ("BP request for '{req}' runtime ready {context.RuntimeReady}", req, GetContext (sessionId).IsRuntimeReady);

			var breakpoints = new List<Breakpoint> ();

			foreach (var sourceId in locations) {
				var loc = sourceId.First ();
				var bp = await SetMonoBreakpoint (sessionId, req.Id, loc, token);

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

			try {
				var uri = new Uri (src_file.Url);
				string source = $"// Unable to find document {src_file.SourceUri}";

				using (var data = await src_file.GetSourceAsync (checkHash: false, token: token)) {
						if (data.Length == 0)
							return false;

						using (var reader = new StreamReader (data))
							source = await reader.ReadToEndAsync ();
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

		async Task DeleteWebDriver (SessionId sessionId, CancellationToken token)
		{
			// see https://github.com/mono/mono/issues/19549 for background
			if (hideWebDriver && sessions.Add (sessionId)) {
				var res = await SendCommand (sessionId,
					"Page.addScriptToEvaluateOnNewDocument",
					JObject.FromObject (new { source = "delete navigator.constructor.prototype.webdriver"}),
					token);

				if (sessionId != SessionId.Null && !res.IsOk)
					sessions.Remove (sessionId);
			}
		}
	}
}
