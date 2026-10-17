using System;

namespace JMW.Templates;

[Serializable]
public class ParseException : Exception
{
    public string Location { get; } = string.Empty;
    public string ErrorText { get; }

    public ParseException(string message)
        : base(message)
    {
        ErrorText = message;
    }

    public ParseException(string message, Token token)
        : base(message + " " + token.LocationToString())
    {
        Location = token.LocationToString();
        ErrorText = message;
    }

    public ParseException(Token token)
        : base(token.ToString())
    {
        Location = token.LocationToString();
        ErrorText = token.Type + ": " + token.Value;
    }
}