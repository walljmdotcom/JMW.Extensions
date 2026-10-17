/*
The MIT License (MIT)
Copyright (c) 2016 Jason Wall

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JMW.Collections;

/// <summary>
/// <para>
/// This class serves as a base class for classes that can be indexed by <see cref="IndexedCollection{T}"/>.
/// </para>
///
/// <para>
/// The purpose of the class is to implement the <see cref="INotifyPropertyChanged"/> and
/// <see cref="INotifyPropertyChanging"/> interfaces, to implement an <see cref="IndexedPropertyChanged"/>
/// event and create helper methods for setting the value so that all the proper events are fired.
/// </para>
///
/// <para>This class is used in conjunction with the <see cref="IndexedCollection{T}"/> class to allow for an in
/// memory indexed lookup table.</para>
///
/// <para>The only caveat here is that you must add the <see cref="Indexed"/> attribute to the appropriate
/// property and use the Set method to handle setting props to trigger the event.</para>
/// </summary>
public abstract class IndexedClass : ObservableClass
{
    /// <summary>
    /// The signature of the <see cref="IndexedPropertyChangedHandler"/> used by the <see cref="IndexedPropertyChanged"/>
    /// event.
    /// </summary>
    /// <param name="sender">The object that called the event.</param>
    /// <param name="e">The <see cref="IndexedPropertyChangedEventArgs"/></param>
    public delegate void IndexedPropertyChangedHandler(object sender, IndexedPropertyChangedEventArgs e);

    #region Properties

    /// <summary>
    /// The number of registered handlers for the event.
    /// </summary>
    public int IndexedPropertyChangedEventCount => IndexedPropertyChanged?.GetInvocationList().Length ?? 0;

    #endregion Properties

    #region Public Methods

    /// <summary>
    ///   Clears all the events attached to the object.
    ///
    /// <para>Specifically IndexedPropertyChanged, PropertyChanged, and PropertyChanging</para>
    /// </summary>
    public override void ClearEvents()
    {
        IndexedPropertyChanged = null;
        base.ClearEvents();
    }

    /// <summary>
    /// Clears the IndexedPropertyChanged events.
    /// </summary>
    public void ClearIndexedPropertyChanged()
    {
        IndexedPropertyChanged = null;
    }

    #endregion Public Methods

    #region Events

    // The collections holding this item: null, one IndexObserverToken, or a List of them. One collection is the
    // common case, so it costs a single reference. Collections listen here rather than through the event.
    private object? _observers;

    /// <summary>
    /// Fires before a property changes.  This event will give you the before and after value of the
    /// property, as well as the property name.
    /// </summary>
    public event IndexedPropertyChangedHandler? IndexedPropertyChanged;

    #endregion Events

    /// <summary>
    /// Registers a collection to be told about indexed-property changes. Attaching the same token twice means two
    /// notifications per change, the same as subscribing an event handler twice.
    /// </summary>
    internal void AttachObserver(IndexObserverToken token)
    {
        switch (_observers)
        {
            case null:
                _observers = token;
                break;
            case IndexObserverToken single:
                _observers = new List<IndexObserverToken>(2) { single, token };
                break;
            case List<IndexObserverToken> list:
                list.Add(token);
                break;
        }
    }

    /// <summary>
    /// Removes one registration of <paramref name="token"/>.
    /// </summary>
    internal void DetachObserver(IndexObserverToken token)
    {
        switch (_observers)
        {
            case IndexObserverToken single when ReferenceEquals(single, token):
                _observers = null;
                break;
            case List<IndexObserverToken> list:
                var at = list.LastIndexOf(token);
                if (at >= 0) list.RemoveAt(at);
                if (list.Count == 1) _observers = list[0];
                else if (list.Count == 0) _observers = null;
                break;
        }
    }

    private void NotifyObservers(string propertyName, object? before, object? after)
    {
        switch (_observers)
        {
            case null:
                return;
            case IndexObserverToken single:
                if (single.TryGetObserver(out var observer))
                    observer.OnIndexedPropertyChanging(this, propertyName, before, after);
                else
                    _observers = null;
                return;
            case List<IndexObserverToken> list:
                // a snapshot, so an observer that detaches while handling the change does not skip the next one
                foreach (var token in list.ToArray())
                {
                    if (token.TryGetObserver(out var target))
                        target.OnIndexedPropertyChanging(this, propertyName, before, after);
                    else
                        DetachObserver(token);
                }

                return;
        }
    }

    /// <summary>
    /// Sets a value and fires the appropriate events.
    /// </summary>
    /// <typeparam name="T">The type of the Property</typeparam>
    /// <param name="field">A reference to the field being set.  This is usually the private backer of the property.</param>
    /// <param name="value">The value to set the field to.</param>
    /// <param name="property_name">The name of the Property being set.  This should be provided automatically
    /// using the <see cref="CallerMemberNameAttribute"/> via the magic of C# 6.
    /// </param>
    /// <returns>true/false indicating the success of the operation.  false if the field is null or the value is the same</returns>
    protected override bool Set<T>(ref T field, T value, [CallerMemberName]string property_name = "")
    {
        // if the value and the field are the same, then don't bother.
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        RaiseIndexedPropertyChanging(property_name, field, value);
        return base.Set(ref field, value, property_name);
    }

    #region Private Methods

    private void RaiseIndexedPropertyChanging(string prop_name, object? before, object? after)
    {
        NotifyObservers(prop_name, before, after);

        IndexedPropertyChanged?.Invoke(this, new IndexedPropertyChangedEventArgs(prop_name, before, after));
    }

    #endregion Private Methods
}