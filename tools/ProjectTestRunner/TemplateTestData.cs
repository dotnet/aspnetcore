using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace ProjectTestRunner
{
    public class TemplateTestData : IXunitSerializable
    {
        public TemplateTestData()
        {
        }

        public string Name { get; set; }
        public string Variation { get; set; }
        public string CreateCommand { get; set; }
        public string[] Paths { get; set; }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Name = info.GetValue<string>(nameof(Name));
            Variation = info.GetValue<string>(nameof(Variation));
            CreateCommand = info.GetValue<string>(nameof(CreateCommand));
            Paths = info.GetValue<string[]>(nameof(Paths));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Name), Name, typeof(string));
            info.AddValue(nameof(Variation), Variation, typeof(string));
            info.AddValue(nameof(CreateCommand), CreateCommand, typeof(string));
            info.AddValue(nameof(Paths), Paths, typeof(string[]));
        }

        public override string ToString()
        {
            return Name;
        }
    }

}
