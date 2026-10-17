using System;
using System.Diagnostics.CodeAnalysis;

namespace JMW.Collections;

/// <summary>
/// Receives indexed-property changes from the items of an <see cref="IndexedCollection{T}"/> without going through
/// a delegate subscription per item.
/// </summary>
internal interface IIndexObserver
{
    void OnIndexedPropertyChanging(IndexedClass sender, string propertyName, object? before, object? after);
}

/// <summary>
/// One per collection, shared by all of its items. Items hold the token strongly and the token holds the collection
/// weakly, so an item costs one reference and a dropped collection can still be collected.
/// </summary>
internal sealed class IndexObserverToken(IIndexObserver observer)
{
    private readonly WeakReference<IIndexObserver> observer = new(observer);

    public bool TryGetObserver([NotNullWhen(true)] out IIndexObserver? target) => observer.TryGetTarget(out target);
}
