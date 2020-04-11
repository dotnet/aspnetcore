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

	internal struct SessionId {
		public readonly string sessionId;

		public SessionId (string sessionId)
		{
			this.sessionId = sessionId;
		}

		public override int GetHashCode ()
			=> sessionId?.GetHashCode () ?? 0;

		public override bool Equals (object obj)
			=> (obj is SessionId) ? ((SessionId) obj).sessionId == sessionId : false;

		public override string ToString ()
			=> $"session-{sessionId}";
	}

	internal struct MessageId {
		public readonly string sessionId;
		public readonly int id;

		public MessageId (string sessionId, int id)
		{
			this.sessionId = sessionId;
			this.id = id;
		}

		public static implicit operator SessionId (MessageId id)
			=> new SessionId (id.sessionId);

		public override string ToString ()
			=> $"msg-{sessionId}:::{id}";

		public override int GetHashCode ()
			=> (sessionId?.GetHashCode () ?? 0) ^ id.GetHashCode ();

		public override bool Equals (object obj)
			=> (obj is MessageId) ? ((MessageId) obj).sessionId == sessionId && ((MessageId) obj).id == id : false;
	}

	internal struct Result {
		public JObject Value { get; private set; }
		public JObject Error { get; private set; }

		public bool IsOk => Value != null;
		public bool IsErr => Error != null;

		Result (JObject result, JObject error)
		{
			if (result != null && error != null)
				throw new ArgumentException ($"Both {nameof(result)} and {nameof(error)} arguments cannot be non-null.");

			bool resultHasError = String.Compare ((result? ["result"] as JObject)? ["subtype"]?. Value<string> (), "error") == 0;
			if (result != null && resultHasError) {
				this.Value = null;
				this.Error = result;
			} else {
				this.Value = result;
				this.Error = error;
			}
		}

		public static Result FromJson (JObject obj)
		{
			//Log ("protocol", $"from result: {obj}");
			return new Result (obj ["result"] as JObject, obj ["error"] as JObject);
		}

		public static Result Ok (JObject ok)
			=> new Result (ok, null);

		public static Result OkFromObject (object ok)
			=> Ok (JObject.FromObject(ok));

		public static Result Err (JObject err)
			=> new Result (null, err);

		public static Result Exception (Exception e)
			=> new Result (null, JObject.FromObject (new { message = e.Message }));

		public JObject ToJObject (MessageId target) {
			if (IsOk) {
				return JObject.FromObject (new {
					target.id,
					target.sessionId,
					result = Value
				});
			} else {
				return JObject.FromObject (new {
					target.id,
					target.sessionId,
					error = Error
				});
			}
		}

		public override string ToString ()
		{
			return $"[Result: IsOk: {IsOk}, IsErr: {IsErr}, Value: {Value?.ToString ()}, Error: {Error?.ToString ()} ]";
		}
	}

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

		public static MonoCommands GetObjectProperties (int objectId, bool expandValueTypes)
			=> new MonoCommands ($"MONO.mono_wasm_get_object_properties({objectId}, { (expandValueTypes ? "true" : "false") })");

		public static MonoCommands GetArrayValues (int objectId)
			=> new MonoCommands ($"MONO.mono_wasm_get_array_values({objectId})");

		public static MonoCommands GetArrayValueExpanded (int objectId, int idx)
			=> new MonoCommands ($"MONO.mono_wasm_get_array_value_expanded({objectId}, {idx})");

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

		int nextValueTypeId = 0;
		public Dictionary<string, JToken> ValueTypesCache = new Dictionary<string, JToken> ();
		public Dictionary<string, JToken> LocalsCache = new Dictionary<string, JToken> ();

		public DebugStore Store {
			get {
				if (store == null || !Source.Task.IsCompleted)
					return null;

				return store;
			}
		}

		public void ClearState ()
		{
			CallStack = null;
			ValueTypesCache.Clear ();
			LocalsCache.Clear ();
			nextValueTypeId = 0;
		}

		public int NextValueTypeId () => Interlocked.Increment (ref nextValueTypeId);

	}
}
