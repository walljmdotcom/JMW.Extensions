using System.Collections.Generic;
using JMW.Collections;

namespace JMW.Benchmarks;

public sealed class Device : IndexedClass
{
    private string id = string.Empty;
    private string site = string.Empty;
    private int vlan;
    private List<string> tags = [];
    private string description = string.Empty;

    [Indexed(true)]
    public string Id { get => id; set => Set(ref id, value); }

    [Indexed]
    public string Site { get => site; set => Set(ref site, value); }

    [Indexed]
    public int Vlan { get => vlan; set => Set(ref vlan, value); }

    [Indexed]
    public List<string> Tags { get => tags; set => Set(ref tags, value); }

    public string Description { get => description; set => Set(ref description, value); }

    public static string IdFor(int i) => "device-" + i;

    public static string SiteFor(int i) => "site-" + (i % 50);

    public static Device Create(int i)
    {
        return new Device
        {
            Id = IdFor(i),
            Site = SiteFor(i),
            Vlan = i % 100,
            Tags = ["tag-" + (i % 20), "tag-" + ((i + 7) % 20), "tag-" + ((i + 13) % 20)],
            Description = "Device number " + i,
        };
    }
}
