namespace JMW.Template.Tags;

public interface ITagHandler
{
    void Handler(Tag token, Interpreter interp);

    void Validate(Tag tag, Token token);

    string TagName { get; }
}