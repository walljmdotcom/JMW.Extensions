using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JMW.Template.Tags
{
    public static class Include
    {
        public static string TAG { get; } = "include";
        public const string ATTR_PATH = "path";
        public const string ATTR_NAME = "name";
        public const string ATTR_DEFINE = "define";

        private const int maxDepth = 32;

        public static HashSet<string> ALLOWEDPROPS { get; } = [ATTR_PATH, ATTR_NAME, ATTR_DEFINE];

        /// <summary>
        /// Replaces every include tag in <paramref name="tags"/> with the tags it refers to, expanding includes
        /// inside included content too. Paths are resolved by <see cref="Interpreter.FileRoot"/>.
        /// </summary>
        public static void ReplaceIncludes(List<Tag> tags, Dictionary<string, Tag> includes, Interpreter interp)
        {
            ReplaceIncludes(tags, includes, interp, []);
        }

        // chain holds the includes being expanded right now, outermost first, so a cycle can be named.
        private static void ReplaceIncludes(List<Tag> tags, Dictionary<string, Tag> includes, Interpreter interp, List<string> chain)
        {
            for (var i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];

                // ignore the include definitions.
                if (tag.Properties.ContainsKey(ATTR_DEFINE) && tag.Name.ToLowerInvariant() == TAG)
                    continue;

                if (tag.TagType == TagTypes.Tag && string.Equals(tag.Name.ToLowerInvariant(), TAG, StringComparison.Ordinal))
                {
                    List<Tag> replacement;
                    string key;

                    if (tag.Properties.TryGetValue(ATTR_PATH, out var path))
                    {
                        var file_name = interp.ResolveTemplatePath(path, TAG);
                        key = "path:" + file_name;
                        Enter(chain, key);
                        replacement = interp.Parser.Parse(ReadTemplate(file_name));
                    }
                    else if (tag.Properties.TryGetValue(ATTR_NAME, out var name))
                    {
                        if (!includes.TryGetValue(name, out var definition))
                            throw new Exception("The <" + TAG + "> tag referenced named " + TAG + " '" + name + "' which does not exist in the template.");

                        key = "name:" + name;
                        Enter(chain, key);
                        replacement = new List<Tag>(definition.Children);
                    }
                    else
                        throw new Exception("The <" + TAG + "> tag requires either a '" + ATTR_NAME + "' attribute or a '" + ATTR_PATH + "' attribute.");

                    ReplaceIncludes(replacement, includes, interp, chain);
                    chain.RemoveAt(chain.Count - 1);

                    tags.RemoveAt(i);
                    tags.InsertRange(i, replacement);

                    // the replacement is fully expanded, so continue after it.
                    i += replacement.Count - 1;
                }
                else if (tag.TagType == TagTypes.Tag && tag.HasChildren)
                {
                    ReplaceIncludes(tag.Children, includes, interp, chain);
                }
            }
        }

        private static void Enter(List<string> chain, string key)
        {
            if (chain.Contains(key))
            {
                throw new Exception("The <" + TAG + "> tags include each other in a loop: " + string.Join(" -> ", chain.Concat([key])) + ".");
            }

            if (chain.Count >= maxDepth)
            {
                throw new Exception("The <" + TAG + "> tags are nested more than " + maxDepth + " levels deep.");
            }

            chain.Add(key);
        }

        private static string ReadTemplate(string file_name)
        {
            try
            {
                return File.ReadAllText(file_name);
            }
            catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
            {
                throw new Exception("The <" + TAG + "> tag refers to a template which does not exist: '" + file_name + "'.", e);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                throw new Exception("The <" + TAG + "> tag could not read the template '" + file_name + "': " + e.Message, e);
            }
        }
    }
}
