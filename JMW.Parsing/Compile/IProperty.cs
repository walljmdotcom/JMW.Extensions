using System.Collections.Generic;
using System.IO;

namespace JMW.Parsing.Compile;

public interface IProperty
{
    string Name { get; }

    object Parse(TextReader reader);
}