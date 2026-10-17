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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace JMW.Collections
{
    /// <summary>
    /// A collection that allows you to index more than one property.  The indices are maintained, so when a value
    /// is modified, the indices are updated.
    /// </summary>
    /// <remarks>
    /// Every lookup has an overload that names the property with a lambda, such as <c>p =&gt; p.Id</c>. The compiler
    /// builds an expression tree at each such call, which costs far more than the lookup itself, so in hot paths use
    /// the overload that takes the name, for example <c>nameof(Device.Id)</c>.
    /// </remarks>
    /// <typeparam name="T">The type of object being stored in the collection.</typeparam>
    public class IndexedCollection<T> : INotifyCollectionChanged, IList<T>, IIndexObserver where T : IndexedClass
    {
        private const string IndexViolationMessage =
            "Operation was cancelled due to an index violation. The Index may have been violated.";

        // Reflection over T's [Indexed] properties happens once per T, not once per collection.
        private static readonly PropInfo[] s_uniqueProps;
        private static readonly PropInfo[] s_nonUniqueProps;
        private static readonly Dictionary<string, PropInfo> s_indexedProps;

        static IndexedCollection()
        {
            var unique = new List<PropInfo>();
            var nonUnique = new List<PropInfo>();
            foreach (var info in typeof(T).GetPropertiesByAttribute<Indexed>())
            {
                if (IsUnique(info))
                    unique.Add(new PropInfo(info, unique.Count));
                else
                    nonUnique.Add(new PropInfo(info, nonUnique.Count));
            }

            s_uniqueProps = [.. unique];
            s_nonUniqueProps = [.. nonUnique];
            s_indexedProps = unique.Concat(nonUnique).ToDictionary(p => p.Name, StringComparer.Ordinal);
        }

        // The same dictionaries, reachable by property name for the public lookups and by slot for the hot paths.
        // Buckets are deques so an item is removed from either end in O(1); they are handed out as read-only views.
        private readonly Dictionary<string, Dictionary<string, Deque<T>>> _indexCollection = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, T>> _uniqueIndexCollection = new(StringComparer.Ordinal);
        private readonly Dictionary<string, T>[] _uniqueIndexes;
        private readonly Dictionary<string, Deque<T>>[] _nonUniqueIndexes;
        private readonly Dictionary<string, IReadOnlyDictionary<string, T>> _uniqueIndexViews = new(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<T>>> _indexViews = new(StringComparer.Ordinal);
        // A deque rather than a List: removing from either end is O(1), where a List shifts every later item.
        private readonly Deque<T> _items = new();
        private readonly IndexObserverToken _token;

        // Reused across operations; the collection is not thread-safe, so one set per instance is enough.
        private readonly List<string> _keys = [];
        private readonly List<string> _otherKeys = [];
        private readonly HashSet<string> _seenKeys = new(StringComparer.Ordinal);
        private readonly List<(Dictionary<string, T> Index, string Key)> _uniqueKeys = [];

        public IndexedCollection()
        {
            _token = new IndexObserverToken(this);

            _uniqueIndexes = new Dictionary<string, T>[s_uniqueProps.Length];
            foreach (var prop in s_uniqueProps)
            {
                _uniqueIndexes[prop.Slot] = new Dictionary<string, T>(StringComparer.Ordinal);
                _uniqueIndexCollection.Add(prop.Name, _uniqueIndexes[prop.Slot]);
                _uniqueIndexViews.Add(prop.Name, new ReadOnlyDictionary<string, T>(_uniqueIndexes[prop.Slot]));
            }

            _nonUniqueIndexes = new Dictionary<string, Deque<T>>[s_nonUniqueProps.Length];
            foreach (var prop in s_nonUniqueProps)
            {
                _nonUniqueIndexes[prop.Slot] = new Dictionary<string, Deque<T>>(StringComparer.Ordinal);
                _indexCollection.Add(prop.Name, _nonUniqueIndexes[prop.Slot]);
                _indexViews.Add(prop.Name, new BucketIndexView(_nonUniqueIndexes[prop.Slot]));
            }
        }

        /// <summary>
        /// This event gets fired whenever the collection is modified.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public event EventHandler<IndexViolationEventArgs<T>>? IndexViolated;

        #region Public Methods

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">A string that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <param name="value">A read-only view of the items under <paramref name="key"/>, in the order they were added.
        /// It is not a copy: enumerating it while the collection changes throws, and after a change the key should be
        /// looked up again.</param>
        /// <returns>True if the operation succeeded.</returns>
        public bool TryGetByIndex(string name, object key, [NotNullWhen(true)] out IReadOnlyList<T>? value)
        {
            if (_indexCollection.TryGetValue(name, out var internalValue)
                && KeyOf(key) is string k
                && internalValue.TryGetValue(k, out var bucket))
            {
                value = bucket;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="property_lambda"/>.
        /// </summary>
        /// <param name="property_lambda">A property lambda that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <param name="value">The object identified by the <paramref name="key"/>.</param>
        /// <returns>True if the operation succeeded.</returns>
        public bool TryGetByIndex(Expression<Func<T, object>> property_lambda, object key, [NotNullWhen(true)] out IReadOnlyList<T>? value)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);

            return TryGetByIndex(name, key, out value);
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">A string that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A <see cref="Maybe{T}"/> that holds a read-only view of the items, as described for
        /// <see cref="TryGetByIndex(string, object, out IReadOnlyList{T})"/>.</returns>
        public Maybe<IReadOnlyList<T>> MaybeGetByIndex(string name, object key)
        {
            if (!_indexCollection.TryGetValue(name, out var value))
                return new Maybe<IReadOnlyList<T>>(new ArgumentException("Property does not have an index."));

            return KeyOf(key) is string k && value.TryGetValue(k, out var found)
                ? new Maybe<IReadOnlyList<T>>((IReadOnlyList<T>)found)
                : new Maybe<IReadOnlyList<T>>(new KeyNotFoundException("The key was not found in the index."));
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="property_lambda"/>.
        /// </summary>
        /// <param name="property_lambda">A property lambda that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A <see cref="Maybe{T}"/> that holds the object.</returns>
        public Maybe<IReadOnlyList<T>> MaybeGetByIndex(Expression<Func<T, object>> property_lambda, object key)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);

            return MaybeGetByIndex(name, key);
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">A string that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A read-only view of the items, as described for
        /// <see cref="TryGetByIndex(string, object, out IReadOnlyList{T})"/>.</returns>
        /// <exception cref="KeyNotFoundException">No item has <paramref name="key"/>.</exception>
        public IReadOnlyList<T> GetByIndex(string name, object key)
        {
            return !_indexCollection.TryGetValue(name, out var value)
                ? throw new ArgumentException("Property does not have an index.")
                : value[KeyOf(key) ?? throw new ArgumentNullException(nameof(key))];
        }

        /// <summary>
        /// Gets the object keyed by the <paramref name="key"/> for the index indicated by the <paramref name="property_lambda"/>.
        /// </summary>
        /// <param name="property_lambda">A property lambda that identifies a property that has been indexed.</param>
        /// <param name="key">The value in the index that the object is keyed to.</param>
        /// <returns>A read-only view of the items.</returns>
        public IReadOnlyList<T> GetByIndex(Expression<Func<T, object>> property_lambda, object key)
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
                || KeyOf(key) is not string k
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

            return KeyOf(key) is string k && value.TryGetValue(k, out var found)
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
                : value[KeyOf(key) ?? throw new ArgumentNullException(nameof(key))];
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
        /// A read-only view of the index for a uniquely indexed property: key to item. It is not a copy, so it
        /// reflects later changes to the collection, and enumerating it while the collection changes throws.
        /// </summary>
        /// <param name="name">a string that indicates an indexed property name</param>
        /// <returns>A <see cref="Maybe{T}"/> with the view.</returns>
        public Maybe<IReadOnlyDictionary<string, T>> MaybeGetUniqueIndex(string name)
        {
            return _uniqueIndexViews.TryGetValue(name, out var view)
                ? new Maybe<IReadOnlyDictionary<string, T>>(view)
                : new Maybe<IReadOnlyDictionary<string, T>>(new ArgumentException("Property \"" + name + "\" does not exist."));
        }

        /// <summary>
        /// A read-only view of the index for a uniquely indexed property, as described for
        /// <see cref="MaybeGetUniqueIndex(string)"/>.
        /// </summary>
        /// <param name="property_lambda">an expression in the form of p=>p.PropertyName</param>
        /// <returns>A <see cref="Maybe{T}"/> with the view.</returns>
        public Maybe<IReadOnlyDictionary<string, T>> MaybeGetUniqueIndex(Expression<Func<T, object>> property_lambda)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);
            return MaybeGetUniqueIndex(name);
        }

        /// <summary>
        /// A read-only view of the index for a uniquely indexed property, as described for
        /// <see cref="MaybeGetUniqueIndex(string)"/>.
        /// </summary>
        /// <param name="name">a string that indicates an indexed property name</param>
        /// <returns>The view.</returns>
        public IReadOnlyDictionary<string, T> GetUniqueIndex(string name)
        {
            return _uniqueIndexViews.TryGetValue(name, out var view)
                ? view
                : throw new ArgumentException("Property \"" + name + "\" does not exist.");
        }

        /// <summary>
        /// A read-only view of the index for a uniquely indexed property, as described for
        /// <see cref="MaybeGetUniqueIndex(string)"/>.
        /// </summary>
        /// <param name="property_lambda">an expression in the form of p=>p.PropertyName</param>
        /// <returns>The view.</returns>
        public IReadOnlyDictionary<string, T> GetUniqueIndex(Expression<Func<T, object>> property_lambda)
        {
            var name = PropertyExpressions.GetPropertyName(property_lambda);
            return GetUniqueIndex(name);
        }

        /// <summary>
        /// A read-only view of the index for a non-uniquely indexed property: key to the items under it, in the order
        /// they were added. It is not a copy, so it reflects later changes to the collection, and enumerating it, or a
        /// list in it, while the collection changes throws.
        /// </summary>
        /// <param name="name">a string that indicates an indexed property name</param>
        /// <returns>A <see cref="Maybe{T}"/> with the view.</returns>
        public Maybe<IReadOnlyDictionary<string, IReadOnlyList<T>>> MaybeGetIndex(string name)
        {
            return _indexViews.TryGetValue(name, out var view)
                ? new Maybe<IReadOnlyDictionary<string, IReadOnlyList<T>>>(view)
                : new Maybe<IReadOnlyDictionary<string, IReadOnlyList<T>>>(new ArgumentException("Property \"" + name + "\" does not exist."));
        }

        /// <summary>
        /// A read-only view of the index for a non-uniquely indexed property, as described for
        /// <see cref="MaybeGetIndex(string)"/>.
        /// </summary>
        /// <param name="propertyLambda">an expression in the form of p=>p.PropertyName</param>
        /// <returns>A <see cref="Maybe{T}"/> with the view.</returns>
        public Maybe<IReadOnlyDictionary<string, IReadOnlyList<T>>> MaybeGetIndex(Expression<Func<T, object>> propertyLambda)
        {
            var name = PropertyExpressions.GetPropertyName(propertyLambda);

            return MaybeGetIndex(name);
        }

        /// <summary>
        /// A read-only view of the index for a non-uniquely indexed property, as described for
        /// <see cref="MaybeGetIndex(string)"/>.
        /// </summary>
        /// <param name="name">a string that indicates an indexed property name</param>
        /// <returns>The view.</returns>
        public IReadOnlyDictionary<string, IReadOnlyList<T>> GetIndex(string name)
        {
            return _indexViews.TryGetValue(name, out var view)
                ? view
                : throw new ArgumentException("Property \"" + name + "\" does not exist.");
        }

        /// <summary>
        /// A read-only view of the index for a non-uniquely indexed property, as described for
        /// <see cref="MaybeGetIndex(string)"/>.
        /// </summary>
        /// <param name="propertyLambda">an expression in the form of p=>p.PropertyName</param>
        /// <returns>The view.</returns>
        public IReadOnlyDictionary<string, IReadOnlyList<T>> GetIndex(Expression<Func<T, object>> propertyLambda)
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
            foreach (var item in _items)
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

            _items.Add(obj);
            obj.AttachObserver(_token);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, obj, _items.Count - 1);
            return true;
        }

        /// <summary>
        /// Removes an item from the collection.  Items are matched by reference, since the indexes track instances.
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <returns>Returns true if the item was successfully removed.</returns>
        public bool Remove(T item)
        {
            ArgumentNullException.ThrowIfNull(item);

            // an item that is not in the collection must not touch the indexes: its keys may belong to a member
            var at = FindByReference(item);
            if (at < 0)
                return false;

            RemoveAtCore(at);
            return true;
        }

        /// <summary>
        /// Removes items that match the supplied predicate.
        /// </summary>
        /// <param name="predicate">A function that evaluates if the item is something we want to remove.</param>
        /// <returns>True if the operation succeeds.</returns>
        public bool Remove(Func<T, bool> predicate)
        {
            if (CollectionChanged is not null)
            {
                // listeners get one Remove event per item, with its index at the time
                for (var i = _items.Count - 1; i >= 0; i--)
                {
                    if (predicate(_items[i]))
                        RemoveAtCore(i);
                }

                return true;
            }

            // nobody is listening, so compact in one pass instead of shifting the list once per removal
            var kept = 0;
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (predicate(item))
                {
                    RemoveFromDictionaries(item);
                    item.DetachObserver(_token);
                    continue;
                }

                _items[kept++] = item;
            }

            _items.Truncate(kept);
            return true;
        }

        /// <summary>
        /// Removes an item at the supplied location.
        /// </summary>
        /// <param name="index">The index to remove the item from.</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within the bounds of the collection.");

            RemoveAtCore(index);
        }

        private void RemoveAtCore(int index)
        {
            var item = _items[index];
            RemoveFromDictionaries(item);
            _items.RemoveAt(index);
            item.DetachObserver(_token);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        }

        // Items are matched by identity: the indexes track instances, whatever Equals says.
        private int FindByReference(T item) => _items.IndexOfReference(item);

        private void OnCollectionChanged(NotifyCollectionChangedAction action, T item, int index)
        {
            // the args are only allocated when someone is listening
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index));
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Called by an item before one of its properties changes, with the value before and after the change.
        /// </summary>
        void IIndexObserver.OnIndexedPropertyChanging(IndexedClass sender, string propertyName, object? before, object? after)
        {
            if (!s_indexedProps.TryGetValue(propertyName, out var prop)) return; // don't update the index if the property isn't indexed

            var item = (T)sender;

            if (!prop.IsCollection && !prop.IsDict)
            {
                // scalars, the common case, need no key lists
                var beforeKey = KeyOf(before);
                var afterKey = KeyOf(after);
                if (prop.IsUnique)
                {
                    var index = _uniqueIndexes[prop.Slot];
                    if (afterKey is not null) RequireFree(index, prop, afterKey, item);
                    if (beforeKey is not null) index.Remove(beforeKey);
                    if (afterKey is not null) index[afterKey] = item;
                }
                else
                {
                    var index = _nonUniqueIndexes[prop.Slot];
                    if (beforeKey is not null) removeFromBucket(index, beforeKey, item);
                    if (afterKey is not null) addToBucket(index, afterKey, item);
                }

                return;
            }

            // new lists rather than the shared ones: this can run from user code inside an Add that is using them
            var beforeKeys = new List<string>();
            var afterKeys = new List<string>();
            prop.AddKeys(before, beforeKeys);
            prop.AddKeys(after, afterKeys);

            if (prop.IsUnique)
            {
                var index = _uniqueIndexes[prop.Slot];
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var key in afterKeys)
                {
                    if (!seen.Add(key))
                        throw new ArgumentException("The value contains the key '" + key + "' more than once, which violates the unique index '" + prop.Name + "'.");
                    RequireFree(index, prop, key, item);
                }

                foreach (var key in beforeKeys) index.Remove(key);
                foreach (var key in afterKeys) index[key] = item;
            }
            else
            {
                var index = _nonUniqueIndexes[prop.Slot];
                foreach (var key in beforeKeys) removeFromBucket(index, key, item);
                foreach (var key in afterKeys) addToBucket(index, key, item);
            }
        }

        // Checked before any index is touched, so a violation throws with every index still matching the item, whose
        // property has not been assigned yet.
        private static void RequireFree(Dictionary<string, T> index, PropInfo prop, string key, T item)
        {
            if (index.TryGetValue(key, out var owner) && !ReferenceEquals(owner, item))
                throw new ArgumentException("The key '" + key + "' is already in the unique index '" + prop.Name + "'.");
        }

        /// <summary>
        /// Removes <paramref name="item"/> from the bucket for <paramref name="key"/>, dropping the bucket once empty.
        /// </summary>
        private static void removeFromBucket(Dictionary<string, Deque<T>> index, string key, T item)
        {
            if (!index.TryGetValue(key, out var bucket)) return;

            var at = bucket.IndexOfReference(item);
            if (at < 0) return;

            bucket.RemoveAt(at);
            if (bucket.Count == 0)
                index.Remove(key);
        }

        private static void addToBucket(Dictionary<string, Deque<T>> index, string key, T item)
        {
            if (!index.TryGetValue(key, out var bucket))
            {
                bucket = new Deque<T>();
                index.Add(key, bucket);
            }

            bucket.Add(item);
        }

        /// <summary>
        /// Adds the value to all the relevant dictionaries (indexes).  Every unique key is checked before any index
        /// is written, so a violation leaves every index unchanged.
        /// </summary>
        /// <param name="val">The value to add.</param>
        /// <returns>True if the operation succeeded.</returns>
        private bool AddToDictionaries(T val)
        {
            if (!TryCollectUniqueKeys(val, null))
                return false;

            WriteToDictionaries(val);
            return true;
        }

        /// <summary>
        /// Gathers into <see cref="_uniqueKeys"/> the unique index keys <paramref name="val"/> would occupy, raising
        /// <see cref="IndexViolated"/> and returning false on the first key already held by another item.  Keys held by
        /// <paramref name="replacing"/> count as free, since that item is about to leave the indexes.
        /// </summary>
        private bool TryCollectUniqueKeys(T val, T? replacing)
        {
            _uniqueKeys.Clear();
            foreach (var prop in s_uniqueProps)
            {
                var index = _uniqueIndexes[prop.Slot];
                _keys.Clear();
                prop.AddKeys(prop.Get(val), _keys);

                // a key repeated within the item's own collection violates the index too
                _seenKeys.Clear();
                foreach (var key in _keys)
                {
                    if (isFree(index, key, replacing) && _seenKeys.Add(key))
                    {
                        _uniqueKeys.Add((index, key));
                        continue;
                    }

                    IndexViolated?.Invoke(this, new IndexViolationEventArgs<T>(val, "Value violated index '" + prop.Name + "' using key '" + key + "'"));
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
        /// Writes <paramref name="val"/> into every index, using the unique keys <see cref="TryCollectUniqueKeys"/> just
        /// checked.
        /// </summary>
        private void WriteToDictionaries(T val)
        {
            // assignment rather than Add: a key the replaced item still holds is overwritten instead of throwing
            foreach (var (index, key) in _uniqueKeys)
                index[key] = val;

            foreach (var prop in s_nonUniqueProps)
            {
                var index = _nonUniqueIndexes[prop.Slot];
                _keys.Clear();
                prop.AddKeys(prop.Get(val), _keys);
                foreach (var key in _keys)
                    addToBucket(index, key, val);
            }
        }

        /// <summary>
        /// Removes the value from all the relevant dictionaries (indexes)
        /// </summary>
        /// <param name="val">The value to remove</param>
        /// <returns>True if the operation succeeded.</returns>
        private bool RemoveFromDictionaries(T val)
        {
            foreach (var prop in s_uniqueProps)
            {
                var index = _uniqueIndexes[prop.Slot];
                _keys.Clear();
                prop.AddKeys(prop.Get(val), _keys);
                foreach (var key in _keys)
                {
                    if (index.Remove(key)) continue;

                    IndexViolated?.Invoke(this, new IndexViolationEventArgs<T>(val, "Value was not found in index '" + prop.Name + "' using key '" + key + "'"));
                    return false;
                }
            }

            foreach (var prop in s_nonUniqueProps)
            {
                var index = _nonUniqueIndexes[prop.Slot];
                _keys.Clear();
                prop.AddKeys(prop.Get(val), _keys);
                foreach (var key in _keys)
                    removeFromBucket(index, key, val);
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
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public void Clear()
        {
            foreach (var obj in _items) obj.DetachObserver(_token);

            _items.Clear();

            foreach (var index in _uniqueIndexes) index.Clear();
            foreach (var index in _nonUniqueIndexes) index.Clear();

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            return _items.IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public int IndexOf(T item)
        {
            return _items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ArgumentNullException.ThrowIfNull(item);

            // checked before the indexes are written so a bad position cannot leave the item half indexed
            if (index < 0 || index > _items.Count)
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
            _items.Insert(index, item);
            item.AttachObserver(_token);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get
            {
                return _items[index];
            }

            set
            {
                var item = _items[index];

                // check before removing the old item so a violation leaves every index exactly as it was
                // RemoveFromDictionaries only uses _keys, so the unique keys collected here survive it
                if (!TryCollectUniqueKeys(value, item))
                    throw new OperationCanceledException(IndexViolationMessage);

                RemoveFromDictionaries(item);
                WriteToDictionaries(value);

                item.DetachObserver(_token);
                _items[index] = value;
                value.AttachObserver(_token);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, item, index));
            }
        }

        #endregion Interface Implementations

        #region Linq

        public T First()
        {
            return _items.First();
        }

        public T First(Func<T, bool> predicate)
        {
            return _items.First(predicate);
        }

        public void Each(Action<T> action)
        {
            foreach (var item in _items)
                action(item);
        }

        #endregion Linq

        #region Private classes

        private sealed class PropInfo
        {
            public PropInfo(PropertyInfo info, int slot)
            {
                Name = info.Name;
                Slot = slot;
                IsUnique = IsUnique(info);
                IsCollection = info.PropertyType.HasInterface<ICollection>();
                IsDict = info.PropertyType.HasInterface<IDictionary>();
                Get = CreateGetter(info);
            }

            public string Name { get; }

            /// <summary>
            /// Position of this property's index in the collection's unique or non-unique index array.
            /// </summary>
            public int Slot { get; }

            public bool IsUnique { get; }

            public bool IsDict { get; }

            public bool IsCollection { get; }

            public Func<T, object?> Get { get; }

            /// <summary>
            /// Appends the index keys for a value of this property: one per element for a collection, one for anything
            /// else. Null values and null elements have no key. A non-unique dictionary is keyed by its values.
            /// </summary>
            public void AddKeys(object? value, List<string> keys)
            {
                if (value is null)
                    return;

                if (IsDict && !IsUnique && value is IDictionary dict)
                {
                    foreach (DictionaryEntry entry in dict)
                    {
                        if (KeyOf(entry.Value) is string k) keys.Add(k);
                    }

                    return;
                }

                if ((IsCollection || IsDict) && value is IEnumerable items)
                {
                    foreach (var item in items)
                    {
                        if (KeyOf(item) is string k) keys.Add(k);
                    }

                    return;
                }

                if (KeyOf(value) is string key) keys.Add(key);
            }

            private static Func<T, object?> CreateGetter(PropertyInfo info)
            {
                var getter = info.GetGetMethod(nonPublic: true);
                if (getter is null || getter.IsStatic)
                    return item => info.GetValue(item);

                var typed = typeof(PropInfo).GetMethod(nameof(CreateTypedGetter), BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException("CreateTypedGetter is missing.");
                return typed.MakeGenericMethod(info.PropertyType).Invoke(null, [getter]) as Func<T, object?>
                    ?? throw new InvalidOperationException("Could not create a getter for " + info.Name + ".");
            }

            // A delegate bound to the getter avoids PropertyInfo.GetValue's reflection cost on every read, and unlike
            // a compiled expression it needs no dynamic code, so it is fast on AOT platforms too.
            private static Func<T, object?> CreateTypedGetter<TProperty>(MethodInfo getter)
            {
                var get = (Func<T, TProperty>)Delegate.CreateDelegate(typeof(Func<T, TProperty>), getter);
                return item => get(item);
            }
        }

        /// <summary>
        /// A non-unique index seen as read-only: each bucket is exposed as a read-only list without copying.
        /// </summary>
        private sealed class BucketIndexView(Dictionary<string, Deque<T>> index) : IReadOnlyDictionary<string, IReadOnlyList<T>>
        {
            public IReadOnlyList<T> this[string key] => index[key];

            public IEnumerable<string> Keys => index.Keys;

            public IEnumerable<IReadOnlyList<T>> Values => index.Values;

            public int Count => index.Count;

            public bool ContainsKey(string key) => index.ContainsKey(key);

            // netstandard2.0 declares the out value non-null, so a miss gets an empty list rather than null
            public bool TryGetValue(string key, out IReadOnlyList<T> value)
            {
                if (index.TryGetValue(key, out var bucket))
                {
                    value = bucket;
                    return true;
                }

                value = Array.Empty<T>();
                return false;
            }

            public IEnumerator<KeyValuePair<string, IReadOnlyList<T>>> GetEnumerator()
            {
                foreach (var pair in index)
                    yield return new KeyValuePair<string, IReadOnlyList<T>>(pair.Key, pair.Value);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// The index key for a value. Strings are used as they are; other formattable values use the invariant culture
        /// so keys do not depend on the current culture. Writes and lookups both go through here.
        /// </summary>
        private static string? KeyOf(object? value)
        {
            return value switch
            {
                null => null,
                string s => s,
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString(),
            };
        }

        #endregion Private classes
    }
}