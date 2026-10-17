using JMW.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JMW;

public static class Differential
{
    /// <summary>
    ///   Takes two indexed collections and performs a comparison.
    /// </summary>
    /// <typeparam name="T">The type of object being compared</typeparam>
    /// <typeparam name="M">The type of the object storing the difference.</typeparam>
    /// <param name="lstA">List A</param>
    /// <param name="lstB">List B</param>
    /// <param name="retriever">A function that takes an object and list of objects and returns an object from the list that matches the supplied object or returns null if no matching object exists in the supplied list.</param>
    /// <param name="comparer">A function that compares two objects found in both lists and returns an <seealso cref="ObjectDifferential{T, M}"/></param>
    /// <returns></returns>
    public static CollectionDifferential<T, M> RunCollectionDifferential<T, M>(
        IList<T> lstA,
        IList<T> lstB,
        Func<T, IList<T>, T?> retriever,
        Func<T, T, ObjectDifferential<T, M>?> comparer)
    {
        // some objects to hold things.
        var diff = new CollectionDifferential<T, M>();
        var same = new List<Tuple<T, T>>();

        // loop through all the objects in lstA and see if there is an equivalent object in lstB
        for (int i = 0; i < lstA.Count; i++)
        {
            var a_item = lstA[i];
            var b_item = retriever(a_item, lstB);
            if (b_item is not null)
            {
                // you found an object, so store it and its equivalent form lstB in the "same" collection
                same.Add(new Tuple<T, T>(a_item, b_item));
            }
            else // no object found, it exists only in lstA
                diff.OnlyInListA.Add(a_item);
        }

        // loop through all the objects in lstB and see if there is an equivalent in lstA
        for (int i = 0; i < lstB.Count; i++)
        {
            var b_item = lstB[i];
            var a_item = retriever(b_item, lstA);

            // you only store the ones that fail because you've already stored the items that are the same
            //   when you iterated through lstA
            if (a_item is null)
                diff.OnlyInListB.Add(b_item);
        }

        // loop an figure the differences between each object that exists in both.
        foreach (var t in same)
        {
            // for the items that are both in the list, compare to see if they are different.
            var item = comparer(t.Item1, t.Item2);
            if (item is not null)
                diff.Modified.Add(item);
        }

        return diff;
    }

    /// <summary>
    ///   Takes two collections and performs a comparison.  Returns an object showing whats only in lst a, only in lst b, whats in both, and what has changed
    /// </summary>
    /// <typeparam name="T">The type of object being compared</typeparam>
    /// <typeparam name="M">The type of the object storing the difference.</typeparam>
    /// <typeparam name="TKey">The type of the value being compared</typeparam>
    /// <param name="lst_a">List A</param>
    /// <param name="lst_b">List B</param>
    /// <param name="key_selector">A property selector that is used to get the name of the property used to compare against.</param>
    /// <param name="object_differ">A function that compares two objects found in both lists and returns an <seealso cref="ObjectDifferential{T, M}"/></param>
    public static CollectionDifferential<T, M> RunCollectionDifferential<T, M, TKey>(
        IEnumerable<T> lst_a,
        IEnumerable<T> lst_b,
        Func<T, TKey> key_selector,
        Func<T, T, ObjectDifferential<T, M>?> object_differ)
    {
        var diff = new CollectionDifferential<T, M>();
        mergeByKey(
            lst_a,
            lst_b,
            key_selector,
            key_selector,
            diff.OnlyInListA,
            diff.OnlyInListB,
            (a, b) => diff.Shared.Add(new Pair<T>(a, b))
        );

        // loop an figure the differences between each object that exists in both.
        foreach (var item in diff.Shared)
        {
            // for the items that are both in the list, compare to see if they are different.
            var d = object_differ(item.A, item.B);
            if (d is not null)
                diff.Modified.Add(d);
        }

        return diff;
    }

