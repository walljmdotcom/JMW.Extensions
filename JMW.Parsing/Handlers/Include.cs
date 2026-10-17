using JMW.Extensions;
using JMW.Parsing.Compile;
using JMW.Parsing.Expressions;

namespace JMW.Parsing.Handlers
{
    public class Include
    {
        public const string NAME = "include";
        public const string START = "start";
        public const string STOP = "stop";
        public const string INCLUDESTART = "include_start";
        public const string INCLUDESTOP = "include_stop";

        public IExpression? Start { get; }
        public IExpression? Stop { get; }
        public bool IncludeStart { get; private set; } = false;
        public bool IncludeStop { get; private set; } = false;

        public Include(IExpression? start, IExpression? stop)
        {
            this.Start = start;
            this.Stop = stop;
        }

        public Include(Tag t)
        {
            if (t.Properties.TryGetValue(START, out var startTag))
            {
                this.Start = Expressions.Base.ToExpression(startTag);
            }
            if (t.Properties.TryGetValue(STOP, out var stopTag))
            {
                this.Stop = Expressions.Base.ToExpression(stopTag);
            }
            if (t.Properties.TryGetValue(INCLUDESTART, out var includeStartTag))
            {
                includeStartTag.Value.ToString()?.MaybeToBoolean().IfSuccess(v => this.IncludeStart = v);
            }
            if (t.Properties.TryGetValue(INCLUDESTOP, out var includeStopTag))
            {
                includeStopTag.Value.ToString()?.MaybeToBoolean().IfSuccess(v => this.IncludeStop = v);
            }
        }
    }
}