/*
The MIT License (MIT)
Copyright (c) 2016 Jason Wall

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

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
        public int PropertyChangingEventCount => PropertyChanging?.GetInvocationList().Length ?? 0;

        /// <summary>
        /// The number of registered handlers for the event.
        /// </summary>
        public int PropertyChangedEventCount => PropertyChanged?.GetInvocationList().Length ?? 0;

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
            PropertyChanged = null;
        }

        /// <summary>
        /// Clears the PropertyChanging events.
        /// </summary>
        public void ClearPropertyChanging()
        {
            PropertyChanging = null;
        }

        #endregion Public Methods

        #region Events

        /// <summary>
        /// The <see cref="INotifyPropertyChanging"/> event.  Gives you the property name before
        /// the value is changed.
        /// </summary>
        public event PropertyChangingEventHandler? PropertyChanging;

        /// <summary>
        /// The <see cref="INotifyPropertyChanged"/> event.  Gives you the property name after
        /// the value has changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

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

            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(property_name));
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
            return true;
        }
    }
}
