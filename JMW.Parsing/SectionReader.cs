using JMW.Parsing.Expressions;
using JMW.Parsing.Handlers;
using System.IO;

namespace JMW.Parsing;

public class SectionReader
{
    private readonly StreamReader reader;

    private readonly IExpression? start;
    private readonly IExpression? stop;

    private readonly bool includeStartLine = false;
    private readonly bool includeStopLine = false;

    private bool started = false;
    private bool stopped = false;

    #region Constructors

    public SectionReader(StreamReader stream, IExpression? start, IExpression? stop) : this(stream, start, stop, false, false)
    {
    }

    public SectionReader(StreamReader stream, Include include)
    {
        this.reader = stream;
        this.start = include.Start;
        this.stop = include.Stop;
        this.includeStartLine = include.IncludeStart;
        this.includeStopLine = include.IncludeStop;
    }

    public SectionReader(StreamReader stream, IExpression? start, IExpression? stop, bool include_start_line, bool include_stop_line)
    {
        this.reader = stream;
        this.start = start;
        this.stop = stop;
        this.includeStartLine = include_start_line;
        this.includeStopLine = include_stop_line;
    }

    #endregion Constructors

    /// <summary>Reads a line of characters from the current stream and returns the data as a string.</summary>
    /// <returns>The next line from the input stream, or null if the end of the input stream is reached.</returns>
    /// <exception cref="T:System.OutOfMemoryException">There is insufficient memory to allocate a buffer for the returned string. </exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
    /// <filterpriority>1</filterpriority>
    public string? ReadLine()
    {
        if (this.start is null && this.stop is null)
        {
            return this.reader.ReadLine();
        }

        if (this.stopped)
        {
            return null;
        }

        string? line;
        while ((line = this.reader.ReadLine()) is not null)
        {
            if (!this.started && this.start is not null && !this.start.Test(line))
            {
                continue;
            }

            if (!this.stopped && this.stop is not null && this.stop.Test(line))
            {
                this.stopped = true;
                if (!this.includeStopLine)
                {
                    break;
                }
            }

            if (!this.started)
            {
                this.started = true;
                if (!this.includeStartLine)
                {
                    // don't include the line that starts the section.
                    continue;
                }
            }

            return line;
        }

        return null;
    }
}