    /// <summary>
    ///   Takes two indexed collections and performs a comparison.  Returns an object showing whats only in lst a, only in lst b, and whats in both
    /// </summary>
    /// <typeparam name="Ta">The type of object being compared</typeparam>
    /// <typeparam name="Tb">The type of object being compared</typeparam>
    /// <param name="lst_a">List A</param>
    /// <param name="lst_b">List B</param>
    /// <param name="retriever_a">A function that takes an object and list of objects and returns an object from the list that matches the supplied object or returns null if no matching object exists in the supplied list.</param>
    /// <param name="retriever_b">A function that takes an object and list of objects and returns an object from the list that matches the supplied object or returns null if no matching object exists in the supplied list.</param>
    public static CollectionDifferentialSimple<Ta, Tb> RunCollectionDifferential<Ta, Tb>(
        IEnumerable<Ta> lst_a,
        IEnumerable<Tb> lst_b,
        Func<Ta, IEnumerable<Tb>, Tb?> retriever_a,
        Func<Tb, IEnumerable<Ta>, Ta?> retriever_b)
    {
        // some objects to hold things.
        var diff = new CollectionDifferentialSimple<Ta, Tb>();

        // loop through all the objects in lstA and see if there is an equivalent object in lstB
        foreach (var a_item in lst_a)
        {
            var b_item = retriever_a(a_item, lst_b);
            if (b_item is not null) // you found an object, so store it and its equivalent form lstB in the "same" collection
                diff.Shared.Add(new Pair<Ta, Tb>(a_item, b_item));
            else // no object found, it exists only in lstA
                diff.OnlyInListA.Add(a_item);
        }

        // loop through all the objects in lstB and see if there is an equivalent in lstA
        foreach (var b_item in lst_b)
        {
            var a_item = retriever_b(b_item, lst_a);

            // you only store the ones that fail because you've already stored the items that are the same
            //   when you iterated through lstA
            if (a_item is null)
                diff.OnlyInListB.Add(b_item);
        }

        return diff;
    }

    /// <summary>
    ///   Takes two indexed collections and performs a comparison.  Returns an object showing whats only in lst a, only in lst b, and whats in both
    /// </summary>
    /// <typeparam name="Ta">The type of object in list a being compared</typeparam>
    /// <typeparam name="TKey">The type of the value being compared returned by the key_selector</typeparam>
    /// <typeparam name="Tb">The type of the object in list b being compared</typeparam>
    /// <param name="lst_a">List A</param>
    /// <param name="lst_b">List B</param>
    /// <param name="key_selector_a">An expression that is used to identify the property to compare the lists with.</param>
    /// <param name="key_selector_b">An expression that is used to identify the property to compare the lists with.</param>
    public static CollectionDifferentialSimple<Ta, Tb> RunCollectionDifferential<Ta, Tb, TKey>(
        IEnumerable<Ta> lst_a,
        IEnumerable<Tb> lst_b,
        Func<Ta, TKey> key_selector_a,
        Func<Tb, TKey> key_selector_b)
    {
        var diff = new CollectionDifferentialSimple<Ta, Tb>();
        mergeByKey(
            lst_a,
            lst_b,
            key_selector_a,
            key_selector_b,
            diff.OnlyInListA,
            diff.OnlyInListB,
            (a, b) => diff.Shared.Add(new Pair<Ta, Tb>(a, b))
        );

        return diff;
    }

    /// <summary>
    ///   Takes two indexed collections and performs a comparison.  Returns an object showing whats only in lst a, only in lst b, and whats in both
    /// </summary>
    /// <typeparam name="T">The type of object being compared</typeparam>
    /// <typeparam name="TKey">The type of the value being compared returned by the key_selector</typeparam>
    /// <param name="lst_a">List A</param>
    /// <param name="lst_b">List B</param>
    /// <param name="key_selector">An expression that is used to identify the property to compare the lists with.</param>
    public static CollectionDifferentialSimple<T> RunCollectionDifferential<T, TKey>(
        IEnumerable<T> lst_a,
        IEnumerable<T> lst_b,
        Func<T, TKey> key_selector)
    {
        var diff = new CollectionDifferentialSimple<T>();
        mergeByKey(
            lst_a,
            lst_b,
            key_selector,
            key_selector,
            diff.OnlyInListA,
            diff.OnlyInListB,
            (a, _) => diff.Shared.Add(a)
        );

        return diff;
    }

    /// <summary>
    /// Sorts both lists by key and walks them together, reporting each item as only in A, only in B, or shared.
    /// Duplicate keys pair off one to one; any surplus is reported as only in its own list.
    /// </summary>
    private static void mergeByKey<Ta, Tb, TKey>(
        IEnumerable<Ta> lst_a,
        IEnumerable<Tb> lst_b,
        Func<Ta, TKey> key_selector_a,
        Func<Tb, TKey> key_selector_b,
        List<Ta> onlyInA,
        List<Tb> onlyInB,
        Action<Ta, Tb> shared)
    {
        var comparer = Comparer<TKey>.Default;

        var sorted_a = lst_a.OrderBy(key_selector_a, comparer).ToList();
        var sorted_b = lst_b.OrderBy(key_selector_b, comparer).ToList();

        var i = 0;
        var j = 0;
        while (i < sorted_a.Count && j < sorted_b.Count)
        {
            var order = comparer.Compare(key_selector_a(sorted_a[i]), key_selector_b(sorted_b[j]));
            if (order == 0)
            {
                shared(sorted_a[i], sorted_b[j]);
                i++;
                j++;
            }
            else if (order < 0)
            {
                onlyInA.Add(sorted_a[i++]);
            }
            else
            {
                onlyInB.Add(sorted_b[j++]);
            }
        }

        for (; i < sorted_a.Count; i++)
            onlyInA.Add(sorted_a[i]);

        for (; j < sorted_b.Count; j++)
            onlyInB.Add(sorted_b[j]);
    }
}

