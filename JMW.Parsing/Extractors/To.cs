using JMW.Extensions;
using JMW.Parsing.Compile;
using System.Collections.Generic;

namespace JMW.Parsing.Extractors
{
    public class To : Base
    {
        public const string NAME = "to";

        public override string Parse(string s)
        {
            var isCaseInsensitive = this.Search.Mods.Contains(Constants.CASE_INSENSITIVE); // case insensitive
            var shouldTripOutput = this.Search.Mods.Contains(Constants.TRIM); // trim the output
            var includeSearchItem = this.Search.Mods.Contains(Constants.WITH_VALUE);
            var queryArray = this.QueryArray();

            var r = includeSearchItem switch
            {
                true when this.Quantifier > 0
                    => s.ParseToDesignatedIndexOf_PlusLength(this.Quantifier, isCaseInsensitive, queryArray),
                true when this.Search.Mods.Contains(Constants.LAST)
                    => s.ParseToLastIndexOf_PlusLength(isCaseInsensitive, queryArray),
                true
                    => s.ParseToIndexOf_PlusLength(isCaseInsensitive, queryArray),
                _ => s.ParseToIndexOf(isCaseInsensitive, queryArray),
            };

            return shouldTripOutput ? r.Trim() : r;
        }

        public To(Tag t) : base(t)
        {
        }

        public To(List<string> search, string mods, int q) : base(search, mods, q, -1)
        {
        }
    }
}