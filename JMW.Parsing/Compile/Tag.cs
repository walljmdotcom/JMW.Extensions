using System.Collections.Generic;

namespace JMW.Parsing.Compile
{
    public class Tag
    {
        public TagTypes TagType { get; set; } = TagTypes.Object;

        private string _Name = string.Empty;

        public string Name
        {
            get { return this._Name; }
            set { this._Name = value.Trim().ToLowerInvariant(); }
        }

        public Dictionary<string, Tag> Properties { get; set; } = [];

        public object Value { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{this.Name}:{this.TagType}";
        }
    }
}
