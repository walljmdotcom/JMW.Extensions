/*
The MIT License (MIT)
Copyright (c) 2016 Jason Wall

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JMW.Extensions.Enumerable
{
    public static class Extensions
    {
        public static IEnumerable<T> ReverseOrder<T>(this IList<T> lst)
        {
            for (var i = lst.Count - 1; i >= 0; i--)
            {
                yield return lst[i];
            }
        }

        /// <summary>
        /// Indicates if the given index is the last item in the list.
        /// </summary>
        /// <param name="source">List to check</param>
        /// <param name="i">Index to check</param>
        /// <returns></returns>
        public static bool IsLast<T>(this IEnumerable<T> source, int i)
        {
            return source switch
            {
                T[] source1 => source1.Length - 1 == i,
                ICollection<T> collection => collection.Count - 1 == i,
                _ => source.ToArray().Length - 1 == i,
            };
        }

        /// <summary>
        ///   Return the last items in the list in the specified quantity
        /// </summary>
        /// <typeparam name="T">Type of items in the list</typeparam>
        /// <param name="source">The list to return items from</param>
        /// <param name="qty">The number of items to return</param>
        public static IEnumerable<T> Last<T>(this IEnumerable<T> source, int qty = 1)
        {
            var enumerable = source as T[] ?? source.ToArray();
            var len = enumerable.Length;

            // if the quatity requested is greater than the number of items in the list, just return the list
            if (qty > len)
            {
                foreach (var item in enumerable) yield return item;
            }

            // if the quatity requested is less than 1, return and empty list
            else if (qty < 1)
            {
            }
            else
            {
                var start = len - qty;
                var i = 0;
                foreach (var item in enumerable)
                {
                    if (i++ < start) continue;
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Converts an IEnumerable{T} to a HashSet{T}, excluding duplicate entries.
        /// </summary>
        /// <typeparam name="T">Type of objects to store in the HashSet</typeparam>
        /// <param name="lst">The list of objects to convert</param>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> lst)
        {
            var hash = new HashSet<T>();
            foreach (var i in lst)
            {
                hash.Add(i);
            }
            return hash;
        }

        /// <summary>
        /// Merges the second Set into the first and returns the first set with the merged items in it.
        /// </summary>
        /// <typeparam name="T">Type of items stored in the sets</typeparam>
        /// <param name="hsh1">The ISet to merge the items into</param>
        /// <param name="hsh2">The ISet being merged in</param>
        public static ISet<T> Merge<T>(this ISet<T> hsh1, ISet<T> hsh2)
        {
            foreach (var i in hsh2)
            {
                hsh1.Add(i);
            }

            return hsh1;
        }

        /// <summary>
        /// Merges the list into the set and returns the set with the merged items in it.
        /// </summary>
        /// <typeparam name="T">Type of items stored in the sets</typeparam>
        /// <param name="hsh">The ISet to merge the items into</param>
        /// <param name="enumerable">The IEnumerable being merged in</param>
        public static ISet<T> Merge<T>(this ISet<T> hsh, IEnumerable<T> enumerable)
        {
            foreach (var i in enumerable)
            {
                hsh.Merge(i);
            }

            return hsh;
        }

        /// <summary>
        /// Merges the item into the first set and returns the first set with the merged item in it.
        /// </summary>
        /// <typeparam name="T">Type of items stored in the sets</typeparam>
        /// <param name="hsh">The ISet to merge the items into</param>
        /// <param name="item">The item being merged in</param>
        public static ISet<T> Merge<T>(this ISet<T> hsh, T item)
        {
            hsh.TryAdd(item);

            return hsh;
        }

        /// <summary>
        /// Trys to add a value to an ISet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hsh"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool TryAdd<T>(this ISet<T> hsh, T item)
        {
            if (hsh.Contains(item)) return false;
            hsh.Add(item);
            return true;
        }

        /// <summary>
        /// Safely adds values to a dictionary of lists.  This function handles creating the list if the key is new.
        /// </summary>
        /// <typeparam name="K">Type of the Key</typeparam>
        /// <typeparam name="V">Type of the List value</typeparam>
        /// <param name="dict">Dictionary to add pairs to</param>
        /// <param name="key">The key</param>
        /// <param name="val">The value to add to the list</param>
        public static void SafeAdd<K, V>(this IDictionary<K, List<V>> dict, K? key, V? val)
        {
            if (key is null)
            {
                return;
            }
            if (val is null)
            {
                return;
            }

            if (dict.TryGetValue(key, out List<V>? value))
            {
                value.Add(val);
            }
            else
            {
                dict.Add(key, [val]);
            }
        }

        /// <summary>
        /// Adds a key and value if not present in the dict.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool AddIfNotPresent<K, V>(this IDictionary<K, V> dict, K? key, V? val)
        {
            if (key is null || val is null)
            {
                return false;
            }

            if (dict.ContainsKey(key)) return false;
            dict.Add(key, val);
            return true;
        }

        /// <summary>
        /// Converts a list of strings into a dilimited string.
        /// </summary>
        /// <param name="Ss">List of strings to concatenate</param>
        /// <param name="delimiter">char to insert between items</param>
        /// <param name="trimend">whether to leave a trailing dilimeter</param>
        public static string ToDelimitedString(this IEnumerable Ss, char delimiter, bool trimend = true)
        {
            return Ss.ToDelimitedString(delimiter.ToString(), trimend);
        }

        /// <summary>
        /// Converts a list of strings into a dilimited string.
        /// </summary>
        /// <param name="Ss">List of strings to concatenate</param>
        /// <param name="delimiter">text to insert between items</param>
        /// <param name="trimend">whether to leave a trailing dilimeter</param>
        /// <returns></returns>
        public static string ToDelimitedString(this IEnumerable Ss, string delimiter, bool trimend = true)
        {
            var o = new StringBuilder();
            var first = true;
            foreach (var s in Ss)
            {
                if (!first) o.Append(delimiter);
                o.Append(s);
                first = false;
            }

            if (!trimend && !first)
                o.Append(delimiter);

            return o.ToString();
        }

        /// <summary>
        /// Converts an IEnumerable into an IEnumerable{T}
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static IEnumerable<T> Shim<T>(this IEnumerable enumerable)
        {
            return from object current in enumerable select (T)current;
        }

        /// <summary>
        /// Maps a function across every item in an enumerable list, then returns the items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lst"></param>
        /// <param name="act"></param>
        /// <returns></returns>
        public static void Each<T>(this IEnumerable<T> lst, Action<T> act)
        {
            foreach (var item in lst)
                act(item);
        }

        private static class ThreadSafeRandom
        {
            [ThreadStatic]
            private static Random? Local;

            public static Random ThisThreadsRandom => Local ??= new Random(unchecked(Environment.TickCount * 31 + Environment.CurrentManagedThreadId));
        }

        /// <summary>
        /// Randomly shuffles a list of items.
        /// </summary>
        /// <typeparam name="T">Type of item in the list</typeparam>
        /// <param name="list">List to be shuffled</param>
        /// <returns></returns>
        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            //http://stackoverflow.com/questions/273313/randomize-a-listt-in-c-sharp
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }

            return list;
        }

        /// <summary>
        /// Returns a list of the duplicate items (without duplicates beign removed).
        /// </summary>
        /// <param name="lst">List to enumerate</param>
        /// <returns></returns>
        public static IEnumerable<T> GetDuplicates<T>(this IEnumerable<T> lst)
        {
            var uniques = new HashSet<T>();

            foreach (var s in lst)
            {
                if (!uniques.Add(s))
                    yield return s;
            }
        }

        /// <summary>
        /// Safely gets a range of values from a list.  If the start index begins after the end of the list, no values are returned.  If the
        ///   length of the request exceeds the list, the values are returned up to the end of the list.
        /// </summary>
        /// <typeparam name="T">Type of objects in enumeration</typeparam>
        /// <param name="lst">List to be enumerated</param>
        /// <param name="start">Starting index</param>
        /// <param name="length">Number of items to return</param>
        /// <returns></returns>
        public static List<T> GetRangeSafe<T>(this List<T> lst, int start, int length)
        {
            if (lst.Count == 0 || start > lst.Count) return [];
            return lst.Count < start + length ? lst.GetRange(start, lst.Count - start) : lst.GetRange(start, length);
        }

        /// <summary> This function allows you to create a histogram on any property of an <see cref="IEnumerable{T}"/> list. </summary>
        /// <typeparam name="T" />The type of object being examined in the <see cref="IEnumerable{T}"/>
        /// <param name="items">The list of items being evaluated for the histogram</param>
        /// <param name="get_key">a function that returns the value being tested in the histogram</param>
        /// <returns>a histogram of the values returned by the get function</returns>
        public static Dictionary<string, List<T>> CreateHistogram<T>(this IEnumerable<T> items, Func<T, string> get_key)
        {
            var groups = new Dictionary<string, List<T>>();
            foreach (var dvc in items)
            {
                if (!groups.ContainsKey(get_key(dvc)))
                {
                    groups.Add(get_key(dvc), [dvc]);
                }
                else
                {
                    groups[get_key(dvc)].Add(dvc);
                }
            }

            return groups;
        }

        /// <summary> This function allows you to create a histogram on any property of an <see cref="IEnumerable{V}"/> list. </summary>
        /// <typeparam name="V">The type of object being examined in the <see cref="IEnumerable{V}"/> </typeparam>
        /// <typeparam name="K">Type of the Key</typeparam>
        /// <param name="items">The list of items being evaluated for the histogram</param>
        /// <param name="key">a function that returns the value being tested in the histogram</param>
        /// <returns>a histogram of the values returned by the get function</returns>
        public static Dictionary<K, List<V>> CreateHistogram<K, V>(this IEnumerable<V> items, Func<V, K> key)
            where K : notnull
        {
            var groups = new Dictionary<K, List<V>>();
            foreach (var dvc in items)
            {
                if (!groups.ContainsKey(key(dvc)))
                {
                    groups.Add(key(dvc), [dvc]);
                }
                else
                {
                    groups[key(dvc)].Add(dvc);
                }
            }

            return groups;
        }

        /// <summary>
        /// Wraps an object instance into an IEnumerable{T}; consisting of a single item.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="item">The instance that will be wrapped.</param>
        /// <returns>An IEnumerable{T}; consisting of a single item.</returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        /// <summary>
        /// Wraps an object instance into an IEnumerable&lt;T&gt; consisting of a single item.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="item">The instance that will be wrapped.</param>
        /// <returns>An IEnumerable&lt;T&gt; consisting of a single item.</returns>
        public static List<T> ToListOfItem<T>(this T item)
        {
            return [item];
        }

        /// <summary>
        /// Returns the distinct items using a selector to indicate the distinct key
        /// </summary>
        /// <typeparam name="T">Type of object in list</typeparam>
        /// <param name="lst">List to enumerate</param>
        /// <param name="get_key">Function that returns a string to be used as distinct value</param>
        /// <returns></returns>
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> lst, Func<T, object> get_key)
        {
            var hsh = new HashSet<object>();
            foreach (var item in lst)
            {
                if (hsh.TryAdd(get_key(item)))
                    yield return item;
            }
        }

        public static bool TryAdd<K, V>(this IDictionary<K, V> dict, K key, V val)
        {
            if (dict.ContainsKey(key))
                return false;

            dict.Add(key, val);
            return true;
        }

        /// <summary>
        /// Adds the item if the key is not present, or creates a new entry if it exists.
        /// </summary>
        /// <typeparam name="K">Key Type</typeparam>
        /// <typeparam name="V">Value Type</typeparam>
        /// <param name="dict">Dictionary to add items to</param>
        /// <param name="key">Key Item</param>
        /// <param name="val">Value Item</param>
        public static void AddOrCreate<K, V>(this IDictionary<K, List<V>> dict, K key, V val)
        {
            if (dict.TryGetValue(key, out List<V>? value))
                value.Add(val);
            else
                dict.Add(key, [val]);
        }

        /// <summary>
        /// Adds the item if the key is not present, or creates a new entry if it exists.
        /// </summary>
        /// <typeparam name="K">Key Type</typeparam>
        /// <typeparam name="V">Value Type</typeparam>
        /// <param name="dict">Dictionary to add items to</param>
        /// <param name="key">Key Item</param>
        /// <param name="val">Value Item</param>
        public static void AddOrCreate<K, V>(this IDictionary<K, HashSet<V>> dict, K key, V val)
        {
            if (dict.TryGetValue(key, out HashSet<V>? value))
                value.Add(val);
            else
                dict.Add(key, [val]);
        }

        /// <summary>
        /// Creates a <see>
        ///         <cref>Dictionary{K, List{V}}</cref>
        ///     </see>
        /// </summary>
        /// <typeparam name="K">Type of the key</typeparam>
        /// <typeparam name="V">Type of the value</typeparam>
        /// <param name="lst">List of items to index</param>
        /// <param name="keySelector">function that takes an item of type <typeparamref name="V"/> and returns a key of type <see cref="K"/>.</param>
        /// <returns>a <see cref="Dictionary{K, V}"/> of the items indexed by the keys returned by the <paramref name="keySelector"/></returns>
        public static Dictionary<K, List<V>> ToDictionaryOfLists<K, V>(this IEnumerable<V> lst, Func<V, K> keySelector)
            where K : notnull
        {
            var ret = new Dictionary<K, List<V>>();
            foreach (var item in lst)
                ret.AddOrCreate(keySelector(item), item);

            return ret;
        }

        /// <summary>
        /// Creates a <see>
        ///         <cref>Dictionary{K, List{V}}</cref>
        ///     </see>
        ///     using multiple keys from each item.  This is patterned off of the .SelectMany function in Linq.
        /// </summary>
        /// <typeparam name="K">Type of the key</typeparam>
        /// <typeparam name="V">Type of the value</typeparam>
        /// <param name="lst">List of items to index</param>
        /// <param name="keySelector">function that takes an item of type <typeparamref name="V"/> and returns an <see cref="IEnumerable{K}"/> of keys.</param>
        /// <returns>a <see cref="Dictionary{K, V}"/> of the items indexed by the keys returned by the <paramref name="keySelector"/></returns>
        public static Dictionary<K, List<V>> ToDictionaryOfListsOfMany<K, V>(this IEnumerable<V> lst, Func<V, IEnumerable<K>> keySelector)
            where K : notnull
        {
            var ret = new Dictionary<K, List<V>>();
            foreach (var item in lst)
            {
                foreach (var key in keySelector(item))
                    ret.AddOrCreate(key, item);
            }

            return ret;
        }

        /// <summary>
        /// Creates a <see>
        ///         <cref>Dictionary{K, HashSet{V}}</cref>
        ///     </see>
        ///     using multiple keys from each item.  This is patterned off of the .SelectMany function in Linq.
        /// </summary>
        /// <typeparam name="K">Type of the key</typeparam>
        /// <typeparam name="V">Type of the value</typeparam>
        /// <param name="lst">List of items to index</param>
        /// <param name="keySelector">function that takes an item of type <typeparamref name="V"/> and returns an <see cref="IEnumerable{K}"/> of keys.</param>
        /// <returns>a <see cref="Dictionary{K, V}"/> of the items indexed by the keys returned by the <paramref name="keySelector"/></returns>
        public static Dictionary<K, HashSet<V>> ToDictionaryOfHashSetsOfMany<K, V>(this IEnumerable<V> lst, Func<V, IEnumerable<K>> keySelector)
            where K : notnull
        {
            var ret = new Dictionary<K, HashSet<V>>();
            foreach (var item in lst)
            {
                foreach (var key in keySelector(item))
                    ret.AddOrCreate(key, item);
            }
            return ret;
        }

        /// <summary>
        /// Creates a <see><cref>Dictionary{K, HashSet{V}}</cref></see> that turns a list into a dictionary where multiple items with the same key are stored in a HashSet as the value of the dict.
        /// </summary>
        /// <typeparam name="K">Type of the key</typeparam>
        /// <typeparam name="V">Type of the value</typeparam>
        /// <param name="lst">List of items to index</param>
        /// <param name="keySelector">function that takes an item of type <typeparamref name="V"/> and returns a key of type <see cref="K"/>.</param>
        /// <returns>a <see cref="Dictionary{K, V}"/> of the items indexed by the keys returned by the <paramref name="keySelector"/></returns>
        public static Dictionary<K, HashSet<V>> ToDictionaryOfHashSets<K, V>(this IEnumerable<V> lst, Func<V, K> keySelector)
            where K : notnull
        {
            var ret = new Dictionary<K, HashSet<V>>();
            foreach (var item in lst)
                ret.AddOrCreate(keySelector(item), item);

            return ret;
        }

        /// <summary>
        /// Creates a <see><cref>Dictionary{K, V}</cref></see> of types that is indexed on multiple values of that type.
        /// The keys must all be unique.
        /// </summary>
        /// <typeparam name="K">Type of the key</typeparam>
        /// <typeparam name="V">Type of the value</typeparam>
        /// <param name="lst">List of items to index</param>
        /// <param name="get_keys">function that takes an item of type <typeparamref name="V"/> and returns an <see cref="IEnumerable{K}"/> of keys to be indexed by.</param>
        /// <returns>a <see cref="Dictionary{K, V}"/> of the items indexed by the keys returned by the <paramref name="get_keys"/></returns>
        public static Dictionary<K, V> ToDictionaryOfMany<K, V>(this IEnumerable<V> lst, Func<V, IEnumerable<K>> get_keys)
            where K : notnull
        {
            var dict = new Dictionary<K, V>();
            foreach (var item in lst)
            {
                foreach (var key in get_keys(item))
                    dict.TryAdd(key, item);
            }
            return dict;
        }

        /// <summary>
        /// Checks to see if a key is present in the Dictionary and runs an action on the value if true.
        /// </summary>
        /// <typeparam name="K">Type of the key</typeparam>
        /// <typeparam name="V">Type of the value</typeparam>
        /// <param name="dict">Dictionary to check for the key</param>
        /// <param name="key">Key to check for</param>
        /// <param name="act">Action to run if the key is present.</param>
        public static void IfHasKey<K, V>(this Dictionary<K, V> dict, K key, Action<V> act)
            where K : notnull
        {
            if (dict.TryGetValue(key, out V? value))
                act(value);
        }

        /// <summary>
        /// Groups a list of sequential items into sets using a function to determine where a set begins.
        /// </summary>
        /// <param name="items">List of lines to group</param>
        /// <param name="is_beginning">This function is evaluated against each item in the <paramref name="items"/> and returns true if the item is the beginning of a new set.</param>
        /// <typeparam name="T">Type of item to be grouped.</typeparam>
        public static IEnumerable<List<T>> GroupIntoSets<T>(this IEnumerable<T> items, Predicate<T> is_beginning)
        {
            var set = new List<T>();
            var first = true;

            foreach (var item in items)
            {
                if (is_beginning(item))
                {
                    // don't return the first set, it will always be junk.
                    if (!first) yield return set;

                    // you're past the first junk set, reset.
                    first = false;
                    set = [];
                }

                set.Add(item);
            }

            yield return set;
        }
    }
}