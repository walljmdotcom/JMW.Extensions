using System.Collections.Generic;

namespace JMW.Template
{
    public class Tag
    {
        public TagTypes TagType { get; set; } = TagTypes.Text;

        private string _Name = string.Empty;
        public string Name { get { return _Name; } set { _Name = value.Trim().ToLowerInvariant(); } }

        public Dictionary<string, string> Properties { get; set; } = [];

        public string TokenText { get; set; } = string.Empty;

        public List<Tag> Children { get; set; } = [];

        public bool HasChildren
        {
            get
            {
                return Children.Count > 0;
            }
        }

        public override string ToString()
        {
            return Name + ":" + TagType;
        }
    }
}
