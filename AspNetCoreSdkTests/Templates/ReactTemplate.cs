namespace AspNetCoreSdkTests.Templates
{
    public class ReactTemplate : SpaBaseTemplate
    {
        public new static ReactTemplate Instance { get; } = new ReactTemplate();

        protected ReactTemplate() { }

        public override string Name => "react";
    }
}
