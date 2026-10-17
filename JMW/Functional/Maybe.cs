/*
The MIT License (MIT)
Copyright (c) 2016 Jason Wall

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;

namespace JMW.Functional;

/// <summary>
/// The Maybe Class is an algebraic data type that allows you to safely work with nullable values and forces you to handle exceptions.
/// </summary>
/// <typeparam name="T">The type of value</typeparam>
public class Maybe<T> : IEquatable<T>
{
    private readonly T? internalValue;
    private readonly Exception? exception;

    /// <summary>
    /// Indicates if the conversion was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Constructor if the conversion failed.
    /// </summary>
    /// <param name="ex"></param>
    public Maybe(Exception? ex)
    {
        exception = ex;
        Success = false;
    }

    /// <summary>
    /// Constructor if the conversion succeeded.
    /// </summary>
    /// <param name="val"></param>
    public Maybe(T? val)
    {
        if (val is null)
        {
            Success = false;
            exception = new ArgumentNullException(nameof(val));
        }
        else
        {
            internalValue = val;
            Success = true;
        }
    }

    /// <summary>
    /// This function allows you to use the converted value.  Functions are not executed if they are null.
    /// </summary>
    /// <param name="ifSuccess">The Action to execute on the successfully converted value.</param>
    public void IfSuccess(Action<T> ifSuccess)
    {
        if (Success && internalValue is not null)
        {
            ifSuccess?.Invoke(internalValue);
        }
    }

    /// <summary>
    /// This function allows you to use the converted value.  Functions are not executed if they are null.
    /// </summary>
    /// <param name="ifException">The Action to execute on the Exception produced by the conversion.</param>
    public void IfException(Action<Exception> ifException)
    {
        if (!Success && exception is not null)
        {
            ifException?.Invoke(exception);
        }
    }

    /// <summary>
    /// This function allows you to use the converted value.  Functions are not executed if they are null.
    /// </summary>
    /// <param name="ifSuccess">The Action to execute on the successfully converted value.</param>
    public void Do(Action<T> ifSuccess)
    {
        IfSuccess(ifSuccess);
    }

    /// <summary>
    /// This function allows you to use the converted value.  Functions are not executed if they are null.
    /// </summary>
    /// <param name="ifSuccess">The Action to execute on the successfully converted value.</param>
    /// <param name="ifException">The Action to execute on the Exception produced by the conversion.</param>
    public void Do(Action<T>? ifSuccess, Action<Exception>? ifException)
    {
        if (Success && internalValue is not null)
        {
            ifSuccess?.Invoke(internalValue);
        }
        else if (exception is not null)
        {
            ifException?.Invoke(exception);
        }
    }

    /// <summary>
    /// This function allows you to use the converted value and return it.
    /// </summary>
    /// <param name="ifSuccess">A function that gets the converted value and allows you to return it.</param>
    /// <param name="ifException">A function that gets the exception and allows you to return a value of type T.</param>
    public T Do(Func<T, T>? ifSuccess, Func<Exception, T>? ifException)
    {
        if (Success && internalValue is not null)
        {
            return ifSuccess is null
                ? throw new ArgumentException("if_success parameter cannot be null.", nameof(ifSuccess))
                : ifSuccess(internalValue);
        }

        if (ifException is null)
        {
            throw new ArgumentException("if_exception parameter cannot be null.", nameof(ifException));
        }

        return exception is not null ? ifException(exception) : throw new InvalidOperationException();
    }

    /// <summary>
    /// This function allows you to use the converted value and return it.
    /// </summary>
    /// <param name="ifSuccess">A function that gets the converted value and allows you to return it.</param>
    /// <param name="ifException">A function that gets the exception and allows you to return a value of type T.</param>
    public Maybe<T> MaybeDo(Func<T, T> ifSuccess, Func<Exception, T> ifException)
    {
        if (Success && internalValue is not null)
        {
            return ifSuccess is null
                ? throw new ArgumentException("if_success parameter cannot be null.", nameof(ifSuccess))
                : new Maybe<T>(ifSuccess(internalValue));
        }

        if (ifException is null)
        {
            throw new ArgumentException("if_exception parameter cannot be null.", nameof(ifException));
        }

        return exception is not null ? new Maybe<T>(ifException(exception)) : throw new InvalidOperationException();
    }

    /// <summary>
    /// Does an equality comparison between the provided value of type <typeparamref name="T"/> and the internal value of the <see cref="Maybe{T}"/>.
    ///
    /// This function returns false if the <see cref="Maybe{T}"/> is not successful.
    /// </summary>
    /// <param name="other">Value to compare.</param>
    /// <returns>True if the <see cref="Maybe{T}"/> was successful and the value is equal to <paramref name="other"/></returns>
    public bool Equals(T? other)
    {
        return Success && internalValue is not null && internalValue.Equals(other);
    }

    /// <summary>
    /// Compares two <see cref="Maybe{T}"/> objects.
    /// </summary>
    /// <param name="other">Value to compare</param>
    /// <returns>Returns true if both <see cref="Maybe{T}"/> were successful and their values are equal.</returns>
    public bool Equals(Maybe<T> other)
    {
        var r = false;

        if (Success && internalValue is T value)
        {
            other.Do(ifSuccess: data => r = value.Equals(data), ifException: err => { });
        }

        return r;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Maybe<T>)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = internalValue is null ? 0 : EqualityComparer<T>.Default.GetHashCode(internalValue);
            hashCode = (hashCode * 397) ^ (exception?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ Success.GetHashCode();
            return hashCode;
        }
    }

    #region Operators

    /// <summary>
    ///   Operator overload.  Compares two <see cref="Maybe{T}"/> objects.
    /// </summary>
    /// <param name="left">Left side parameter</param>
    /// <param name="right">Right side parameter</param>
    /// <returns>Returns true if both <see cref="Maybe{T}"/> were successful and their values are equal, or if both are null.</returns>
    public static bool operator ==(Maybe<T?>? left, Maybe<T?>? right)
    {
        if (left is null && right is null)
            return true;
        return left is not null && right is not null && left.Equals(right);
    }

    /// <summary>
    /// Operator overload. Compares two <see cref="Maybe{T}"/> objects.
    /// </summary>
    /// <param name="left">Left side parameter</param>
    /// <param name="right">Right side parameter</param>
    /// <returns>Returns false if both <see cref="Maybe{T}"/> were successful and their values are equal, or if both are null.</returns>
    public static bool operator !=(Maybe<T?>? left, Maybe<T?>? right)
    {
        if (left is null && right is null)
            return false;
        return left is null || right is null || !(left == right);
    }

    /// <summary>
    ///   Operator overload.  Compares a <see cref="Maybe{T}"/> object and an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="left">Left side parameter</param>
    /// <param name="right">Right side parameter</param>
    /// <returns>Returns true if the <see cref="Maybe{T}"/> was successful and their values are equal, or if both are null.</returns>
    public static bool operator ==(Maybe<T?>? left, T? right)
    {
        if (left is null && right is null)
            return true;
        return left is not null && right is not null && left.Equals(right);
    }

    /// <summary>
    /// Operator overload. Compares a <see cref="Maybe{T}"/> object and an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="left">Left side parameter</param>
    /// <param name="right">Right side parameter</param>
    /// <returns>Returns false if the <see cref="Maybe{T}"/> was successful and their values are equal, or if both are null.</returns>
    public static bool operator !=(Maybe<T?>? left, T? right)
    {
        return !(left == right);
    }

    /// <summary>
    ///   Operator overload.  Compares a <see cref="Maybe{T}"/> object and an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="left">Left side parameter</param>
    /// <param name="right">Right side parameter</param>
    /// <returns>Returns true if the <see cref="Maybe{T}"/> was successful and their values are equal, or if both are null.</returns>
    public static bool operator ==(T? left, Maybe<T?>? right)
    {
        return right == left;
    }

    /// <summary>
    /// Operator overload. Compares a <see cref="Maybe{T}"/> object and an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="left">Left side parameter</param>
    /// <param name="right">Right side parameter</param>
    /// <returns>Returns false if the <see cref="Maybe{T}"/> was successful and their values are equal, or if both are null.</returns>
    public static bool operator !=(T? left, Maybe<T?>? right)
    {
        return !(right == left);
    }

    #endregion Operators
}