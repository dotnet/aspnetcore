namespace Microsoft.HttpRepl.OpenApi
{
    public class Parameter
    {
        public string Name { get; set; }

        public string Location { get; set; }

        public bool IsRequired { get; set; }

        public Schema Schema { get; set; }
    }
}