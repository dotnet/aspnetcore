using System;
using System.IO;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace WsProxy {
	internal class BreakPointRequest {
		public string Assembly { get; private set; }
		public string File { get; private set; }
		public int Line { get; private set; }
		public int Column { get; private set; }

		public override string ToString () {
			return $"BreakPointRequest Assembly: {Assembly} File: {File} Line: {Line} Column: {Column}";
		}

		public static BreakPointRequest Parse (JObject args)
		{
			if (args == null)
				return null;

			var url = args? ["url"]?.Value<string> ();
			if (!url.StartsWith ("dotnet://", StringComparison.InvariantCulture))
				return null;

			var parts = url.Substring ("dotnet://".Length).Split ('/');
			if (parts.Length != 2)
				return null;

			var line = args? ["lineNumber"]?.Value<int> ();
			var column = args? ["columnNumber"]?.Value<int> ();
			if (line == null || column == null)
				return null;

			return new BreakPointRequest () {
				Assembly = parts [0],
				File = parts [1],
				Line = line.Value,
				Column = column.Value
			};
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

		private MethodInfo method;
		private int offset;

		public CliLocation (MethodInfo method, int offset)
		{
			this.method = method;
			this.offset = offset;
		}

		public MethodInfo Method { get => method; }
		public int Offset { get => offset; }
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
			this.line = sp.StartLine;
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
		readonly List<SourceFile> sources = new List<SourceFile> ();

		public AssemblyInfo (byte[] assembly, byte[] pdb)
		{
			lock (typeof (AssemblyInfo)) {
				this.id = ++next_id;
			}

			ReaderParameters rp = new ReaderParameters (/*ReadingMode.Immediate*/);
			if (pdb != null) {
				rp.ReadSymbols = true;
				rp.SymbolReaderProvider = new PortablePdbReaderProvider ();
				rp.SymbolStream = new MemoryStream (pdb);
			}

			rp.InMemory = true;

			this.image = ModuleDefinition.ReadModule (new MemoryStream (assembly), rp);

			Populate ();
		}

		public AssemblyInfo ()
		{
		}

		void Populate ()
		{
			var d2s = new Dictionary<Document, SourceFile> ();

			Func<Document, SourceFile> get_src = (doc) => {
				if (doc == null)
					return null;
				if (d2s.ContainsKey (doc))
					return d2s [doc];
				var src = new SourceFile (this, sources.Count, doc);
				sources.Add (src);
				d2s [doc] = src;
				return src;
			};

			foreach (var m in image.GetTypes ().SelectMany (t => t.Methods)) {
				Document first_doc = null;
				foreach (var sp in m.DebugInformation.SequencePoints) {
					if (first_doc == null) {
						first_doc = sp.Document;
					} else if (first_doc != sp.Document) {
						//FIXME this is needed for (c)ctors in corlib
						throw new Exception ($"Cant handle multi-doc methods in {m}");
					}
				}

				var src = get_src (first_doc);
				var mi = new MethodInfo (this, m, src);
				int mt = (int)m.MetadataToken.RID;
				this.methods [mt] = mi;
				if (src != null)
					src.AddMethod (mi);

			}
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
			return methods [token];
		}

	}

	internal class SourceFile {
		HashSet<MethodInfo> methods;
		AssemblyInfo assembly;
		int id;
		Document doc;

		internal SourceFile (AssemblyInfo assembly, int id, Document doc)
		{
			this.methods = new HashSet<MethodInfo> ();
			this.assembly = assembly;
			this.id = id;
			this.doc = doc;
		}

		internal void AddMethod (MethodInfo mi)
		{
			this.methods.Add (mi);
		}
		public string FileName => Path.GetFileName (doc.Url);
		public string Url => $"dotnet://{assembly.Name}/{FileName}";
		public string DocHashCode => "abcdee" + id;
		public SourceId SourceId => new SourceId (assembly.Id, this.id);
		public string LocalPath => doc.Url;

		public IEnumerable<MethodInfo> Methods => this.methods;
	}

	internal class DebugStore {
		List<AssemblyInfo> assemblies = new List<AssemblyInfo> ();

		public DebugStore (string[] loaded_files)
		{
			bool MatchPdb (string asm, string pdb) {
				return Path.ChangeExtension (asm, "pdb") == pdb;
			}

			var asm_files = new List<string> ();
			var pdb_files = new List<string> ();
			foreach (var f in loaded_files) {
				var file_name = f.ToLower ();
				if (file_name.EndsWith (".pdb", StringComparison.Ordinal))
					pdb_files.Add (file_name);
				else
					asm_files.Add (file_name);
			}

			//FIXME make this parallel
			foreach (var p in asm_files) {
				var pdb = pdb_files.FirstOrDefault (n => MatchPdb (p, n));
				HttpClient h = new HttpClient ();
				var assembly_bytes = h.GetByteArrayAsync (p).Result;
				byte[] pdb_bytes = null;
				if (pdb != null)
					pdb_bytes = h.GetByteArrayAsync (pdb).Result;

				this.assemblies.Add (new AssemblyInfo (assembly_bytes, pdb_bytes));
			}
		}

		public IEnumerable<SourceFile> AllSources ()
		{
			foreach (var a in assemblies) {
				foreach (var s in a.Sources)
					yield return s;
			}

		}

		public SourceFile GetFileById (SourceId id)
		{
			return AllSources ().FirstOrDefault (f => f.SourceId.Equals (id));
		}

		public AssemblyInfo GetAssemblyByName (string name)
		{
			return assemblies.FirstOrDefault (a => a.Name.Equals (name, StringComparison.InvariantCultureIgnoreCase));
		}

		/*
		Matching logic here is hilarious and it goes like this:
		We inject one line at the top of all sources to make it easy to identify them [1].
		V8 uses zero based indexing for both line and column.
		PPDBs uses one based indexing for both line and column.
		Which means that:
		- for lines, values are already adjusted (v8 numbers come +1 due to the injected line)
		- for columns, we need to +1 the v8 numbers
		[1] It's so we can deal with the Runtime.compileScript ide cmd
		*/
		static bool Match (SequencePoint sp, SourceLocation start, SourceLocation end)
		{
			if (start.Line > sp.StartLine)
				return false;
			if ((start.Column + 1) > sp.StartColumn && start.Line == sp.StartLine)
				return false;

			if (end.Line < sp.EndLine)
				return false;

			if ((end.Column + 1) < sp.EndColumn && end.Line == sp.EndLine)
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
		Matching logic here is hilarious and it goes like this:
		We inject one line at the top of all sources to make it easy to identify them [1].
		V8 uses zero based indexing for both line and column.
		PPDBs uses one based indexing for both line and column.
		Which means that:
		- for lines, values are already adjusted (v8 numbers come + 1 due to the injected line)
		- for columns, we need to +1 the v8 numbers
		[1] It's so we can deal with the Runtime.compileScript ide cmd
		*/
		static bool Match (SequencePoint sp, int line, int column)
		{
			if (sp.StartLine > line || sp.EndLine < line)
				return false;

			//Chrome sends a zero column even if getPossibleBreakpoints say something else
			if (column == 0)
				return true;

			if (sp.StartColumn > (column + 1) && sp.StartLine == line)
				return false;

			if (sp.EndColumn < (column + 1) && sp.EndLine == line)
				return false;

			return true;
		}

		public SourceLocation FindBestBreakpoint (BreakPointRequest req)
		{
			var asm = this.assemblies.FirstOrDefault (a => a.Name == req.Assembly);
			var src = asm.Sources.FirstOrDefault (s => s.FileName == req.File);

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
		{
			return GetFileById (location.Id).Url;
		}
	}
}
