namespace AspNetCoreSdkTests.Templates
{
    public class ReactReduxTemplate : ReactTemplate
    {
        public new static ReactReduxTemplate Instance { get; } = new ReactReduxTemplate();

        protected ReactReduxTemplate() { }

        public override string Name => "reactredux";
    }
}
