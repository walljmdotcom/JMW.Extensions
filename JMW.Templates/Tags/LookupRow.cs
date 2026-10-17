using System;
using System.Collections.Generic;

namespace JMW.Templates.Tags
{
    public class LookupRow : TagHandlerBase
    {
        public const string TAG = "row";
        public override string TagName { get; } = TAG;
        public override HashSet<string> ALLOWEDPROPS { get; } = [ATTR_KEY];
        public const string ATTR_KEY = "key";

        private readonly Lookup? _tab;

        public LookupRow()
        {
        }

        public LookupRow(string name, Lookup data)
        {
            TagName = name;
            _tab = data;
        }

        public override void Handler(Tag tag, Interpreter interp)
        {
            if (_tab is null || _tab?.Name is null)
            {
                throw new InvalidOperationException();
            }

            TagHelpers.PrefixTags(tag.Children, _tab.Name);

            if (tag.Properties.TryGetValue(ATTR_KEY, out var attrKey))
            {
                _tab.Key = attrKey;

                // Handles all rows in sheet.
                for (_tab.CurrentRow = 0; _tab.CurrentRow < _tab.LookupTable[_tab.Key].Count; _tab.CurrentRow++)
                {
                    // we access the row using the CurrentRow value, but it need to be called for every row
                    interp.Eval(tag.Children);
                }
            }
            else
            {
                throw new System.Exception("The '" + ATTR_KEY + "' attribute is required.");
            }
        }
    }
}