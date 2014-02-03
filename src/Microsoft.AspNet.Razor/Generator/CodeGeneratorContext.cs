// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Utils;

namespace Microsoft.AspNet.Razor.Generator
{
    public class CodeGeneratorContext
    {
        internal const string DesignTimeHelperMethodName = "__RazorDesignTimeHelpers__";

        private CodeGeneratorContext()
        {
            ExpressionRenderingMode = ExpressionRenderingMode.WriteToOutput;
        }

        // Internal/Private state. Technically consumers might want to use some of these but they can implement them independently if necessary.
        // It's way safer to make them internal for now, especially with the code generator stuff in a bit of flux.
        internal ExpressionRenderingMode ExpressionRenderingMode { get; set; }
        public string SourceFile { get; internal set; }
        public string RootNamespace { get; private set; }
        public string ClassName { get; private set; }

        #region deletable
#if NET45
        // This section is #if'd because it contains SOME incompatible pieces but also will not be needed once we transition over 
        // to using the CodeTree

        private int _nextDesignTimePragmaId = 1;
        private bool _expressionHelperVariableWriten;
        private CodeMemberMethod _designTimeHelperMethod;
        private StatementBuffer _currentBuffer = new StatementBuffer();

        private Action<string, CodeLinePragma> StatementCollector { get; set; }
        private Func<CodeWriter> CodeWriterFactory { get; set; }

        public CodeCompileUnit CompileUnit { get; internal set; }

        public CodeNamespace Namespace { get; internal set; }

        public CodeTypeDeclaration GeneratedClass { get; internal set; }
        public CodeMemberMethod TargetMethod { get; set; }
        public IDictionary<int, GeneratedCodeMapping> CodeMappings { get; private set; }
        public string CurrentBufferedStatement
        {
            get { return _currentBuffer == null ? String.Empty : _currentBuffer.Builder.ToString(); }
        }

        public void AddDesignTimeHelperStatement(CodeSnippetStatement statement)
        {
            if (_designTimeHelperMethod == null)
            {
                _designTimeHelperMethod = new CodeMemberMethod()
                {
                    Name = DesignTimeHelperMethodName,
                    Attributes = MemberAttributes.Private
                };
                _designTimeHelperMethod.Statements.Add(
                    new CodeSnippetStatement(BuildCodeString(cw => cw.WriteDisableUnusedFieldWarningPragma())));
                _designTimeHelperMethod.Statements.Add(
                    new CodeSnippetStatement(BuildCodeString(cw => cw.WriteRestoreUnusedFieldWarningPragma())));
                GeneratedClass.Members.Insert(0, _designTimeHelperMethod);
            }
            _designTimeHelperMethod.Statements.Insert(_designTimeHelperMethod.Statements.Count - 1, statement);
        }


