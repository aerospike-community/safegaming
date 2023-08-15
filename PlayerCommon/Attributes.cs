using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayerCommonDummy
{
#pragma warning disable CA1018 // Mark attributes with AttributeUsageAttribute
    public class BsonConstructor : Attribute
    {
        public BsonConstructor() { }
    }

    public class BsonIgnore : Attribute
    {
        public BsonIgnore() { }
    }

    public class BsonId : Attribute
    {
        public BsonId() { }
    }

    public class BsonElement : Attribute
    {
        public BsonElement() { }
        public BsonElement(string name) { this.Name = name; }

        public string Name { get; }
    }
#pragma warning restore CA1018 // Mark attributes with AttributeUsageAttribute
}
