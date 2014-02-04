using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeTreeBuilder
    {
        private readonly Stack<ChunkBlock> _blockChain;
        private Chunk _lastChunk;

        public CodeTreeBuilder()
        {
            CodeTree = new CodeTree();
            _blockChain = new Stack<ChunkBlock>();
        }

        public CodeTree CodeTree { get; private set; }

        public void AddChunk(Chunk chunk, SyntaxTreeNode association, CodeGeneratorContext context, bool topLevel = false)
        {
            _lastChunk = chunk;

            chunk.Start = association.Start;
            chunk.Association = association;
            chunk.WriterName = context.TargetWriterName;

            // If we're not in the middle of a chunk block
            if (_blockChain.Count == 0 || topLevel == true)
            {
                CodeTree.Chunks.Add(chunk);
            }
            else
            {
                _blockChain.Peek().Children.Add(chunk);
            }
        }

        public void AddLiteralChunk(string literal, SyntaxTreeNode association, CodeGeneratorContext context)
        {
            if (_lastChunk is LiteralChunk)
            {
                ((LiteralChunk)_lastChunk).Text += literal;
            }
            else
            {
                AddChunk(new LiteralChunk
                {
                    Text = literal,
                }, association, context);
            }
        }

        public void AddExpressionChunk(string expression, ExpressionRenderingMode renderingMode, SyntaxTreeNode association, CodeGeneratorContext context)
        {
            AddChunk(new ExpressionChunk
            {
                Code = expression,
                RenderingMode = renderingMode
            }, association, context);
        }

        public void AddStatementChunk(string code, SyntaxTreeNode association, CodeGeneratorContext context)
        {
            AddChunk(new StatementChunk
            {
                Code = code,
            }, association, context);
        }

        public void AddUsingChunk(string usingNamespace, SyntaxTreeNode association, CodeGeneratorContext context)
        {
            AddChunk(new UsingChunk
            {
                Namespace = usingNamespace,
            }, association, context, topLevel: true);
        }

        public void AddTypeMemberChunk(string code, SyntaxTreeNode association, CodeGeneratorContext context)
        {
            AddChunk(new TypeMemberChunk
            {
                Code = code,
            }, association, context, topLevel: true);
        }

        public void AddLiteralCodeAttributeChunk(string code, SyntaxTreeNode association, CodeGeneratorContext context)
        {
            AddChunk(new LiteralCodeAttributeChunk
            {
                Code = code,
            }, association, context);
        }

        public void AddResolveUrlChunk(string url, SyntaxTreeNode association, CodeGeneratorContext context)
        {
            AddChunk(new ResolveUrlChunk
            {
                Url = url,
                RenderingMode = context.ExpressionRenderingMode
            }, association, context);
        }

        public void AddSetLayoutChunk(string layout, SyntaxTreeNode association, CodeGeneratorContext context)
        {
            AddChunk(new SetLayoutChunk
            {
                Layout = layout
            }, association, context);
        }

        public void AddSetBaseTypeChunk(string typeName, SyntaxTreeNode association, CodeGeneratorContext context)
        {
            AddChunk(new SetBaseTypeChunk
            {
                TypeName = typeName.Trim()
            }, association, context, topLevel: true);
        }

        public void AddSessionStateChunk(string value, SyntaxTreeNode association, CodeGeneratorContext context)
        {
            AddChunk(new SessionStateChunk
            {
                Value = value
            }, association, context, topLevel: true);
        }

        public T StartChunkBlock<T>(SyntaxTreeNode association, CodeGeneratorContext context) where T : ChunkBlock
        {
            return StartChunkBlock<T>(association, context, topLevel: false);
        }

        public T StartChunkBlock<T>(SyntaxTreeNode association, CodeGeneratorContext context, bool topLevel) where T : ChunkBlock
        {
            T chunk = (T)Activator.CreateInstance(typeof(T));

            AddChunk(chunk, association, context, topLevel);

            _blockChain.Push(chunk);

            return chunk;
        }

        public void EndChunkBlock()
        {
            _lastChunk = _blockChain.Pop();
        }
    }
}
