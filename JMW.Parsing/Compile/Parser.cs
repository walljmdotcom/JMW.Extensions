using System.Collections.Generic;
using System.IO;

namespace JMW.Parsing.Compile;

/// <summary>
///   Parses template tags for a simple generic templating engine.
/// </summary>
public class Parser
{
    public Parser()
    {
    }

    /// <summary>
    /// Parses a template into a list of Tags to process.
    /// </summary>
    /// <param name="block">The input template</param>
    /// <returns>List of parsed Tag objects</returns>
    public List<Tag> Parse(string block)
    {
        return this.Parse(new StringReader(block));
    }

    public List<Tag> Parse(TextReader reader)
    {
        var tokenizer = new Tokenizer(reader);
        var stack = new Stack<Tag>();
        var ast = new List<Tag>();
        Tag? last = null;
        while (tokenizer.Next() != TokenType.Error)
        {
            var tag = new Tag();
            var token = tokenizer.Token;
            switch (token.Type)
            {
                case TokenType.ObjectStart:
                    tag.TagType = TagTypes.Object;
                    tag.Name = token.Value?.ToLowerInvariant().Trim() ?? string.Empty;
                    stack.Push(tag);
                    break;

                case TokenType.ArrayStart:
                    tag.TagType = TagTypes.Array;
                    tag.Name = token.Value?.ToLowerInvariant().Trim() ?? string.Empty;
                    tag.Value = new Stack<Tag>();
                    stack.Push(tag);

                    break;

                case TokenType.ArrayStop:
                    {
                        if (stack.Count == 0)
                        {
                            throw new ParseException("Unexpected ] with no open [.", token);
                        }
                        var curr = stack.Pop();
                        last = curr;
                        if (stack.Count > 0 && stack.Peek().TagType == TagTypes.Property)
                        {
                            var t = stack.Pop();
                            if (stack.Count == 0)
                            {
                                throw new ParseException("Property '" + t.Name + "' is not inside an object.", token);
                            }
                            stack.Peek().Properties[t.Name] = curr;
                            break;
                        }
                        if (stack.Count == 0)
                        {
                            throw new ParseException("Array '" + curr.Name + "' is not inside an object.", token);
                        }
                        stack.Peek().Properties[curr.Name] = curr;
                        break;
                    }
                case TokenType.ObjectStop:
                    {
                        if (stack.Count == 0)
                        {
                            throw new ParseException("Unexpected } with no open {.", token);
                        }
                        var curr = stack.Pop();
                        last = curr;
                        if (stack.Count > 0 && stack.Peek().TagType == TagTypes.Array)
                        {
                            var o = (Stack<Tag>)stack.Peek().Value;
                            o.Push(curr);
                            break;
                        }
                        if (stack.Count > 0 && stack.Peek().TagType == TagTypes.Property)
                        {
                            var t = stack.Pop();
                            if (stack.Count == 0)
                            {
                                throw new ParseException("Property '" + t.Name + "' is not inside an object.", token);
                            }
                            stack.Peek().Properties[t.Name] = curr;
                            break;
                        }

                        if (stack.Count > 0 && stack.Peek().TagType == TagTypes.Object)
                        {
                            // its an object like an Or or and And that has a list of expressions.
                            var o = stack.Peek();
                            if (o.Value.GetType() != typeof(Stack<Tag>))
                            {
                                o.Value = new Stack<Tag>();
                            }

                            ((Stack<Tag>)o.Value).Push(curr);
                            break;
                        }

                        if (stack.Count == 0)
                        {
                            ast.Add(curr);
                        }
                        break;
                    }
                case TokenType.PropertyName:
                    tag.Name = token.Value?.ToLowerInvariant().Trim() ?? string.Empty;
                    tag.TagType = TagTypes.Property;
                    stack.Push(tag);

                    break;

                case TokenType.Options:
                    tag.Name = "m";
                    tag.Value = token.Value ?? string.Empty;
                    tag.TagType = TagTypes.Property;
                    last?.Properties.Add(tag.Name, tag);
                    break;

                case TokenType.Value:
                    if (stack.Count > 0 && stack.Peek().TagType == TagTypes.Property)
                    {
                        var o = stack;
                        if (stack.Count > 0 && stack.Peek().TagType == TagTypes.Array)
                            o = (Stack<Tag>)stack.Peek().Value;

                        var t = o.Pop();
                        t.Value = token.Value ?? string.Empty;
                        t.TagType = TagTypes.Property;
                        if (stack.Count == 0)
                        {
                            throw new ParseException("Property '" + t.Name + "' is not inside an object.", token);
                        }
                        stack.Peek().Properties[t.Name] = t;
                    }
                    if (stack.Count > 0 && stack.Peek().TagType == TagTypes.Array)
                    {
                        var o = (Stack<Tag>)stack.Peek().Value;
                        tag.TagType = TagTypes.Word;
                        tag.Name = string.Empty;
                        tag.Value = token.Value ?? string.Empty;
                        o.Push(tag);
                    }
                    break;
            }
        }

        if (tokenizer.Token.Type == TokenType.Error && tokenizer.Token.Value != "EOF")
        {
            throw new ParseException(tokenizer.Token);
        }

        return stack.Count != 0
            ? throw new ParseException("Parsing code ended prematurely. Did you forget a closing }?", tokenizer.Token)
            : ast;
    }
}