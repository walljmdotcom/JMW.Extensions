/*
The MIT License (MIT)
Copyright (c) 2016 Jason Wall

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JMW.Extensions;

public static class ReflectionExtensions
{
    /// <summary>
    /// Converts a delegate to another delegate type with a compatible signature. Every handler in a multicast
    /// delegate is converted, in order.
    /// </summary>
    public static T CastDelegate<T>(this Delegate src) where T : class
    {
        var parts = src.GetInvocationList();
        var cast = new Delegate?[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            // throwOnBindFailure: true means CreateDelegate throws rather than returning null.
            cast[i] = Delegate.CreateDelegate(typeof(T), parts[i].Target, parts[i].Method, true);
        }

        return Delegate.Combine(cast) as T ?? throw new InvalidOperationException("The delegate could not be cast.");
    }

    /// <summary>
    /// Gets the list of <see cref="PropertyInfo"/> that have and attribute of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The Attribute Type you are filtering on</typeparam>
    /// <param name="type">The type to search for properties </param>
    /// <returns><see cref="IEnumerable{PropertyInfo}"/> that have an Attribute of type <typeparamref name="T"/></returns>
    public static IEnumerable<PropertyInfo> GetPropertiesByAttribute<T>(this Type type) where T : Attribute
    {
        return type.GetProperties()
                   .Where(p => Attribute.IsDefined(p, typeof(T)));
    }

    /// <summary>
    /// Indicates if the provided type implements the Interface of <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">Type of Interface to check for</typeparam>
    /// <param name="type">The type to check</param>
    /// <returns>Boolean indicated if the type implments the interface.</returns>
    public static bool HasInterface<T>(this Type type) where T : class
    {
        var t_name = typeof(T).Name;

        return type.GetInterfaces().Any(t => t.Name == t_name);
    }

    public static T? GetValue<T>(this PropertyInfo info, object obj)
    {
        return (T?)info.GetValue(obj, null);
    }

    /// <summary>
    /// Get the attribute of the specified type from an enum value.
    /// </summary>
    /// <typeparam name="TAttribute">The type of the attribute to get.</typeparam>
    /// <param name="enumVal">The enum value.</param>
    /// <returns>The first attribute of the given type that exists on the enum value.</returns>
    public static TAttribute? GetAttributeOfType<TAttribute>(this Enum enumVal)
        where TAttribute : Attribute
    {
        var memInfo = enumVal.GetType().GetMember(enumVal.ToString());
        return memInfo[0].GetAttributeOfType<TAttribute>();
    }

    /// <summary>
    /// Get the attribute of the specified type from the given <paramref name="provider"/>.
    /// </summary>
    /// <typeparam name="TAttribute">The type of the attribute to get.</typeparam>
    /// <param name="provider">The provider.</param>
    /// <returns>The first attribute of the given type that exists on the provider.</returns>
    public static TAttribute? GetAttributeOfType<TAttribute>(this ICustomAttributeProvider provider)
        where TAttribute : Attribute
    {
        var attributes = provider.GetCustomAttributes(typeof(TAttribute), true);
        return attributes.Length > 0 ? attributes[0] as TAttribute : null;
    }
}