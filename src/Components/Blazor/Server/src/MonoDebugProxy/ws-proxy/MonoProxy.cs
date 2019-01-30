using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using System.Net.WebSockets;
using System.Threading;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace WsProxy {

	internal class MonoCommands {
		public const string GET_CALL_STACK = "MONO.mono_wasm_get_call_stack()";
		public const string IS_RUNTIME_READY_VAR = "MONO.mono_wasm_runtime_is_ready";
		public const string START_SINGLE_STEPPING = "MONO.mono_wasm_start_single_stepping({0})";
		public const string GET_SCOPE_VARIABLES = "MONO.mono_wasm_get_variables({0}, [ {1} ])";
		public const string SET_BREAK_POINT = "MONO.mono_wasm_set_breakpoint(\"{0}\", {1}, {2})";
		public const string REMOVE_BREAK_POINT = "MONO.mono_wasm_remove_breakpoint({0})";
		public const string GET_LOADED_FILES = "MONO.mono_wasm_get_loaded_files()";
		public const string CLEAR_ALL_BREAKPOINTS = "MONO.mono_wasm_clear_all_breakpoints()";
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
		public int LocalId { get; private set; }
		public int RemoteId { get; set; }
		public BreakPointState State { get; set; }

		public Breakpoint (SourceLocation loc, int localId, BreakPointState state)
		{
			this.Location = loc;
			this.LocalId = localId;
			this.State = state;
		}
	}

	enum BreakPointState {
		Active,
		Disabled,
		Pending
	}

	enum StepKind {
		Into,
		Out,
		Over
	}

	internal class MonoProxy : WsProxy {
		DebugStore store;
		List<Breakpoint> breakpoints = new List<Breakpoint> ();
		List<Frame> current_callstack;
		bool runtime_ready;
		int local_breakpoint_id;
		int ctx_id;
		JObject aux_ctx_data;

		public MonoProxy () { }

		protected override async Task<bool> AcceptEvent (string method, JObject args, CancellationToken token)
		{
			switch (method) {
			case "Runtime.executionContextCreated": {
					var ctx = args? ["context"];
					var aux_data = ctx? ["auxData"] as JObject;
					if (aux_data != null) {
						var is_default = aux_data ["isDefault"]?.Value<bool> ();
						if (is_default == true) {
							var ctx_id = ctx ["id"].Value<int> ();
							await OnDefaultContext (ctx_id, aux_data, token);
						}
					}
					break;
				}
			case "Debugger.paused": {
					//TODO figure out how to stich out more frames and, in particular what happens when real wasm is on the stack
					var top_func = args? ["callFrames"]? [0]? ["functionName"]?.Value<string> ();
					if (top_func == "mono_wasm_fire_bp" || top_func == "_mono_wasm_fire_bp") {
						await OnBreakPointHit (args, token);
						return true;
					}
					if (top_func == MonoConstants.RUNTIME_IS_READY) {
						await OnRuntimeReady (token);
						return true;
					}
					break;
				}
			case "Debugger.scriptParsed":{
					if (args?["url"]?.Value<string> ()?.StartsWith ("wasm://") == true) {
						// Console.WriteLine ("ignoring wasm event");
						return true;
					}
					break;
				}
			}

			return false;
		}


		protected override async Task<bool> AcceptCommand (int id, string method, JObject args, CancellationToken token)
		{
			switch (method) {
			case "Debugger.getScriptSource": {
					var script_id = args? ["scriptId"]?.Value<string> ();
					if (script_id.StartsWith ("dotnet://", StringComparison.InvariantCultureIgnoreCase)) {
						OnGetScriptSource (id, script_id, token);
						return true;
					}

					break;
				}
			case "Runtime.compileScript": {
					var exp = args? ["expression"]?.Value<string> ();
					if (exp.StartsWith ("//dotnet:", StringComparison.InvariantCultureIgnoreCase)) {
						OnCompileDotnetScript (id, token);
						return true;
					}
					break;
				}

			case "Debugger.getPossibleBreakpoints": {
					var start = SourceLocation.Parse (args? ["start"] as JObject);
					//FIXME support variant where restrictToFunction=true and end is omitted
					var end = SourceLocation.Parse (args? ["end"] as JObject);
					if (start != null && end != null)
						return GetPossibleBreakpoints (id, start, end, token);
					break;
				}

			case "Debugger.setBreakpointByUrl": {
					Info ($"BP req {args}");
					var bp_req = BreakPointRequest.Parse (args);
					if (bp_req != null) {
						await SetBreakPoint (id, bp_req, token);
						return true;
					}
					break;
				}
			case "Debugger.removeBreakpoint": {
				return await RemoveBreakpoint (id, args, token);
			}

			case "Debugger.resume": {
					await OnResume (token);
					break;
				}

			case "Debugger.stepInto": {
					if (this.current_callstack != null) {
						await Step (id, StepKind.Into, token);
						return true;
					}
					break;
				}

			case "Debugger.stepOut": {
					if (this.current_callstack != null) {
						await Step (id, StepKind.Out, token);
						return true;
					}
					break;
				}

			case "Debugger.stepOver": {
					if (this.current_callstack != null) {
						await Step (id, StepKind.Over, token);
						return true;
					}
					break;
				}

			case "Runtime.getProperties": {
					var objId = args? ["objectId"]?.Value<string> ();
					if (objId.StartsWith ("dotnet:scope:", StringComparison.InvariantCulture)) {
						await GetScopeProperties (id, int.Parse (objId.Substring ("dotnet:scope:".Length)), token);
						return true;
					}

					break;
				}
			}

			return false;
		}

		async Task OnRuntimeReady (CancellationToken token)
		{
			Info ("RUNTIME READY, PARTY TIME");
			await RuntimeReady (token);
			await SendCommand ("Debugger.resume", new JObject (), token);
		}

		async Task OnBreakPointHit (JObject args, CancellationToken token)
		{
			//FIXME we should send release objects every now and then? Or intercept those we inject and deal in the runtime
			var o = JObject.FromObject (new {
				expression = MonoCommands.GET_CALL_STACK,
				objectGroup = "mono_debugger",
				includeCommandLineAPI = false,
				silent = false,
				returnByValue = true
			});

			var orig_callframes = args? ["callFrames"]?.Values<JObject> ();
			var res = await SendCommand ("Runtime.evaluate", o, token);

			if (res.IsErr) {
				//Give up and send the original call stack
				SendEvent ("Debugger.paused", args, token);
				return;
			}

			//step one, figure out where did we hit
			//lol no, fuck it, let's use fake data
			var res_value = res.Value? ["result"]? ["value"];
			if (res_value == null || res_value is JValue) {
				//Give up and send the original call stack
				SendEvent ("Debugger.paused", args, token);
				return;
			}

			Debug ($"call stack (err is {res.Error} value is:\n{res.Value}");
			var bp_id = res_value? ["breakpoint_id"]?.Value<int> ();
			Debug ($"We just hit bp {bp_id}");
			if (!bp_id.HasValue) {
				//Give up and send the original call stack
				SendEvent ("Debugger.paused", args, token);
				return;
			}
			var bp = this.breakpoints.FirstOrDefault (b => b.RemoteId == bp_id.Value);

			var src = bp == null ? null : store.GetFileById (bp.Location.Id);

			var callFrames = new List<JObject> ();
			foreach (var f in orig_callframes) {
				var function_name = f ["functionName"]?.Value<string> ();
				var url = f ["url"]?.Value<string> ();
				if ("mono_wasm_fire_bp" == function_name || "_mono_wasm_fire_bp" == function_name) {
					var frames = new List<Frame> ();
					int frame_id = 0;
					var the_mono_frames = res.Value? ["result"]? ["value"]? ["frames"]?.Values<JObject> ();
					foreach (var mono_frame in the_mono_frames) {
						var il_pos = mono_frame ["il_pos"].Value<int> ();
						var method_token = mono_frame ["method_token"].Value<int> ();
						var assembly_name = mono_frame ["assembly_name"].Value<string> ();

						var asm = store.GetAssemblyByName (assembly_name);
						var method = asm.GetMethodByToken (method_token);
						var location = method.GetLocationByIl (il_pos);

						// When hitting a breakpoint on the "IncrementCount" method in the standard
						// Blazor project template, one of the stack frames is inside mscorlib.dll
						// and we get location==null for it. It will trigger a NullReferenceException
						// if we don't skip over that stack frame.
						if (location == null)
						{
							continue;
						}

						Info ($"frame il offset: {il_pos} method token: {method_token} assembly name: {assembly_name}");
						Info ($"\tmethod {method.Name} location: {location}");
						frames.Add (new Frame (method, location, frame_id));

						callFrames.Add (JObject.FromObject (new {
							functionName = method.Name,

							functionLocation = method.StartLocation.ToJObject (),

							location = location.ToJObject (),

							url = store.ToUrl (location),

							scopeChain = new [] {
								new {
									type = "local",
									@object = new {
										@type = "object",
							 			className = "Object",
										description = "Object",
										objectId = $"dotnet:scope:{frame_id}"
									},
									name = method.Name,
									startLocation = method.StartLocation.ToJObject (),
									endLocation = method.EndLocation.ToJObject (),
								}
							},

							@this = new {
							}
						}));

						++frame_id;
						this.current_callstack = frames;
					}
				} else if (!url.StartsWith ("wasm://wasm/", StringComparison.InvariantCulture)) {
					callFrames.Add (f);
				}
			}

			var bp_list = new string [bp == null ? 0 : 1];
			if (bp != null)
				bp_list [0] = $"dotnet:{bp.LocalId}";

			o = JObject.FromObject (new {
				callFrames = callFrames,
				reason = "other", //other means breakpoint
				hitBreakpoints = bp_list,
			});

			SendEvent ("Debugger.paused", o, token);
		}

		async Task OnDefaultContext (int ctx_id, JObject aux_data, CancellationToken token)
		{
			Debug ("Default context created, clearing state and sending events");

			//reset all bps
			foreach (var b in this.breakpoints){
				b.State = BreakPointState.Pending;
			}
			this.runtime_ready = false;

			var o = JObject.FromObject (new {
				expression = MonoCommands.IS_RUNTIME_READY_VAR,
				objectGroup = "mono_debugger",
				includeCommandLineAPI = false,
				silent = false,
				returnByValue = true
			});
			this.ctx_id = ctx_id;
			this.aux_ctx_data = aux_data;

			Debug ("checking if the runtime is ready");
			var res = await SendCommand ("Runtime.evaluate", o, token);
			var is_ready = res.Value? ["result"]? ["value"]?.Value<bool> ();
			//Debug ($"\t{is_ready}");
			if (is_ready.HasValue && is_ready.Value == true) {
				Debug ("RUNTIME LOOK READY. GO TIME!");
				await RuntimeReady (token);
			}
		}


		async Task OnResume (CancellationToken token)
		{
			//discard frames
			this.current_callstack = null;
			await Task.CompletedTask;
		}

		async Task Step (int msg_id, StepKind kind, CancellationToken token)
		{

			var o = JObject.FromObject (new {
				expression = string.Format (MonoCommands.START_SINGLE_STEPPING, (int)kind),
				objectGroup = "mono_debugger",
				includeCommandLineAPI = false,
				silent = false,
				returnByValue = true,
			});

			var res = await SendCommand ("Runtime.evaluate", o, token);

			SendResponse (msg_id, Result.Ok (new JObject ()), token);

			this.current_callstack = null;

			await SendCommand ("Debugger.resume", new JObject (), token);
		}

		async Task GetScopeProperties (int msg_id, int scope_id, CancellationToken token)
		{
			var scope = this.current_callstack.FirstOrDefault (s => s.Id == scope_id);
			var vars = scope.Method.GetLiveVarsAt (scope.Location.CliLocation.Offset);


			var var_ids = string.Join (",", vars.Select (v => v.Index));

			var o = JObject.FromObject (new {
				expression = string.Format (MonoCommands.GET_SCOPE_VARIABLES, scope.Id, var_ids),
				objectGroup = "mono_debugger",
				includeCommandLineAPI = false,
				silent = false,
				returnByValue = true,
			});

			var res = await SendCommand ("Runtime.evaluate", o, token);

			//if we fail we just buble that to the IDE (and let it panic over it)
			if (res.IsErr) {
				SendResponse (msg_id, res, token);
				return;
			}

			var values = res.Value? ["result"]? ["value"]?.Values<JObject> ().ToArray ();

			var var_list = new List<JObject> ();

			// Trying to inspect the stack frame for DotNetDispatcher::InvokeSynchronously
			// results in a "Memory access out of bounds", causing 'values' to be null,
			// so skip returning variable values in that case.
			for (int i = 0; values != null && i < vars.Length; ++i) {
				var_list.Add (JObject.FromObject (new {
					name = vars [i].Name,
					value = values [i] ["value"]
				}));

			}
			o = JObject.FromObject (new {
				result = var_list
			});

			SendResponse (msg_id, Result.Ok (o), token);
		}

		async Task<Result> EnableBreakPoint (Breakpoint bp, CancellationToken token)
		{
			var asm_name = bp.Location.CliLocation.Method.Assembly.Name;
			var method_token = bp.Location.CliLocation.Method.Token;
			var il_offset = bp.Location.CliLocation.Offset;

			var o = JObject.FromObject (new {
				expression = string.Format (MonoCommands.SET_BREAK_POINT, asm_name, method_token, il_offset),
				objectGroup = "mono_debugger",
				includeCommandLineAPI = false,
				silent = false,
				returnByValue = true,
			});

			var res = await SendCommand ("Runtime.evaluate", o, token);
			var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

			if (ret_code.HasValue) {
				bp.RemoteId = ret_code.Value;
				bp.State = BreakPointState.Active;
				//Debug ($"BP local id {bp.LocalId} enabled with remote id {bp.RemoteId}");
			}

			return res;
		}

		async Task RuntimeReady (CancellationToken token)
		{

			var o = JObject.FromObject (new {
				expression = MonoCommands.GET_LOADED_FILES,
				objectGroup = "mono_debugger",
				includeCommandLineAPI = false,
				silent = false,
				returnByValue = true,
			});
			var loaded_pdbs = await SendCommand ("Runtime.evaluate", o, token);
			var the_value = loaded_pdbs.Value? ["result"]? ["value"];
			var the_pdbs = the_value?.ToObject<string[]> ();
			this.store = new DebugStore (the_pdbs);

			foreach (var s in store.AllSources ()) {
				var ok = JObject.FromObject (new {
					scriptId = s.SourceId.ToString (),
					url = s.Url,
					executionContextId = this.ctx_id,
					hash = s.DocHashCode,
					executionContextAuxData = this.aux_ctx_data
				});
				//Debug ($"\tsending {s.Url}");
				SendEvent ("Debugger.scriptParsed", ok, token);
			}

			o = JObject.FromObject (new {
				expression = MonoCommands.CLEAR_ALL_BREAKPOINTS,
				objectGroup = "mono_debugger",
				includeCommandLineAPI = false,
				silent = false,
				returnByValue = true,
			});

			var clear_result = await SendCommand ("Runtime.evaluate", o, token);
			if (clear_result.IsErr) {
				Debug ($"Failed to clear breakpoints due to {clear_result}");
			}


			runtime_ready = true;

			foreach (var bp in breakpoints) {
				if (bp.State != BreakPointState.Pending)
					continue;
				var res = await EnableBreakPoint (bp, token);
				var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

				//if we fail we just buble that to the IDE (and let it panic over it)
				if (!ret_code.HasValue) {
					//FIXME figure out how to inform the IDE of that.
					Info ($"FAILED TO ENABLE BP {bp.LocalId}");
					bp.State = BreakPointState.Disabled;
				}
			}
		}

		async Task<bool> RemoveBreakpoint(int msg_id, JObject args, CancellationToken token) {
			var bpid = args? ["breakpointId"]?.Value<string> ();
			if (bpid?.StartsWith ("dotnet:") != true)
				return false;

			var the_id = int.Parse (bpid.Substring ("dotnet:".Length));

			var bp = breakpoints.FirstOrDefault (b => b.LocalId == the_id);
			if (bp == null) {
				Info ($"Could not find dotnet bp with id {the_id}");
				return false;
			}

			breakpoints.Remove (bp);
			//FIXME verify result (and log?)
			var res = await RemoveBreakPoint (bp, token);

			return true;
		}


		async Task<Result> RemoveBreakPoint (Breakpoint bp, CancellationToken token)
		{
			var o = JObject.FromObject (new {
				expression = string.Format (MonoCommands.REMOVE_BREAK_POINT, bp.RemoteId),
				objectGroup = "mono_debugger",
				includeCommandLineAPI = false,
				silent = false,
				returnByValue = true,
			});

			var res = await SendCommand ("Runtime.evaluate", o, token);
			var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

			if (ret_code.HasValue) {
				bp.RemoteId = -1;
				bp.State = BreakPointState.Disabled;
			}

			return res;
		}

		async Task SetBreakPoint (int msg_id, BreakPointRequest req, CancellationToken token)
		{
			var bp_loc = store.FindBestBreakpoint (req);
			Info ($"BP request for '{req}' runtime ready {runtime_ready} location '{bp_loc}'");
			if (bp_loc == null) {

				Info ($"Could not resolve breakpoint request: {req}");
				SendResponse (msg_id, Result.Err(JObject.FromObject (new {
					code = (int)MonoErrorCodes.BpNotFound,
					message = $"C# Breakpoint at {req} not found."
				})), token);
				return;
			}

			Breakpoint bp = null;
			if (!runtime_ready) {
				bp = new Breakpoint (bp_loc, local_breakpoint_id++, BreakPointState.Pending);
			} else {
				bp = new Breakpoint (bp_loc, local_breakpoint_id++, BreakPointState.Disabled);

				var res = await EnableBreakPoint (bp, token);
				var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

				//if we fail we just buble that to the IDE (and let it panic over it)
				if (!ret_code.HasValue) {
					SendResponse (msg_id, res, token);
					return;
				}
			}

			var locations = new List<JObject> ();

			locations.Add (JObject.FromObject (new {
				scriptId = bp_loc.Id.ToString (),
				lineNumber = bp_loc.Line,
				columnNumber = bp_loc.Column
			}));

			breakpoints.Add (bp);

			var ok = JObject.FromObject (new {
				breakpointId = $"dotnet:{bp.LocalId}",
				locations = locations,
			});

			SendResponse (msg_id, Result.Ok (ok), token);
		}

		bool GetPossibleBreakpoints (int msg_id, SourceLocation start, SourceLocation end, CancellationToken token)
		{
			var bps = store.FindPossibleBreakpoints (start, end);
			if (bps == null)
				return false;

			var loc = new List<JObject> ();
			foreach (var b in bps) {
				loc.Add (b.ToJObject ());
			}

			var o = JObject.FromObject (new {
				locations = loc
			});

			SendResponse (msg_id, Result.Ok (o), token);

			return true;
		}

		void OnCompileDotnetScript (int msg_id, CancellationToken token)
		{
			var o = JObject.FromObject (new { });

			SendResponse (msg_id, Result.Ok (o), token);

		}

		void OnGetScriptSource (int msg_id, string script_id, CancellationToken token)
		{
			var id = new SourceId (script_id);
			var src_file = store.GetFileById (id);

			var res = new StringWriter ();
			res.WriteLine ($"//dotnet:{id}");

			using (var f = new StreamReader (File.Open (src_file.LocalPath, FileMode.Open))) {
				res.Write (f.ReadToEnd ());
			}

			var o = JObject.FromObject (new {
				scriptSource = res.ToString ()
			});

			SendResponse (msg_id, Result.Ok (o), token);
		}
	}
}
