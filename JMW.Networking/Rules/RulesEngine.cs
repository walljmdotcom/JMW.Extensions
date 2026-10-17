using System.Collections.Generic;
using System.Linq;
using JMW.Types;

namespace JMW.Networking.Rules;

public class RulesEngine
{
    private readonly List<FirewallRule> rules;

    public RulesEngine(IEnumerable<FirewallRule> rules)
    {
        this.rules = rules.ToList();
    }

    /// <summary>
    /// Removes rules whose removal cannot change the decision <see cref="Allowed"/> makes for any single packet
    /// (one source address, one destination address, one port).
    /// </summary>
    /// <remarks>
    /// The check is conservative: a rule is kept unless a single other rule proves it redundant, so a rule that is
    /// only redundant because of several overlapping rules together survives.
    /// </remarks>
    public static List<FirewallRule> Compress(IEnumerable<FirewallRule> rules)
    {
        // Bottom-up, so removing a rule never shifts one that has not been checked yet. Removing a rule can make a
        // rule below it redundant, so passes repeat until one removes nothing.
        var lst = rules.ToList();

        bool removed;
        do
        {
            removed = false;
            for (var i = lst.Count - 1; i >= 0; i--)
            {
                if (IsShadowed(lst, i) || IsImpliedByEarlierRules(lst, i))
                {
                    lst.RemoveAt(i);
                    removed = true;
                }
            }
        } while (removed);
        return lst;
    }

    // A later rule matching every packet this rule matches always overrides it, whatever the policies.
    private static bool IsShadowed(List<FirewallRule> rules, int index)
    {
        var rule = rules[index];
        for (var j = index + 1; j < rules.Count; j++)
        {
            if (Covers(rules[j], rule))
            {
                return true;
            }
        }
        return false;
    }

    // Without this rule, every packet it matches would fall through to the earlier rules. Walking up, the decision
    // is unchanged if the first overlapping rule of a different policy comes after a rule of the same policy that
    // covers this one entirely, or after the top of the list when the policy is deny (the default).
    private static bool IsImpliedByEarlierRules(List<FirewallRule> rules, int index)
    {
        var rule = rules[index];
        for (var j = index - 1; j >= 0; j--)
        {
            var earlier = rules[j];
            if (!Overlaps(earlier, rule))
            {
                continue;
            }
            if (earlier.Policy != rule.Policy)
            {
                return false;
            }
            if (Covers(earlier, rule))
            {
                return true;
            }
        }
        return rule.Policy == Policy.Deny;
    }

    private static bool Overlaps(FirewallRule a, FirewallRule b)
    {
        return a.Source.IsMatch(b.Source) && a.Dest.IsMatch(b.Dest) && a.Port.IntersectsWith(b.Port);
    }

    private static bool Covers(FirewallRule outer, FirewallRule inner)
    {
        return Covers(outer.Source, inner.Source)
            && Covers(outer.Dest, inner.Dest)
            && outer.Port.Start <= inner.Port.Start
            && inner.Port.Stop <= outer.Port.Stop;
    }

    private static bool Covers(Location outer, Location inner)
    {
        return outer.IsWildcard
            || (!inner.IsWildcard && outer.Address is not null && outer.Address.Equals(inner.Address));
    }

    public bool Allowed(Location source, Location dest, IntegerRange port)
    {
        return Allowed(rules, source, dest, port);
    }

    private static bool Allowed(IEnumerable<FirewallRule> rules, Location source, Location dest, IntegerRange port)
    {
        var r = false;
        foreach (var rule in rules)
        {
            if (rule.Source.IsMatch(source)
                &&
                rule.Dest.IsMatch(dest)
                &&
                rule.Port.IntersectsWith(port))
            {
                r = rule.Policy != Policy.Deny;
            }
        }

        return r;
    }
}
