/*
The MIT License (MIT)
Copyright (c) 2016 Jason Wall

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using JMW.Extensions;
using JMW.Functional;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace JMW.Collections
{
    /// <summary>
    /// A collection that allows you to index more than one property.  The indices are maintained, so when a value
    /// is modified, the indices are updated.
    /// </summary>
    /// <typeparam name="T">The type of object being stored in the collection.</typeparam>
    public class IndexedCollection<T> : INotifyCollectionChanged, IList<T>, IIndexObserver where T : IndexedClass
    {
        private const string IndexViolationMessage =
            "Operation was cancelled due to an index violation. The Index may have been violated.";

        private readonly Dictionary<string, Dictionary<string, List<T>>> _indexCollection = [];
        private readonly Dictionary<string, Dictionary<string, T>> _uniqueIndexCollection = [];
        private readonly ObservableCollection<T> _collection = [];
        private readonly Dictionary<string, PropInfo> _indexedProps;
        private readonly IndexObserverToken _token;

        public IndexedCollection()
        {
            _token = new IndexObserverToken(this);

            _indexedProps = typeof(T).GetPropertiesByAttribute<Indexed>().ToDictionary(k => k.Name, v => new PropInfo(v));

            foreach (var prop in _indexedProps)
            {
                if (prop.Value.IsUnique) _uniqueIndexCollection.Add(prop.Key, []);
                else _indexCollection.Add(prop.Key, []);
            }
        }

        /// <summary>
        /// This event gets fired whenever the collection is modified.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add { _collection.CollectionChanged += value; }
            remove { _collection.CollectionChanged -= value; }
        }

        public event EventHandler<IndexViolationEventArgs<T>>? IndexViolated;

        #region Public Methods

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">A string that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <param name="value">The object identified by the <paramref name="key"/>.</param>
        /// <returns>True if the operation succeeded.</returns>
        public bool TryGetByIndex(string name, object key, [NotNullWhen(true)] out List<T>? value)
        {
            if (!_indexCollection.TryGetValue(name, out var internalValue)
                || key.ToString() is not string k
                || !internalValue.TryGetValue(k, out value))
            {
                value = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="property_lambda"/>.
        /// </summary>
        /// <param name="property_lambda">A property lambda that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <param name="value">The object identified by the <paramref name="key"/>.</param>
        /// <returns>True if the operation succeeded.</returns>
        public bool TryGetByIndex(Expression<Func<T, object>> property_lambda, object key, [NotNullWhen(true)] out List<T>? value)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);

            return TryGetByIndex(name, key, out value);
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">A string that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A <see cref="Maybe{T}"/> that holds the object.</returns>
        public Maybe<List<T>> MaybeGetByIndex(string name, object key)
        {
            if (!_indexCollection.TryGetValue(name, out var value))
                return new Maybe<List<T>>(new ArgumentException("Property does not have an index."));

            return key.ToString() is string k && value.TryGetValue(k, out var found)
                ? new Maybe<List<T>>(found)
                : new Maybe<List<T>>(new KeyNotFoundException("The key was not found in the index."));
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="property_lambda"/>.
        /// </summary>
        /// <param name="property_lambda">A property lambda that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A <see cref="Maybe{T}"/> that holds the object.</returns>
        public Maybe<List<T>> MaybeGetByIndex(Expression<Func<T, object>> property_lambda, object key)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);

            return MaybeGetByIndex(name, key);
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">A string that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A <see cref="List{T}"/> that holds the object.</returns>
        public List<T> GetByIndex(string name, object key)
        {
            return !_indexCollection.TryGetValue(name, out var value)
                ? throw new ArgumentException("Property does not have an index.")
                : new List<T>(value[key.ToString()]);
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="property_lambda"/>.
        /// </summary>
        /// <param name="property_lambda">A property lambda that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A <see cref="List{T}"/> that holds the object.</returns>
        public List<T> GetByIndex(Expression<Func<T, object>> property_lambda, object key)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);

            return GetByIndex(name, key);
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">A string that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <param name="value">The object identified by the <paramref name="key"/>.</param>
        /// <returns>True if the operation succeeded.</returns>
        public bool TryGetByUniqueIndex(string name, object key, [NotNullWhen(true)] out T? value)
        {
            if (!_uniqueIndexCollection.TryGetValue(name, out var internalValue)
                || key.ToString() is not string k
                || !internalValue.TryGetValue(k, out value))
            {
                value = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="property_lambda"/>.
        /// </summary>
        /// <param name="property_lambda">A property lambda that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <param name="value">The object identified by the <paramref name="key"/>.</param>
        /// <returns>True if the operation succeeded.</returns>
        public bool TryGetByUniqueIndex(Expression<Func<T, object>> property_lambda, object key, [NotNullWhen(true)] out T? value)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);

            return TryGetByUniqueIndex(name, key, out value);
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">A string that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A <see cref="Maybe{T}"/> that holds the object.</returns>
        public Maybe<T> MaybeGetByUniqueIndex(string name, object key)
        {
            if (!_uniqueIndexCollection.TryGetValue(name, out var value))
                return new Maybe<T>(new ArgumentException("Property does not have an index."));

            return key.ToString() is string k && value.TryGetValue(k, out var found)
                ? new Maybe<T>(found)
                : new Maybe<T>(new KeyNotFoundException("The key was not found in the index."));
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="property_lambda"/>.
        /// </summary>
        /// <param name="property_lambda">A property lambda that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A <see cref="Maybe{T}"/> that holds the object.</returns>
        public Maybe<T> MaybeGetByUniqueIndex(Expression<Func<T, object>> property_lambda, object key)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);

            return MaybeGetByUniqueIndex(name, key);
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">A string that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A <see cref="T"/> that holds the object.</returns>
        public T GetByUniqueIndex(string name, object key)
        {
            return !_uniqueIndexCollection.TryGetValue(name, out var value)
                ? throw new ArgumentException("Property does not have an index.")
                : value[key.ToString()];
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="property_lambda"/>.
        /// </summary>
        /// <param name="property_lambda">A property lambda that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A <see cref="T"/> that holds the object.</returns>
        public T GetByUniqueIndex(Expression<Func<T, object>> property_lambda, object key)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);

            return GetByUniqueIndex(name, key);
        }

        /// <summary>
        /// This function returns a copy of the dictionary index for a given uniquely indexed property.
        /// The values themselves are not
        /// copied, but the Dictionary is, and can be modified without damaging the IndexedCollection.
        /// </summary>
        /// <param name="name">a string that indicates an indexed property name</param>
        /// <returns>A <see cref="Maybe{T}"/> with a copy of the dictionary.</returns>
        public Maybe<Dictionary<string, T>> MaybeGetUniqueIndex(string name)
        {
            return _uniqueIndexCollection.TryGetValue(name, out Dictionary<string, T>? value)
                ? new Maybe<Dictionary<string, T>>(value.ToDictionary(k => k.Key, v => v.Value))
                : new Maybe<Dictionary<string, T>>(new ArgumentException("Property \"" + name + "\" does not exist."));
        }

        /// <summary>
        /// This function returns a copy of the dictionary index for a given uniquely indexed property.
        /// The values themselves are not
        /// copied, but the Dictionary is, and can be modified without damaging the IndexedCollection.
        /// </summary>
        /// <param name="property_lambda">an expression in the form of p=>p.PropertyName</param>
        /// <returns>A <see cref="Maybe{T}"/> with a copy of the dictionary.</returns>
        public Maybe<Dictionary<string, T>> MaybeGetUniqueIndex(Expression<Func<T, object>> property_lambda)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);
            return MaybeGetUniqueIndex(name);
        }

        /// <summary>
        /// This function returns a copy of the dictionary index for a given uniquely indexed property.
        /// The values themselves are not
        /// copied, but the Dictionary is, and can be modified without damaging the IndexedCollection.
        /// </summary>
        /// <param name="name">a string that indicates an indexed property name</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/>  with a copy of the dictionary.</returns>
        public Dictionary<string, T> GetUniqueIndex(string name)
        {
            return _uniqueIndexCollection.TryGetValue(name, out var value)
                ? value.ToDictionary(k => k.Key, v => v.Value)
                : throw new ArgumentException("Property \"" + name + "\" does not exist.");
        }

        /// <summary>
        /// This function returns a copy of the dictionary index for a given uniquely indexed property.
        /// The values themselves are not
        /// copied, but the Dictionary is, and can be modified without damaging the IndexedCollection.
        /// </summary>
        /// <param name="property_lambda">an expression in the form of p=>p.PropertyName</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> with a copy of the dictionary.</returns>
        public Dictionary<string, T> GetUniqueIndex(Expression<Func<T, object>> property_lambda)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);
            return GetUniqueIndex(name);
        }

        /// <summary>
        /// This function returns a copy of the dictionary index for a given property.  The values themselves are not
        /// copied, but the Dictionary is, and can be modified without damaging the IndexedCollection.
        /// </summary>
        /// <param name="name">a string that indicates an indexed property name</param>
        /// <returns>A <see cref="Maybe{T}"/> with a copy of the dictionary.</returns>
        public Maybe<Dictionary<string, List<T>>> MaybeGetIndex(string name)
        {
            return _indexCollection.TryGetValue(name, out var value)
                ? new Maybe<Dictionary<string, List<T>>>(value.ToDictionary(k => k.Key, v => v.Value.ToList()))
                : new Maybe<Dictionary<string, List<T>>>(new ArgumentException("Property \"" + name + "\" does not exist."));
        }

        /// <summary>
        /// This function returns a copy of the dictionary index for a given property.  The values themselves are not
        /// copied, but the Dictionary is, and can be modified without damaging the IndexedCollection.
        /// </summary>
        /// <param name="propertyLambda">an expression in the form of p=>p.PropertyName</param>
        /// <returns>A <see cref="Maybe{T}"/> with a copy of the dictionary.</returns>
        public Maybe<Dictionary<string, List<T>>> MaybeGetIndex(Expression<Func<T, object>> propertyLambda)
        {
            var name = PropertyExpressions.GetPropertyName(propertyLambda);

            return MaybeGetIndex(name);
        }

        /// <summary>
        /// This function returns a copy of the dictionary index for a given property.  The values themselves are not
        /// copied, but the Dictionary is, and can be modified without damaging the IndexedCollection.
        /// </summary>
        /// <param name="name">a string that indicates an indexed property name</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> with a copy of the dictionary.</returns>
        public Dictionary<string, List<T>> GetIndex(string name)
        {
            return _indexCollection.TryGetValue(name, out var value)
                ? value.ToDictionary(k => k.Key, v => v.Value.ToList())
                : throw new ArgumentException("Property \"" + name + "\" does not exist.");
        }

        /// <summary>
        /// This function returns a copy of the dictionary index for a given property.  The values themselves are not
        /// copied, but the Dictionary is, and can be modified without damaging the IndexedCollection.
        /// </summary>
        /// <param name="propertyLambda">an expression in the form of p=>p.PropertyName</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> with a copy of the dictionary.</returns>
        public Dictionary<string, List<T>> GetIndex(Expression<Func<T, object>> propertyLambda)
        {
            var name = PropertyExpressions.GetPropertyName(propertyLambda);

            return GetIndex(name);
        }

        /// <summary>
        /// Clears then rebuilds the indices.  This is sometimes necessary if an exception was thrown during an operation.
        /// </summary>
        public void RefreshIndices()
        {
            foreach (var idx in _indexCollection) idx.Value.Clear();
            foreach (var idx in _uniqueIndexCollection) idx.Value.Clear();
            foreach (var item in _collection)
            {
                item.DetachObserver(_token);
                AddToDictionaries(item);
                item.AttachObserver(_token);
            }
        }

        /// <summary>
        /// Adds an item to the collection.  Throws an exception of type <see cref="OperationCanceledException"/> if the item is unable to be added.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <exception cref="OperationCanceledException">Thrown if the method is unable to add the item to one of the indices</exception>
        public void Add(T item)
        {
            if (!TryAdd(item))
                throw new OperationCanceledException(IndexViolationMessage);
        }

        /// <summary>
        /// Adds an item to the collection.  Throws an exception of type <see cref="OperationCanceledException"/> if the item is unable to be added.
        /// </summary>
        /// <param name="obj">The obejct to add</param>
        /// <returns>True if the item was added successfully.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the method is unable to add the item to one of the indices</exception>
        public bool TryAdd(T obj)
        {
            if (!AddToDictionaries(obj))
                return false;

            _collection.Add(obj);
            obj.AttachObserver(_token);
            return true;
        }

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <returns>Returns true if the item was successfully removed.</returns>
        public bool Remove(T item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var r = RemoveFromDictionaries(item);
            _collection.Remove(item);
            item.DetachObserver(_token);

            return r;
        }

        /// <summary>
        /// Removes items that match the supplied predicate.
        /// </summary>
        /// <param name="predicate">A function that evaluates if the item is something we want to remove.</param>
        /// <returns>True if the operation succeeds.</returns>
        public bool Remove(Func<T, bool> predicate)
        {
            var items = _collection.Where(predicate).ToList();
            foreach (var o in items)
                Remove(o);
            return true;
        }

        /// <summary>
        /// Removes an item at the supplied location.
        /// </summary>
        /// <param name="index">The index to remove the item from.</param>
        public void RemoveAt(int index)
        {
            var item = _collection[index];
            Remove(item);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Called by an item before one of its properties changes, with the value before and after the change.
        /// </summary>
        void IIndexObserver.OnIndexedPropertyChanging(IndexedClass sender, string propertyName, object? before, object? after)
        {
            if (!_indexedProps.TryGetValue(propertyName, out var prop)) return; // don't update the index if the property isn't indexed

            if (prop.IsUnique)
            {
                var unique_index = _uniqueIndexCollection[propertyName];

                if (prop.IsCollection)
                {
                    if (before is IEnumerable beforeItems)
                    {
                        foreach (var item in beforeItems)
                        {
                            if (item?.ToString() is string key) unique_index.Remove(key);
                        }
                    }

                    if (after is IEnumerable afterItems)
                    {
                        foreach (var item in afterItems)
                        {
                            if (item?.ToString() is string key) unique_index.Add(key, (T)sender);
                        }
                    }
                }
                else
                {
                    if (before?.ToString() is string beforeKey) unique_index.Remove(beforeKey);
                    if (after?.ToString() is string afterKey) unique_index.Add(afterKey, (T)sender);
                }
            }
            else
            {
                var index = _indexCollection[propertyName];

                if (prop.IsCollection)
                {
                    if (before is IEnumerable beforeItems)
                    {
                        foreach (var item in beforeItems)
                            RemoveFromIndex(sender, item, index);
                    }

                    if (after is IEnumerable afterItems)
                    {
                        foreach (var item in afterItems)
                            AddToIndex(sender, item, index);
                    }
                }
                else
                {
                    RemoveFromIndex(sender, before, index);
                    AddToIndex(sender, after, index);
                }
            }
        }

        /// <summary>
        /// Removes a value from the dictionaries (indexes).  This is called by the onIndexedPropertyChanged function.
        /// </summary>
        /// <param name="sender">The value/object being added.</param>
        /// <param name="item">The current value of the property</param>
        /// <param name="index">The altered value.</param>
        private static void RemoveFromIndex(object sender, object? item, Dictionary<string, List<T>>? index)
        {
            ArgumentNullException.ThrowIfNull(index);

            removeFromBucket(index, item?.ToString(), (T)sender);
        }

        /// <summary>
        /// Removes <paramref name="item"/> from the bucket for <paramref name="key"/>, dropping the bucket once empty.
        /// </summary>
        private static void removeFromBucket(Dictionary<string, List<T>> index, string? key, T item)
        {
            if (key is null || !index.TryGetValue(key, out var bucket)) return;

            bucket.Remove(item);
            if (bucket.Count == 0)
                index.Remove(key);
        }

        /// <summary>
        /// Adds a value to the dictionaries (indexes).  This is called by the onIndexedPropertyChanged function.
        /// </summary>
        /// <param name="sender">The value/object being added.</param>
        /// <param name="item">The current value of the property</param>
        /// <param name="index">The altered value.</param>
        private static void AddToIndex(object sender, object? item, Dictionary<string, List<T>>? index)
        {
            ArgumentNullException.ThrowIfNull(index);
            if (item?.ToString() is not string key) return;
            if (index.TryGetValue(key, out List<T>? value))
                value.Add((T)sender);
            else
                index.Add(key, [(T)sender]);
        }

        /// <summary>
        /// Adds the value to all the relevant dictionaries (indexes).  Every unique key is checked before any index
        /// is written, so a violation leaves every index unchanged.
        /// </summary>
        /// <param name="val">The value to add.</param>
        /// <returns>True if the operation succeeded.</returns>
        private bool AddToDictionaries(T val)
        {
            if (!TryCollectUniqueKeys(val, null, out var uniqueKeys))
                return false;

            WriteToDictionaries(val, uniqueKeys);
            return true;
        }

        /// <summary>
        /// Gathers the unique index keys <paramref name="val"/> would occupy, raising <see cref="IndexViolated"/> and
        /// returning false on the first key already held by another item.  Keys held by <paramref name="replacing"/>
        /// count as free, since that item is about to leave the indexes.
        /// </summary>
        private bool TryCollectUniqueKeys(
            T val,
            T? replacing,
            out List<(Dictionary<string, T> Index, string Key)> uniqueKeys
        )
        {
            uniqueKeys = [];
            foreach (var prop in _indexedProps.Values)
            {
                if (!prop.IsUnique) continue;

                var p = prop.Info;
                var index = _uniqueIndexCollection[p.Name];

                if (prop.IsCollection || prop.IsDict)
                {
                    // its possible for a property to be specified as indexable, but be null.
                    if (p.GetValue(val) is not IEnumerable objs) continue;

                    // a key repeated within the item's own collection violates the index too
                    var seen = new HashSet<string>();
                    foreach (var o in objs)
                    {
                        // null values are never indexed
                        if (o?.ToString() is not string key) continue;
                        if (isFree(index, key, replacing) && seen.Add(key))
                        {
                            uniqueKeys.Add((index, key));
                            continue;
                        }

                        IndexViolated?.Invoke(this, new IndexViolationEventArgs<T>(val, "Value violated index '" + p.Name + "' using key '" + key + "'"));
                        return false;
                    }
                }
                else
                {
                    // null values are never indexed
                    if (p.GetValue(val, null)?.ToString() is not string key) continue;
                    if (isFree(index, key, replacing))
                    {
                        uniqueKeys.Add((index, key));
                        continue;
                    }

                    IndexViolated?.Invoke(this, new IndexViolationEventArgs<T>(val, "Value violated index '" + p.Name + "' using key '" + key + "'"));
                    return false;
                }
            }

            return true;
        }

        private static bool isFree(Dictionary<string, T> index, string key, T? replacing)
        {
            return !index.TryGetValue(key, out var owner)
                || (replacing is not null && ReferenceEquals(owner, replacing));
        }

        /// <summary>
        /// Writes <paramref name="val"/> into every index, using unique keys already checked by
        /// <see cref="TryCollectUniqueKeys"/>.
        /// </summary>
        private void WriteToDictionaries(T val, List<(Dictionary<string, T> Index, string Key)> uniqueKeys)
        {
            // assignment rather than Add: a key the replaced item still holds is overwritten instead of throwing
            foreach (var (index, key) in uniqueKeys)
                index[key] = val;

            foreach (var prop in _indexedProps.Values)
            {
                if (prop.IsUnique) continue;

                var p = prop.Info;
                if (prop.IsCollection || prop.IsDict)
                {
                    // its possible for a property to be specified as indexed, but be null.
                    if (p.GetValue(val) is not IEnumerable objs) continue;

                    foreach (var o in objs)
                    {
                        var key = prop.IsDict
                            ? o?.GetType().GetProperty(nameof(KeyValuePair<string, T>.Value))?.GetValue(o)?.ToString()
                            : o?.ToString();
                        _indexCollection[p.Name].SafeAdd(key, val);
                    }
                }
                else
                {
                    var key = p.GetValue(val)?.ToString();
                    _indexCollection[p.Name].SafeAdd(key, val);
                }
            }
        }

        /// <summary>
        /// Removes the value from all the relevant dictionaries (indexes)
        /// </summary>
        /// <param name="val">The value to remove</param>
        /// <returns>True if the operation succeeded.</returns>
        private bool RemoveFromDictionaries(T val)
        {
            foreach (var prop in _indexedProps.Values)
            {
                var p = prop.Info;

                if (prop.IsUnique)
                {
                    if (prop.IsCollection || prop.IsDict)
                    {
                        // its possible for a property to be specified as indexable, but be null.
                        if (p.GetValue(val) is not IEnumerable objs) continue;

                        foreach (var o in objs)
                        {
                            if (o?.ToString() is not string key || _uniqueIndexCollection[p.Name].Remove(key)) continue;

                            IndexViolated?.Invoke(this, new IndexViolationEventArgs<T>(val, "Value was not found in index '" + p.Name + "' using key '" + key + "'"));
                            return false;
                        }
                    }
                    else
                    {
                        var key = p.GetValue(val, null)?.ToString();
                        if (key is null || _uniqueIndexCollection[p.Name].Remove(key)) continue;

                        IndexViolated?.Invoke(this, new IndexViolationEventArgs<T>(val, "Value was not found in index '" + p.Name + "' using key '" + key + "'"));
                        return false;
                    }
                }
                else
                {
                    if (prop.IsCollection || prop.IsDict)
                    {
                        // its possible for a property to be specified as indexed, but be null.
                        if (p.GetValue(val) is not IEnumerable objs)
                        {
                            continue;
                        }

                        foreach (var o in objs)
                        {
                            // null values are never indexed, so there is nothing to remove for them
                            var key = prop.IsDict
                                ? o?.GetType().GetProperty(nameof(KeyValuePair<string, T>.Value))?.GetValue(o)?.ToString()
                                : o?.ToString();
                            removeFromBucket(_indexCollection[p.Name], key, val);
                        }
                    }
                    else
                    {
                        removeFromBucket(_indexCollection[p.Name], p.GetValue(val)?.ToString(), val);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if a property is uniquely indexed or not.
        /// </summary>
        /// <param name="prop">A <see cref="PropertyInfo"/> to check for the IsUnique attribute.</param>
        /// <returns>True if the property is unigue.</returns>
        private static bool IsUnique(PropertyInfo prop)
        {
            var attrs = prop.GetCustomAttributes(typeof(Indexed), false);
            return attrs[0] is Indexed indexed && attrs.Length > 0 && indexed.IsUnique;
        }

        #endregion Private Methods

        #region Interface Implementations

        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        public void Clear()
        {
            foreach (var obj in _collection) obj.DetachObserver(_token);

            _collection.Clear();

            foreach (var p in _indexedProps.Values)
            {
                if (p.IsUnique)
                    _uniqueIndexCollection[p.Name].Clear();
                else
                    _indexCollection[p.Name].Clear();
            }
        }

        public bool Contains(T item)
        {
            return _collection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        public int IndexOf(T item)
        {
            return _collection.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ArgumentNullException.ThrowIfNull(item);

            // checked before the indexes are written so a bad position cannot leave the item half indexed
            if (index < 0 || index > _collection.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "Index must be within the bounds of the collection."
                );
            }

            if (!AddToDictionaries(item))
                throw new OperationCanceledException(IndexViolationMessage);

            item.DetachObserver(_token);
            _collection.Insert(index, item);
            item.AttachObserver(_token);
        }

        public int Count => _collection.Count;

        public bool IsReadOnly => ((ICollection<T>)_collection).IsReadOnly;

        public T this[int index]
        {
            get
            {
                return _collection[index];
            }

            set
            {
                var item = _collection[index];

                // check before removing the old item so a violation leaves every index exactly as it was
                if (!TryCollectUniqueKeys(value, item, out var uniqueKeys))
                    throw new OperationCanceledException(IndexViolationMessage);

                RemoveFromDictionaries(item);
                WriteToDictionaries(value, uniqueKeys);

                item.DetachObserver(_token);
                _collection[index] = value;
                value.AttachObserver(_token);
            }
        }

        #endregion Interface Implementations

        #region Linq

        public T First()
        {
            return _collection.First();
        }

        public T First(Func<T, bool> predicate)
        {
            return _collection.First(predicate);
        }

        public void Each(Action<T> action)
        {
            foreach (var item in _collection)
                action(item);
        }

        #endregion Linq

        #region Private classes

        private class PropInfo
        {
            public PropInfo(PropertyInfo info)
            {
                Info = info;
                IsUnique = IsUnique(info);
                IsCollection = info.PropertyType.HasInterface<ICollection>();
                IsDict = info.PropertyType.HasInterface<IDictionary>();
            }

            public PropertyInfo Info { get; }

            public bool IsUnique { get; }

            public string Name => Info.Name;

            public bool IsDict { get; }

            public bool IsCollection { get; }
        }

        #endregion Private classes
    }
}