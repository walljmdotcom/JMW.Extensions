/*
The MIT License (MIT)
Copyright (c) 2016 Jason Wall

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;

namespace JMW.Collections;

/// <summary>
/// Encapsulated information needed by the IndexedPropertyChanged event
/// </summary>
public class IndexedPropertyChangedEventArgs : EventArgs
{
    /// <summary>
    /// The Value of the object before it was changed
    /// </summary>
    public object? Before { get; }

    /// <summary>
    /// The Value of the object after it was changed
    /// </summary>
    public object? After { get; }

    /// <summary>
    /// The name of the property being changed
    /// </summary>
    public string PropertyName { get; }

    public IndexedPropertyChangedEventArgs(string prop_name, object? before, object? after)
    {
        Before = before;
        After = after;
        PropertyName = prop_name;
    }
}