        [SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "generatedCodeStart+1", Justification = "There is no risk of overflow in this case")]
        public int AddCodeMapping(SourceLocation sourceLocation, int generatedCodeStart, int generatedCodeLength)
        {
            if (generatedCodeStart == Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("generatedCodeStart");
            }

            GeneratedCodeMapping mapping = new GeneratedCodeMapping(
                startOffset: sourceLocation.AbsoluteIndex,
                startLine: sourceLocation.LineIndex + 1,
                startColumn: sourceLocation.CharacterIndex + 1,
                startGeneratedColumn: generatedCodeStart + 1,
                codeLength: generatedCodeLength);

            int id = _nextDesignTimePragmaId++;
            CodeMappings[id] = mapping;
            return id;
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "This method requires that a Span be provided")]
        public CodeLinePragma GenerateLinePragma(Span target)
        {
            return GenerateLinePragma(target, 0);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "This method requires that a Span be provided")]
        public CodeLinePragma GenerateLinePragma(Span target, int generatedCodeStart)
        {
            return GenerateLinePragma(target, generatedCodeStart, target.Content.Length);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "This method requires that a Span be provided")]
        public CodeLinePragma GenerateLinePragma(Span target, int generatedCodeStart, int codeLength)
        {
            return GenerateLinePragma(target.Start, generatedCodeStart, codeLength);
        }

        public CodeLinePragma GenerateLinePragma(SourceLocation start, int generatedCodeStart, int codeLength)
        {
            if (!String.IsNullOrEmpty(SourceFile))
            {
                if (Host.DesignTimeMode)
                {
                    int mappingId = AddCodeMapping(start, generatedCodeStart, codeLength);
                    return new CodeLinePragma(SourceFile, mappingId);
                }
                return new CodeLinePragma(SourceFile, start.LineIndex + 1);
            }
            return null;
        }

        public void BufferStatementFragment(Span sourceSpan)
        {
            BufferStatementFragment(sourceSpan.Content, sourceSpan);
        }

        public void BufferStatementFragment(string fragment)
        {
            BufferStatementFragment(fragment, null);
        }

        public void BufferStatementFragment(string fragment, Span sourceSpan)
        {
            if (sourceSpan != null && _currentBuffer.LinePragmaSpan == null)
            {
                _currentBuffer.LinePragmaSpan = sourceSpan;

                // Pad the output as necessary
                int start = _currentBuffer.Builder.Length;
                if (_currentBuffer.GeneratedCodeStart != null)
                {
                    start = _currentBuffer.GeneratedCodeStart.Value;
                }

                int paddingLength; // unused, in this case there is enough context in the original code to calculate the right padding length
                // (padded.Length - _currentBuffer.Builder.Length)

                string padded = CodeGeneratorPaddingHelper.Pad(Host, _currentBuffer.Builder.ToString(), sourceSpan, start, out paddingLength);
                _currentBuffer.GeneratedCodeStart = start + (padded.Length - _currentBuffer.Builder.Length);
                _currentBuffer.Builder.Clear();
                _currentBuffer.Builder.Append(padded);
            }
            _currentBuffer.Builder.Append(fragment);
        }

        public void MarkStartOfGeneratedCode()
        {
            _currentBuffer.MarkStart();
        }

        public void MarkEndOfGeneratedCode()
        {
            _currentBuffer.MarkEnd();
        }

        public void FlushBufferedStatement()
        {
            if (_currentBuffer.Builder.Length > 0)
            {
                CodeLinePragma pragma = null;
                if (_currentBuffer.LinePragmaSpan != null)
                {
                    int start = _currentBuffer.Builder.Length;
                    if (_currentBuffer.GeneratedCodeStart != null)
                    {
                        start = _currentBuffer.GeneratedCodeStart.Value;
                    }
                    int len = _currentBuffer.Builder.Length - start;
                    if (_currentBuffer.CodeLength != null)
                    {
                        len = _currentBuffer.CodeLength.Value;
                    }
                    pragma = GenerateLinePragma(_currentBuffer.LinePragmaSpan, start, len);
                }
                AddStatement(_currentBuffer.Builder.ToString(), pragma);
                _currentBuffer.Reset();
            }
        }

        public void AddStatement(string generatedCode)
        {
            AddStatement(generatedCode, null);
        }

        public void AddStatement(string body, CodeLinePragma pragma)
        {
            if (StatementCollector == null)
            {
                TargetMethod.Statements.Add(new CodeSnippetStatement(body) { LinePragma = pragma });
            }
            else
            {
                StatementCollector(body, pragma);
            }
        }

        public void EnsureExpressionHelperVariable()
        {
            if (!_expressionHelperVariableWriten)
            {
                GeneratedClass.Members.Insert(0,
                                              new CodeMemberField(typeof(object), "__o")
                                              {
                                                  Attributes = MemberAttributes.Private | MemberAttributes.Static
                                              });
                _expressionHelperVariableWriten = true;
            }
        }

        public IDisposable ChangeStatementCollector(Action<string, CodeLinePragma> collector)
        {
            Action<string, CodeLinePragma> oldCollector = StatementCollector;
            StatementCollector = collector;
            return new DisposableAction(() =>
            {
                StatementCollector = oldCollector;
            });
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "We explicitly want the lower-case string here")]
        public void AddContextCall(Span contentSpan, string methodName, bool isLiteral)
        {
            AddStatement(BuildCodeString(cw =>
            {
                cw.WriteStartMethodInvoke(methodName);
                if (!String.IsNullOrEmpty(TargetWriterName))
                {
                    cw.WriteSnippet(TargetWriterName);
                    cw.WriteParameterSeparator();
                }
                cw.WriteStringLiteral(Host.InstrumentedSourceFilePath);
                cw.WriteParameterSeparator();
                cw.WriteSnippet(contentSpan.Start.AbsoluteIndex.ToString(CultureInfo.InvariantCulture));
                cw.WriteParameterSeparator();
                cw.WriteSnippet(contentSpan.Content.Length.ToString(CultureInfo.InvariantCulture));
                cw.WriteParameterSeparator();
                cw.WriteSnippet(isLiteral.ToString().ToLowerInvariant());
                cw.WriteEndMethodInvoke();
                cw.WriteEndStatement();
            }));
        }

        internal CodeWriter CreateCodeWriter()
        {
            Debug.Assert(CodeWriterFactory != null);
            if (CodeWriterFactory == null)
            {
                throw new InvalidOperationException(RazorResources.CreateCodeWriter_NoCodeWriter);
            }
            return CodeWriterFactory();
        }

        internal string BuildCodeString(Action<CodeWriter> action)
        {
            using (CodeWriter cw = CodeWriterFactory())
            {
                action(cw);
                return cw.Content;
            }
        }

        private class StatementBuffer
        {
            public StringBuilder Builder = new StringBuilder();
            public int? GeneratedCodeStart;
            public int? CodeLength;
            public Span LinePragmaSpan;

            public void Reset()
            {
                Builder.Clear();
                GeneratedCodeStart = null;
                CodeLength = null;
                LinePragmaSpan = null;
            }

            public void MarkStart()
            {
                GeneratedCodeStart = Builder.Length;
            }

            public void MarkEnd()
            {
                CodeLength = Builder.Length - GeneratedCodeStart;
            }
        }
