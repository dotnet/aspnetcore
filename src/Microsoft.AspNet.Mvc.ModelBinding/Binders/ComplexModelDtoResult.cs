namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class ComplexModelDtoResult
    {
        public ComplexModelDtoResult(object model/*, ModelValidationNode validationNode*/)
        {
            // TODO: Validation
            //if (validationNode == null)
            //{
            //    throw Error.ArgumentNull("validationNode");
            //}

            Model = model;
            //ValidationNode = validationNode;
        }

        public object Model { get; private set; }

        //public ModelValidationNode ValidationNode { get; private set; }
    }
}
