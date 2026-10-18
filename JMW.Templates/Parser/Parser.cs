using JMW.Extensions;
using JMW.Templates.Tags;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JMW.Templates
{
    /// <summary>
    ///   Parses template tags for a simple generic templating engine.
    /// </summary>
    public class Parser
    {
        private readonly Dictionary<string, ITagHandler> _tags = [];
        private readonly HashSet<string> _columnTags = [];

        // Tag names are lowercased when parsed, so the names they are checked against are lowercased here.
        public void AddColumnTag(string column)
        {
            _columnTags.Add(column.ToLowerInvariant());
        }

        // A join names a column of either table with a left_ or right_ prefix.
        private bool IsColumnTag(string name)
        {
            return _columnTags.Contains(name)
                || (name.StartsWith(JoinColumn.LEFT_PREFIX, System.StringComparison.Ordinal) && _columnTags.Contains(name.Substring(JoinColumn.LEFT_PREFIX.Length)))
                || (name.StartsWith(JoinColumn.RIGHT_PREFIX, System.StringComparison.Ordinal) && _columnTags.Contains(name.Substring(JoinColumn.RIGHT_PREFIX.Length)));
        }

        public void AddTag(string tag_name, ITagHandler tag)
        {
            _tags.Add(tag_name.ToLowerInvariant(), tag);
        }

        public void AddTag(ITagHandler tag)
        {
            AddTag(tag.TagName, tag);
        }

        public Parser()
        {
            // add the default tags
            AddTag(new Conditional());
            AddTag(new Transform());
            AddTag(new Output());
            AddTag(new Table());
            AddTag(new Variable());
            AddTag(new Version());
            AddTag(new Timestamp());
            AddTag(new Lookup());
            AddTag(new Join());
            AddTag(new LookupRow());
        }
        
        /// <summary>
        /// Parses a template into a list of Tags to process.
        /// </summary>
        /// <param name="block">The input template</param>
        /// <returns>List of parsed Tag objects</returns>
        public List<Tag> Parse(string block)
        {
            return parse(new StringReader(handleNewLines(block)));
        }

        /// <summary>
        /// Parses a template read from <paramref name="reader"/>, with the same line-break rules as <see cref="Parse(string)"/>.
        /// </summary>
        public List<Tag> Parse(TextReader reader)
        {
            return Parse(reader.ReadToEnd());
        }

        private List<Tag> parse(TextReader reader)
        {
            var tokenizer = new Tokenizer(reader);
            var stack = new Stack<Tag>();
            var ast = new List<Tag>();
            var currParam = string.Empty;
            while (tokenizer.Next() != TokenType.ERROR)
            {
                var tag = new Tag();
                var token = tokenizer.Token;
                switch (token.Type)
                {
                    case TokenType.OPENTAG:
                        tag.TagType = TagTypes.Tag;
                        tag.Name = token.Value.ToLowerInvariant().Trim();

                        if (stack.Count > 0)
                        {
                            stack.Peek().Children.Add(tag);
                        }
                        stack.Push(tag);
                        break;

                    case TokenType.SINGLEClOSE:
                    case TokenType.ENDTAG:
                        if (stack.Count == 0)
                        {
                            throw new ParseException(
                                token.Type == TokenType.ENDTAG
                                    ? "End tag </" + token.Value + "> has no matching open tag."
                                    : "/> has no matching open tag.",
                                token
                            );
                        }
                        var curr = stack.Pop();

                        if (curr.Name != token.Value.ToLowerInvariant().Trim() && token.Type != TokenType.SINGLEClOSE)
                        {
                            throw new ParseException("Invalid end tag.", token);
                        }

                        if (_tags.ContainsKey(curr.Name))
                        {
                            _tags[curr.Name].Validate(curr, token);
                        }

                        // handle the special case of prefixed vars.
                        else if (curr.Name.StartsWith(Variable.TAG + ":", System.StringComparison.Ordinal))
                        {
                            new Variable().Validate(curr, token);
                        }
                        // handle the special case of prefixed vars.
                        else if (curr.Name == Include.TAG)
                        {
                            TagHelpers.CheckAllowedAttributes(curr, Include.ALLOWEDPROPS, token);
                        }
                        // handle the special case for columns, and join columns
                        else if (IsColumnTag(curr.Name))
                        {
                            TagHelpers.CheckAllowedAttributes(curr, ColumnBase.ALLOWED, token);
                        }

                        if (stack.Count == 0)
                        {
                            ast.Add(curr);
                        }
                        break;

                    case TokenType.PARAMNAME:
                        if (stack.Count == 0)
                        {
                            throw new ParseException("Parameter '" + token.Value + "' is outside any open tag.", token);
                        }
                        if (currParam.IsNotEmpty())
                        {
                            stack.Peek().Properties[currParam] = string.Empty;
                        }
                        currParam = token.Value.ToLowerInvariant();
                        break;

                    case TokenType.PARAMVALUE:
                        if (stack.Count == 0)
                        {
                            throw new ParseException("Parameter value '" + token.Value + "' is outside any open tag.", token);
                        }
                        if (currParam.IsNotEmpty())
                        {
                            stack.Peek().Properties[currParam] = removeSourceLineBreaks(token.Value);
                        }
                        // reset currParam since we found a value for it.
                        currParam = string.Empty;
                        break;

                    case TokenType.TEXT:
                        var text = removeSourceLineBreaks(token.Value);
                        if (text.Length == 0)
                        {
                            break;
                        }
                        tag.TagType = TagTypes.Text;
                        tag.TokenText = text;
                        if (stack.Count > 0)
                        {
                            stack.Peek().Children.Add(tag);
                        }
                        else
                        {
                            ast.Add(tag);
                        }
                        break;
                }
            }

            if (tokenizer.Token.Type == TokenType.ERROR && tokenizer.Token.Value != "EOF")
            {
                throw new ParseException(tokenizer.Token);
            }
            return stack.Count != 0
                ? throw new ParseException("Template ended prematurely. Did you forget a </" +
                    stack.Peek().Name + ">?", tokenizer.Token)
                : ast;
        }

        // Source line breaks become a bare LF, so they still end comments and separate tag attributes,
        // and a \n escape becomes CRLF. After this every CR comes from an escape.
        private static string handleNewLines(string current_template)
        {
            current_template = current_template.Replace("\r\n", "\n");
            current_template = current_template.Replace("\r", "\n");
            current_template = current_template.Replace("\\n", "\r\n");
            return current_template;
        }

        // Drops each LF that came from the template source, keeping the CRLF pairs that \n escapes produced.
        private static string removeSourceLineBreaks(string value)
        {
            if (value.IndexOf('\n') < 0)
            {
                return value;
            }

            var sb = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '\n' && (i == 0 || value[i - 1] != '\r'))
                {
                    continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}