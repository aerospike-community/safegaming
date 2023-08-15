using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayerCommonDummy
{
#pragma warning disable CA1018 // Mark attributes with AttributeUsageAttribute
    internal class BsonConstructor : Attribute
    {
        public BsonConstructor() { }
    }

    internal class BsonIgnore : Attribute
    {
        public BsonIgnore() { }
    }

    internal class BsonId : Attribute
    {
        public BsonId() { }
    }

    internal class BsonElement : Attribute
    {
        public BsonElement() { }
        public BsonElement(string name) { this.Name = name; }

        public string Name { get; }
    }
#pragma warning restore CA1018 // Mark attributes with AttributeUsageAttribute
}