#endif
        #endregion

        public RazorEngineHost Host { get; private set; }
        public string TargetWriterName { get; set; }

        public CodeTreeBuilder CodeTreeBuilder { get; set; }

        public static CodeGeneratorContext Create(RazorEngineHost host, string className, string rootNamespace, string sourceFile, bool shouldGenerateLinePragmas)
        {
            return Create(host, null, className, rootNamespace, sourceFile, shouldGenerateLinePragmas);
        }

        internal static CodeGeneratorContext Create(RazorEngineHost host, Func<CodeWriter> writerFactory, string className, string rootNamespace, string sourceFile, bool shouldGenerateLinePragmas)
        {
            CodeGeneratorContext context = new CodeGeneratorContext()
            {
                CodeTreeBuilder = new CodeTreeBuilder(),
                Host = host,
                SourceFile = shouldGenerateLinePragmas ? sourceFile : null,
                RootNamespace = rootNamespace,
                ClassName = className,
#if NET45
                // This section is #if'd because it contains SOME incompatible pieces but also will not be needed once we transition over 
                // to using the CodeTree

                CodeWriterFactory = writerFactory,
                CompileUnit = new CodeCompileUnit(),
                Namespace = new CodeNamespace(rootNamespace),
                GeneratedClass = new CodeTypeDeclaration(className)
                {
                    IsClass = true
                },
                TargetMethod = new CodeMemberMethod()
                {
                    Name = host.GeneratedClassContext.ExecuteMethodName,
                    Attributes = MemberAttributes.Override | MemberAttributes.Public
                },
                CodeMappings = new Dictionary<int, GeneratedCodeMapping>()
#endif
            };
#if NET45
            // No CodeDOM in CoreCLR.

            context.CompileUnit.Namespaces.Add(context.Namespace);
            context.Namespace.Types.Add(context.GeneratedClass);
            context.GeneratedClass.Members.Add(context.TargetMethod);

            context.Namespace.Imports.AddRange(host.NamespaceImports
                                                   .Select(s => new CodeNamespaceImport(s))
                                                   .ToArray());
#endif
            return context;
        }
    }
}
