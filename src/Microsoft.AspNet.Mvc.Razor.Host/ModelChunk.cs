using Microsoft.AspNet.Razor.Generator.Compiler;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class ModelChunk : Chunk
    {
        /// <summary>
        /// Represents the chunk for an @model statement.
        /// </summary>
        /// <param name="baseType">The base type of the view.</param>
        /// <param name="modelType">The type of the view's Model.</param>
	    public ModelChunk(string baseType, string modelType)
        {
            BaseType = baseType;
            ModelType = modelType;
        }

        public string BaseType { get; private set; }
        public string ModelType { get; private set; }
    }
}