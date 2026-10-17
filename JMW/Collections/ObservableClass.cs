/*
The MIT License (MIT)
Copyright (c) 2016 Jason Wall

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using JMW.Extensions.Reflection;
using JMW.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JMW.Collections
{
    /// <summary>
    /// The purpose of the class is to implement the <see cref="INotifyPropertyChanged"/> and
    /// <see cref="INotifyPropertyChanging"/> interfaces and create helper methods for setting
    /// the value so that all the proper events are fired.
    /// </summary>
    public abstract class ObservableClass : INotifyPropertyChanged, INotifyPropertyChanging
    {
        #region Properties

        /// <summary>
        /// The number of registered handlers for the event.
        /// </summary>
        public int PropertyChangingEventCount => _PropertyChanging.HandlerCount;

        /// <summary>
        /// The number of registered handlers for the event.
        /// </summary>
        public int PropertyChangedEventCount => _PropertyChanged.HandlerCount;

        #endregion Properties

        #region Public Methods

        /// <summary>
        ///   Clears all the events attached to the object.
        ///
        /// <para>Specifically PropertyChanged and PropertyChanging</para>
        /// </summary>
        public virtual void ClearEvents()
        {
            ClearPropertyChanged();
            ClearPropertyChanging();
        }

        /// <summary>
        /// Clears the PropertyChanged events.
        /// </summary>
        public void ClearPropertyChanged()
        {
            _PropertyChanged.Clear();
        }

        /// <summary>
        /// Clears the PropertyChanging events.
        /// </summary>
        public void ClearPropertyChanging()
        {
            _PropertyChanging.Clear();
        }

        #endregion Public Methods

        #region Events

        private readonly WeakEventSource<PropertyChangingEventArgs> _PropertyChanging = new();
        /// <summary>
        /// The <see cref="INotifyPropertyChanging"/> event.  Gives you the property name before
        /// the value is changed.
        ///
        /// <para>Event references are weak.  Events only fire if the connected
        /// objects have not been collected by the GC.  <see cref="WeakReference"/>'s don't prevent
        /// the GC from collecting the objects if there are no more references to them.</para>
        /// </summary>
        public event PropertyChangingEventHandler? PropertyChanging
        {
            add { if (value is not null) _PropertyChanging.Subscribe(value.CastDelegate<EventHandler<PropertyChangingEventArgs>>()); }
            remove { if (value is not null) _PropertyChanging.Unsubscribe(value.CastDelegate<EventHandler<PropertyChangingEventArgs>>()); }
        }

        private readonly WeakEventSource<PropertyChangedEventArgs> _PropertyChanged = new();
        /// <summary>
        /// The <see cref="INotifyPropertyChanged"/> event.  Gives you the property name after
        /// the value has changed.
        /// <para>Event references are weak.  Events only fire if the connected
        /// objects have not been collected by the GC.  <see cref="WeakReference"/>'s don't prevent
        /// the GC from collecting the objects if there are no more references to them.</para>
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { if (value is not null) _PropertyChanged.Subscribe(value.CastDelegate<EventHandler<PropertyChangedEventArgs>>()); }
            remove { if (value is not null) _PropertyChanged.Unsubscribe(value.CastDelegate<EventHandler<PropertyChangedEventArgs>>()); }
        }

        #endregion Events

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
        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName]string property_name = "")
        {
            // if the field and the value are the same, don't bother.
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            RaisePropertyChanging(property_name);
            field = value;
            RaisePropertyChanged(property_name);
            return true;
        }

        #region Private Methods

        private void RaisePropertyChanged(string prop)
        {
            _PropertyChanged.Raise(this, new PropertyChangedEventArgs(prop));
        }

        private void RaisePropertyChanging(string prop)
        {
            _PropertyChanging.Raise(this, new PropertyChangingEventArgs(prop));
        }

        #endregion Private Methods
    }
}