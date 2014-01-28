
namespace Microsoft.AspNet.Routing.Tree
{
    internal class ParameterSegment : ITreeSegment
    {
        public ParameterSegment(string name)
        {
            this.Name = name;
        }

        public string Name
        {
            get;
            private set;
        }
    }
}