/// <summary>
///  A Class that holds the differences between two lists of objects.
/// </summary>
/// <typeparam name="Ta">The first type of object being compared</typeparam>
/// <typeparam name="Tb">The second type of object being compared.</typeparam>
public class CollectionDifferentialSimple<Ta, Tb>
{
    public bool HasDifferences => OnlyInListA.Count > 0 || OnlyInListB.Count > 0;

    public bool HasDeletions => OnlyInListA.Count > 0;
    public bool HasAdditions => OnlyInListB.Count > 0;

    /// <summary>
    /// The objects that were added
    /// </summary>
    public List<Ta> Removals => OnlyInListA;

    /// <summary>
    /// The objects that exist only in List A
    /// </summary>
    public List<Ta> OnlyInListA { get; } = [];

    /// <summary>
    /// The objects that were added
    /// </summary>
    public List<Tb> Additions => OnlyInListB;

    /// <summary>
    /// The objects that exist only in List B
    /// </summary>
    public List<Tb> OnlyInListB { get; } = [];

    /// <summary>
    /// The objects that exist in both lists
    /// </summary>
    public List<Pair<Ta, Tb>> Shared { get; } = [];
}

/// <summary>
///  A Class that holds the differences between two lists of objects.
/// </summary>
/// <typeparam name="T">The type of object being compared</typeparam>
public class CollectionDifferentialSimple<T>
{
    public bool HasDifferences => OnlyInListA.Count > 0 || OnlyInListB.Count > 0;

    public bool HasDeletions => OnlyInListA.Count > 0;
    public bool HasAdditions => OnlyInListB.Count > 0;

    /// <summary>
    /// The objects that were added
    /// </summary>
    public List<T> Removals => OnlyInListA;

    /// <summary>
    /// The objects that exist only in List A
    /// </summary>
    public List<T> OnlyInListA { get; } = [];

    /// <summary>
    /// The objects that were added
    /// </summary>
    public List<T> Additions => OnlyInListB;

    /// <summary>
    /// The objects that exist only in List B
    /// </summary>
    public List<T> OnlyInListB { get; } = [];

    /// <summary>
    /// The objects that exist in both lists
    /// </summary>
    public List<T> Shared { get; } = [];
}

/// <summary>
///  A Class that holds the differences between two lists of objects.
/// </summary>
/// <typeparam name="T">The type of object being compared</typeparam>
/// <typeparam name="M">The type of the object storing the difference.</typeparam>
public class CollectionDifferential<T, M>
{
    public bool HasDifferences => OnlyInListA.Count > 0 || OnlyInListB.Count > 0 || Modified.Count > 0;

    public bool HasDeletions => OnlyInListA.Count > 0;
    public bool HasAdditions => OnlyInListB.Count > 0;
    public bool HasModifications => Modified.Count > 0;

    /// <summary>
    /// The objects that were added
    /// </summary>
    public List<T> Deletions => OnlyInListA;

    /// <summary>
    /// The objects that exist only in List A
    /// </summary>
    public List<T> OnlyInListA { get; } = [];

    /// <summary>
    /// The objects that were added
    /// </summary>
    public List<T> Additions => OnlyInListB;

    /// <summary>
    /// The objects that exist only in List B
    /// </summary>
    public List<T> OnlyInListB { get; } = [];

    /// <summary>
    /// The objects that exist in both lists
    /// </summary>
    public List<Pair<T>> Shared { get; } = [];

    /// <summary>
    /// The objects that exist in both List A and List B, and how they are different.
    /// </summary>
    public List<ObjectDifferential<T, M>> Modified { get; } = [];
}

/// <summary>
/// Holds the differences between two objects.
/// </summary>
/// <typeparam name="T">The type of object being compared</typeparam>
/// <typeparam name="M">The type of the object storing the difference.</typeparam>
public class ObjectDifferential<T, M>
{
    public ObjectDifferential(T objA, T objB, List<M> diffs)
    {
        ObjectA = objA;
        ObjectB = objB;
        Differences = diffs;
    }

    public T ObjectA { get; private set; }
    public T ObjectB { get; private set; }
    public List<M> Differences { get; private set; }
}