using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using JMW.Collections;

namespace JMW.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(FastConfig))]
public class IndexedCollectionBenchmarks
{
    private sealed class FastConfig : ManualConfig
    {
        public FastConfig()
        {
            AddJob(Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance)
                .WithWarmupCount(3)
                .WithIterationCount(10));
        }
    }

    [Params(1_000, 10_000)]
    public int N;

    private Device[] items = [];
    private string[] ids = [];
    private string[] sites = [];
    private IndexedCollection<Device> collection = new();
    private bool toggle;

    [GlobalSetup]
    public void GlobalSetup()
    {
        ids = new string[N];
        sites = new string[N];
        for (var i = 0; i < N; i++)
        {
            ids[i] = Device.IdFor(i);
            sites[i] = Device.SiteFor(i);
        }

        collection = Build();
    }

    private IndexedCollection<Device> Build()
    {
        var c = new IndexedCollection<Device>();
        items = new Device[N];
        for (var i = 0; i < N; i++)
        {
            items[i] = Device.Create(i);
            c.Add(items[i]);
        }

        return c;
    }

    // The cost of creating the items alone, to subtract from Add.
    [Benchmark]
    public Device[] CreateItemsOnly()
    {
        var created = new Device[N];
        for (var i = 0; i < N; i++)
            created[i] = Device.Create(i);
        return created;
    }

    [Benchmark]
    public IndexedCollection<Device> Add()
    {
        var c = new IndexedCollection<Device>();
        for (var i = 0; i < N; i++)
            c.Add(Device.Create(i));
        return c;
    }

    [Benchmark]
    public int LookupUnique()
    {
        var found = 0;
        foreach (var id in ids)
        {
            if (collection.TryGetByUniqueIndex("Id", id, out _))
                found++;
        }

        return found;
    }

    [Benchmark]
    public int LookupUniqueByLambda()
    {
        var found = 0;
        foreach (var id in ids)
        {
            if (collection.TryGetByUniqueIndex(d => d.Id, id, out _))
                found++;
        }

        return found;
    }

    [Benchmark]
    public int LookupNonUnique()
    {
        var found = 0;
        foreach (var site in sites)
        {
            if (collection.TryGetByIndex("Site", site, out var bucket))
                found += bucket.Count;
        }

        return found;
    }

    [Benchmark]
    public int GetByIndex()
    {
        var found = 0;
        foreach (var site in sites)
            found += collection.GetByIndex("Site", site).Count;
        return found;
    }

    // Moves every item between two sites, so each set re-indexes one non-unique key.
    [Benchmark]
    public void UpdateIndexedProperty()
    {
        toggle = !toggle;
        var suffix = toggle ? "-a" : "-b";
        for (var i = 0; i < items.Length; i++)
            items[i].Site = sites[i] + suffix;
    }

    // Unindexed properties still raise the indexed-change event; the collection ignores them.
    [Benchmark]
    public void UpdateUnindexedProperty()
    {
        toggle = !toggle;
        var text = toggle ? "a" : "b";
        foreach (var item in items)
            item.Description = text;
    }

    [IterationSetup(Target = nameof(RemoveAll))]
    public void SetupRemoveAll() => collection = Build();

    [IterationCleanup(Target = nameof(RemoveAll))]
    public void CleanupRemoveAll() => collection = Build();

    [Benchmark]
    public void RemoveAll()
    {
        foreach (var item in items)
            collection.Remove(item);
    }

    [IterationSetup(Target = nameof(RemoveAllNewestFirst))]
    public void SetupRemoveAllNewestFirst() => collection = Build();

    [IterationCleanup(Target = nameof(RemoveAllNewestFirst))]
    public void CleanupRemoveAllNewestFirst() => collection = Build();

    // Removing from the end avoids shifting the list, so this isolates the index cost of Remove.
    [Benchmark]
    public void RemoveAllNewestFirst()
    {
        for (var i = items.Length - 1; i >= 0; i--)
            collection.Remove(items[i]);
    }
}
