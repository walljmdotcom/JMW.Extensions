using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JMW.Networking.Parsers.JunosConfig;

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
        return Parse(new StringReader(block));
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
                    var o_names = CollapseWords(stack);
                    if (o_names.Count > 0)
                    {
                        tag.Name = o_names.First().Value.Trim();
                        tag.Value = (string.Join(" ", o_names.Skip(1).Select(o => o.Value).ToArray()) + " " + token.Value.Trim()).Trim();
                    }
                    else
                        tag.Name = token.Value.Trim();

                    tag.TagType = TagTypes.Object;
                    tag.Children = new Stack<Tag>();
                    stack.Push(tag);
                    break;

                case TokenType.ArrayStart:
                    if (stack.Count > 0)
                        stack.Pop(); // you don't need the current word.
                    tag.TagType = TagTypes.Array;
                    tag.Name = token.Value.ToLowerInvariant().Trim();
                    tag.Children = new Stack<Tag>();
                    stack.Push(tag);
                    break;

                case TokenType.ArrayStop:
                    {
                        if (stack.Count == 0)
                        {
                            throw new ParseException("Unexpected ] with no open [.", token);
                        }
                        var curr = stack.Pop();
                        if (stack.Count == 0)
                        {
                            ast.Add(curr);
                        }
                        else
                        {
                            stack.Peek().Children.Push(curr);
                        }
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
                            stack.Peek().Children.Push(curr);
                            break;
                        }
                        if (stack.Count > 0 && stack.Peek().TagType == TagTypes.Object)
                        {
                            stack.Peek().Children.Push(curr);
                            break;
                        }

                        if (stack.Count == 0)
                        {
                            ast.Add(curr);
                        }
                        break;
                    }
                case TokenType.LineStop:
                    {
                        if (stack.Count > 0 && stack.Peek().TagType == TagTypes.Array)
                        {
                            tag.TagType = TagTypes.Word;
                            tag.Name = string.Empty;
                            tag.Value = token.Value;
                            stack.Peek().Children.Push(tag);
                        }
                        else if (stack.Count > 0 && stack.Peek().TagType == TagTypes.Object)
                        {
                            if (token.Value.Length == 0)
                                break;

                            tag.TagType = TagTypes.Word;
                            tag.Name = string.Empty;
                            tag.Value = token.Value;
                            stack.Peek().Children.Push(tag);
                        }
                        else if (stack.Count > 0)
                        {
                            var words = CollapseWords(stack);
                            tag.TagType = TagTypes.Property;

                            if (words.Count > 2)
                            {
                                tag.Name = words.Last().Value;

                                tag.Value = string.Join(" ", words.Take(words.Count - 1).Select(o => o.Value).ToArray());
                            }
                            else if (words.Count == 2)
                            {
                                tag.Name = words[1].Value;
                                tag.Value = words[0].Value;
                            }
                            else if (words.Count == 1)
                            {
                                tag.Name = words[0].Value;
                            }

                            if (stack.Count > 0)
                            {
                                stack.Peek().Children.Push(tag);
                            }
                            else
                                ast.Add(tag);
                        }
                        break;
                    }
                case TokenType.Word:
                    {
                        tag.TagType = TagTypes.Word;
                        tag.Value = token.Value;
                        if (stack.Count > 0 && stack.Peek().TagType == TagTypes.Array)
                        {
                            stack.Peek().Children.Push(tag);
                        }
                        else
                            stack.Push(tag);
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

    private static List<Tag> CollapseWords(Stack<Tag> stack)
    {
        var words = new List<Tag>();
        while (stack.Count > 0 && stack.Peek().TagType == TagTypes.Word)
            words.Add(stack.Pop());
        return words;
    }
}