using System;
using System.IO;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Mono.Cecil.Pdb;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace WebAssembly.Net.Debugging {
	internal class BreakPointRequest {
		public string Assembly { get; private set; }
		public string File { get; private set; }
		public int Line { get; private set; }
		public int Column { get; private set; }

		public override string ToString () {
			return $"BreakPointRequest Assembly: {Assembly} File: {File} Line: {Line} Column: {Column}";
		}

		public static BreakPointRequest Parse (JObject args, DebugStore store)
		{
			// Events can potentially come out of order, so DebugStore may not be initialized
			// The BP being set in these cases are JS ones, which we can safely ignore
			if (args == null || store == null)
				return null;

			var url = args? ["url"]?.Value<string> ();
			if (url == null) {
				var urlRegex = args?["urlRegex"].Value<string>();
				var sourceFile = store?.GetFileByUrlRegex (urlRegex);

				url = sourceFile?.DotNetUrl;
			}

			if (url != null && !url.StartsWith ("dotnet://", StringComparison.InvariantCulture)) {
				var sourceFile = store.GetFileByUrl (url);
				url = sourceFile?.DotNetUrl;
			}

			if (url == null)
				return null;

			var parts = ParseDocumentUrl (url);
			if (parts.Assembly == null)
				return null;

			var line = args? ["lineNumber"]?.Value<int> ();
			var column = args? ["columnNumber"]?.Value<int> ();
			if (line == null || column == null)
				return null;

			return new BreakPointRequest () {
				Assembly = parts.Assembly,
				File = parts.DocumentPath,
				Line = line.Value,
				Column = column.Value
			};
		}

		static (string Assembly, string DocumentPath) ParseDocumentUrl (string url)
		{
			if (Uri.TryCreate (url, UriKind.Absolute, out var docUri) && docUri.Scheme == "dotnet") {
				return (
					docUri.Host,
					docUri.PathAndQuery.Substring (1)
				);
			} else {
				return (null, null);
			}
		}
	}

	internal class VarInfo {
		public VarInfo (VariableDebugInformation v)
		{
			this.Name = v.Name;
			this.Index = v.Index;
		}

		public VarInfo (ParameterDefinition p)
		{
			this.Name = p.Name;
			this.Index = (p.Index + 1) * -1;
		}

		public string Name { get; private set; }
		public int Index { get; private set; }

		public override string ToString ()
		{
			return $"(var-info [{Index}] '{Name}')";
		}
	}


	internal class CliLocation {
		public CliLocation (MethodInfo method, int offset)
		{
			Method = method;
			Offset = offset;
		}

		public MethodInfo Method { get; private set; }
		public int Offset { get; private set; }
	}


	internal class SourceLocation {
		SourceId id;
		int line;
		int column;
		CliLocation cliLoc;

		public SourceLocation (SourceId id, int line, int column)
		{
			this.id = id;
			this.line = line;
			this.column = column;
		}

		public SourceLocation (MethodInfo mi, SequencePoint sp)
		{
			this.id = mi.SourceId;
			this.line = sp.StartLine - 1;
			this.column = sp.StartColumn - 1;
			this.cliLoc = new CliLocation (mi, sp.Offset);
		}

		public SourceId Id { get => id; }
		public int Line { get => line; }
		public int Column { get => column; }
		public CliLocation CliLocation => this.cliLoc;

		public override string ToString ()
		{
			return $"{id}:{Line}:{Column}";
		}

		public static SourceLocation Parse (JObject obj)
		{
			if (obj == null)
				return null;

			var id = SourceId.TryParse (obj ["scriptId"]?.Value<string> ());
			var line = obj ["lineNumber"]?.Value<int> ();
			var column = obj ["columnNumber"]?.Value<int> ();
			if (id == null || line == null || column == null)
				return null;

			return new SourceLocation (id, line.Value, column.Value);
		}

		internal JObject ToJObject ()
		{
			return JObject.FromObject (new {
				scriptId = id.ToString (),
				lineNumber = line,
				columnNumber = column
			});
		}

	}

	internal class SourceId {
		readonly int assembly, document;

		public int Assembly => assembly;
		public int Document => document;

		internal SourceId (int assembly, int document)
		{
			this.assembly = assembly;
			this.document = document;
		}


		public SourceId (string id)
		{
			id = id.Substring ("dotnet://".Length);
			var sp = id.Split ('_');
			this.assembly = int.Parse (sp [0]);
			this.document = int.Parse (sp [1]);
		}

		public static SourceId TryParse (string id)
		{
			if (!id.StartsWith ("dotnet://", StringComparison.InvariantCulture))
				return null;
			return new SourceId (id);

		}
		public override string ToString ()
		{
			return $"dotnet://{assembly}_{document}";
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			SourceId that = obj as SourceId;
			return that.assembly == this.assembly && that.document == this.document;
		}

		public override int GetHashCode ()
		{
			return this.assembly.GetHashCode () ^ this.document.GetHashCode ();
		}

		public static bool operator == (SourceId a, SourceId b)
		{
			if ((object)a == null)
				return (object)b == null;
			return a.Equals (b);
		}

		public static bool operator != (SourceId a, SourceId b)
		{
			return !a.Equals (b);
		}
	}

	internal class MethodInfo {
		AssemblyInfo assembly;
		internal MethodDefinition methodDef;
		SourceFile source;

		public SourceId SourceId => source.SourceId;

		public string Name => methodDef.Name;

		public SourceLocation StartLocation { get; private set; }
		public SourceLocation EndLocation { get; private set; }
		public AssemblyInfo Assembly => assembly;
		public int Token => (int)methodDef.MetadataToken.RID;

		public MethodInfo (AssemblyInfo assembly, MethodDefinition methodDef, SourceFile source)
		{
			this.assembly = assembly;
			this.methodDef = methodDef;
			this.source = source;

			var sps = methodDef.DebugInformation.SequencePoints;
			if (sps != null && sps.Count > 0) {
				StartLocation = new SourceLocation (this, sps [0]);
				EndLocation = new SourceLocation (this, sps [sps.Count - 1]);
			}

		}

		public SourceLocation GetLocationByIl (int pos)
		{
			SequencePoint prev = null;
			foreach (var sp in methodDef.DebugInformation.SequencePoints) {
				if (sp.Offset > pos)
					break;
				prev = sp;
			}

			if (prev != null)
				return new SourceLocation (this, prev);

			return null;
		}

		public VarInfo [] GetLiveVarsAt (int offset)
		{
			var res = new List<VarInfo> ();

			res.AddRange (methodDef.Parameters.Select (p => new VarInfo (p)));

			res.AddRange (methodDef.DebugInformation.GetScopes ()
				.Where (s => s.Start.Offset <= offset && (s.End.IsEndOfMethod || s.End.Offset > offset))
				.SelectMany (s => s.Variables)
				.Where (v => !v.IsDebuggerHidden)
				.Select (v => new VarInfo (v)));

			return res.ToArray ();
		}
	}

	internal class AssemblyInfo {
		static int next_id;
		ModuleDefinition image;
		readonly int id;
		Dictionary<int, MethodInfo> methods = new Dictionary<int, MethodInfo> ();
		Dictionary<string, string> sourceLinkMappings = new Dictionary<string, string>();
		readonly List<SourceFile> sources = new List<SourceFile>();
		internal string Url { get; private set; }

		public AssemblyInfo (string url, byte[] assembly, byte[] pdb)
		{
			lock (typeof (AssemblyInfo)) {
				this.id = ++next_id;
			}

			try {
				Url = url;
				ReaderParameters rp = new ReaderParameters (/*ReadingMode.Immediate*/);
				rp.ReadSymbols = true;
				rp.SymbolReaderProvider = new PdbReaderProvider ();
				if (pdb != null)
					rp.SymbolStream = new MemoryStream (pdb);

				rp.ReadingMode = ReadingMode.Immediate;
				rp.InMemory = true;

				this.image = ModuleDefinition.ReadModule (new MemoryStream (assembly), rp);
			} catch (BadImageFormatException ex) {
				Console.WriteLine ($"Failed to read assembly as portable PDB: {ex.Message}");
			} catch (ArgumentNullException) {
				if (pdb != null)
					throw;
			}

			if (this.image == null) {
				ReaderParameters rp = new ReaderParameters (/*ReadingMode.Immediate*/);
				if (pdb != null) {
					rp.ReadSymbols = true;
					rp.SymbolReaderProvider = new PdbReaderProvider ();
					rp.SymbolStream = new MemoryStream (pdb);
				}

				rp.ReadingMode = ReadingMode.Immediate;
				rp.InMemory = true;

				this.image = ModuleDefinition.ReadModule (new MemoryStream (assembly), rp);
			}

			Populate ();
		}

		public AssemblyInfo ()
		{
		}

		void Populate ()
		{
            ProcessSourceLink();

			var d2s = new Dictionary<Document, SourceFile> ();

			Func<Document, SourceFile> get_src = (doc) => {
				if (doc == null)
					return null;
				if (d2s.ContainsKey (doc))
					return d2s [doc];
				var src = new SourceFile (this, sources.Count, doc, GetSourceLinkUrl (doc.Url));
				sources.Add (src);
				d2s [doc] = src;
				return src;
			};

			foreach (var m in image.GetTypes().SelectMany(t => t.Methods)) {
				Document first_doc = null;
				foreach (var sp in m.DebugInformation.SequencePoints) {
					if (first_doc == null && !sp.Document.Url.EndsWith (".g.cs")) {
						first_doc = sp.Document;
					}
					//  else if (first_doc != sp.Document) {
					//	//FIXME this is needed for (c)ctors in corlib
					//	throw new Exception ($"Cant handle multi-doc methods in {m}");
					//}
				}

				if (first_doc == null) {
					// all generated files
					first_doc = m.DebugInformation.SequencePoints.FirstOrDefault ()?.Document;
				}

				if (first_doc != null) {
					var src = get_src (first_doc);
					var mi = new MethodInfo (this, m, src);
					int mt = (int)m.MetadataToken.RID;
					this.methods [mt] = mi;
					if (src != null)
						src.AddMethod (mi);
				}
			}
		}

		private void ProcessSourceLink ()
		{
			var sourceLinkDebugInfo = image.CustomDebugInformations.FirstOrDefault (i => i.Kind == CustomDebugInformationKind.SourceLink);

			if (sourceLinkDebugInfo != null) {
				var sourceLinkContent = ((SourceLinkDebugInformation)sourceLinkDebugInfo).Content;

				if (sourceLinkContent != null) {
					var jObject = JObject.Parse (sourceLinkContent) ["documents"];
					sourceLinkMappings = JsonConvert.DeserializeObject<Dictionary<string, string>> (jObject.ToString ());
				}
			}
		}

		private Uri GetSourceLinkUrl (string document)
		{
			if (sourceLinkMappings.TryGetValue (document, out string url)) {
				return new Uri (url);
			}

			foreach (var sourceLinkDocument in sourceLinkMappings) {
				string key = sourceLinkDocument.Key;

				if (Path.GetFileName (key) != "*") {
					continue;
				}

				var keyTrim = key.TrimEnd ('*');

				if (document.StartsWith(keyTrim, StringComparison.OrdinalIgnoreCase)) {
					var docUrlPart = document.Replace (keyTrim, "");
					return new Uri (sourceLinkDocument.Value.TrimEnd ('*') + docUrlPart);
				}
			}

			return null;
		}

		private string GetRelativePath (string relativeTo, string path)
		{
			var uri = new Uri (relativeTo, UriKind.RelativeOrAbsolute);
			var rel = Uri.UnescapeDataString (uri.MakeRelativeUri (new Uri (path, UriKind.RelativeOrAbsolute)).ToString ()).Replace (Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			if (rel.Contains (Path.DirectorySeparatorChar.ToString ()) == false) {
				rel = $".{ Path.DirectorySeparatorChar }{ rel }";
			}
			return rel;
		}

		public IEnumerable<SourceFile> Sources {
			get { return this.sources; }
		}

		public int Id => id;
		public string Name => image.Name;

		public SourceFile GetDocById (int document)
		{
			return sources.FirstOrDefault (s => s.SourceId.Document == document);
		}

		public MethodInfo GetMethodByToken (int token)
		{
			methods.TryGetValue (token, out var value);
			return value;
		}
	}

	internal class SourceFile {
		HashSet<MethodInfo> methods;
		AssemblyInfo assembly;
		int id;
		Document doc;

		internal SourceFile (AssemblyInfo assembly, int id, Document doc, Uri sourceLinkUri)
		{
			this.methods = new HashSet<MethodInfo> ();
			this.SourceLinkUri = sourceLinkUri;
			this.assembly = assembly;
			this.id = id;
			this.doc = doc;
			this.DebuggerFileName = doc.Url.Replace ("\\", "/").Replace (":", "");

			this.SourceUri = new Uri ((Path.IsPathRooted (doc.Url) ? "file://" : "") + doc.Url, UriKind.RelativeOrAbsolute);
			if (SourceUri.IsFile && File.Exists (SourceUri.LocalPath)) {
				this.Url = this.SourceUri.ToString ();
			} else {
				this.Url = DotNetUrl;
			}

		}

		internal void AddMethod (MethodInfo mi)
		{
			this.methods.Add (mi);
		}
		public string DebuggerFileName { get; }
		public string Url { get; }
		public string AssemblyName => assembly.Name;
		public string DotNetUrl => $"dotnet://{assembly.Name}/{DebuggerFileName}";
		public string DocHashCode => "abcdee" + id;
		public SourceId SourceId => new SourceId (assembly.Id, this.id);
		public Uri SourceLinkUri { get; }
		public Uri SourceUri { get; }

		public IEnumerable<MethodInfo> Methods => this.methods;
	}

	internal class DebugStore {
        // MonoProxy proxy;  - commenting out because never gets assigned
		List<AssemblyInfo> assemblies = new List<AssemblyInfo> ();
		HttpClient client = new HttpClient ();

		class DebugItem {
			public string Url { get; set; }
			public Task<byte[][]> Data { get; set; }
		}

		public async Task Load (SessionId sessionId, string [] loaded_files, CancellationToken token)
		{
			static bool MatchPdb (string asm, string pdb)
				=> Path.ChangeExtension (asm, "pdb") == pdb;

			var asm_files = new List<string> ();
			var pdb_files = new List<string> ();
			foreach (var file_name in loaded_files) {
				if (file_name.EndsWith (".pdb", StringComparison.OrdinalIgnoreCase))
					pdb_files.Add (file_name);
				else
					asm_files.Add (file_name);
			}

			List<DebugItem> steps = new List<DebugItem> ();
			foreach (var url in asm_files) {
				try {
					var pdb = pdb_files.FirstOrDefault (n => MatchPdb (url, n));
					steps.Add (
						new DebugItem {
								Url = url,
								Data = Task.WhenAll (client.GetByteArrayAsync (url), pdb != null ? client.GetByteArrayAsync (pdb) : Task.FromResult<byte []> (null))
						});
				} catch (Exception e) {
					Console.WriteLine ($"Failed to read {url} ({e.Message})");
					var o = JObject.FromObject (new {
						entry = new {
							source = "other",
							level = "warning",
							text = $"Failed to read {url} ({e.Message})"
						}
					});
					// proxy.SendEvent (sessionId, "Log.entryAdded", o, token); - commenting out because `proxy` would always be null

				}
			}

			foreach (var step in steps) {
				try {
					var bytes = await step.Data;
					assemblies.Add (new AssemblyInfo (step.Url, bytes[0], bytes[1]));
				} catch (Exception e) {
					Console.WriteLine ($"Failed to Load {step.Url} ({e.Message})");
				}
			}
		}

		public IEnumerable<SourceFile> AllSources ()
			=> assemblies.SelectMany (a => a.Sources);

		public SourceFile GetFileById (SourceId id)
			=> AllSources ().FirstOrDefault (f => f.SourceId.Equals (id));

		public AssemblyInfo GetAssemblyByName (string name)
			=> assemblies.FirstOrDefault (a => a.Name.Equals (name, StringComparison.InvariantCultureIgnoreCase));

		/*
		V8 uses zero based indexing for both line and column.
		PPDBs uses one based indexing for both line and column.
		*/
		static bool Match (SequencePoint sp, SourceLocation start, SourceLocation end)
		{
			var spStart = (Line: sp.StartLine - 1, Column: sp.StartColumn - 1);
			var spEnd = (Line: sp.EndLine - 1, Column: sp.EndColumn - 1);

			if (start.Line > spStart.Line)
				return false;
			if (start.Column > spStart.Column && start.Line == sp.StartLine)
				return false;

			if (end.Line < spEnd.Line)
				return false;

			if (end.Column < spEnd.Column && end.Line == spEnd.Line)
				return false;

			return true;
		}

		public List<SourceLocation> FindPossibleBreakpoints (SourceLocation start, SourceLocation end)
		{
			//XXX FIXME no idea what todo with locations on different files
			if (start.Id != end.Id)
				return null;
			var src_id = start.Id;

			var doc = GetFileById (src_id);

			var res = new List<SourceLocation> ();
			if (doc == null) {
				//FIXME we need to write up logging here
				Console.WriteLine ($"Could not find document {src_id}");
				return res;
			}

			foreach (var m in doc.Methods) {
				foreach (var sp in m.methodDef.DebugInformation.SequencePoints) {
					if (Match (sp, start, end))
						res.Add (new SourceLocation (m, sp));
				}
			}
			return res;
		}

		/*
		V8 uses zero based indexing for both line and column.
		PPDBs uses one based indexing for both line and column.
		*/
		static bool Match (SequencePoint sp, int line, int column)
		{
			var bp = (line: line + 1, column: column + 1);

			if (sp.StartLine > bp.line || sp.EndLine < bp.line)
				return false;

			//Chrome sends a zero column even if getPossibleBreakpoints say something else
			if (column == 0)
				return true;

			if (sp.StartColumn > bp.column && sp.StartLine == bp.line)
				return false;

			if (sp.EndColumn < bp.column && sp.EndLine == bp.line)
				return false;

			return true;
		}

		public SourceLocation FindBestBreakpoint (BreakPointRequest req)
		{
			var asm = assemblies.FirstOrDefault (a => a.Name.Equals (req.Assembly, StringComparison.OrdinalIgnoreCase));
			var src = asm?.Sources?.FirstOrDefault (s => s.DebuggerFileName.Equals (req.File, StringComparison.OrdinalIgnoreCase));

			if (src == null)
				return null;

			foreach (var m in src.Methods) {
				foreach (var sp in m.methodDef.DebugInformation.SequencePoints) {
					//FIXME handle multi doc methods
					if (Match (sp, req.Line, req.Column))
						return new SourceLocation (m, sp);
				}
			}

			return null;
		}

		public string ToUrl (SourceLocation location)
			=> location != null ? GetFileById (location.Id).Url : "";

		public SourceFile GetFileByUrlRegex (string urlRegex)
		{
			var regex = new Regex (urlRegex);
			return AllSources ().FirstOrDefault (file => regex.IsMatch (file.Url.ToString()));
		}

		public SourceFile GetFileByUrl (string url)
			=> AllSources ().FirstOrDefault (file => file.Url.ToString() == url);
	}
}
