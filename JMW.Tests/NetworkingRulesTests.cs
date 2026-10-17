using System.Collections.Generic;
using System.Net;
using JMW.Networking.Rules;
using JMW.Types;
using NUnit.Framework;

namespace JMW.Extensions.Tests
{
    [TestFixture]
    public class NetworkingRulesTests
    {
        [Test]
        public void TestAllowed1()
        {
            var r0 = new FirewallRule
            {
                Source = new Location { IsWildcard = true },
                Dest = new Location { IsWildcard = true },
                Port = new IntegerRange(0, 1000000000),
                Policy = Policy.Deny
            };
            var r1 = new FirewallRule
            {
                Source = new Location(IPAddress.Parse("192.168.1.1")),
                Dest = new Location(),
                Port = new IntegerRange(22, 33),
                Policy = Policy.Permit
            };

            var engine = new RulesEngine(new List<FirewallRule> { r0, r1 });

            Assert.That(engine.Allowed(new Location(IPAddress.Parse("192.168.1.1")), new Location(IPAddress.Parse("10.110.1.1")), new IntegerRange(22, 22)), Is.True);
            Assert.That(engine.Allowed(new Location(IPAddress.Parse("192.168.1.2")), new Location(IPAddress.Parse("10.110.1.1")), new IntegerRange(22, 22)), Is.False);
        }

        [Test]
        public void TestCompress1()
        {
            var r0 = new FirewallRule
            {
                Source = new Location { IsWildcard = true },
                Dest = new Location { IsWildcard = true },
                Port = new IntegerRange(0, 1000000000),
                Policy = Policy.Deny
            };

            var r1 = new FirewallRule
            {
                Source = new Location(IPAddress.Parse("192.168.1.1")),
                Dest = new Location(),
                Port = new IntegerRange(22, 33),
                Policy = Policy.Permit
            };

            var r2 = new FirewallRule
            {
                Source = new Location(IPAddress.Parse("192.168.1.1")),
                Dest = new Location(),
                Port = new IntegerRange(22, 23),
                Policy = Policy.Permit
            };

            var engine = new RulesEngine(new List<FirewallRule> { r0, r1, r2 });

            var lst = RulesEngine.Compress(new List<FirewallRule> { r0, r1, r2 });
        }

        private static readonly IPAddress HostA = IPAddress.Parse("192.168.1.1");
        private static readonly IPAddress HostB = IPAddress.Parse("192.168.1.2");
        private static readonly IPAddress HostX = IPAddress.Parse("10.0.0.1");
        private static readonly IPAddress HostY = IPAddress.Parse("10.0.0.2");

        private static FirewallRule Rule(Policy policy, IPAddress? source, IPAddress? dest, int start, int stop)
        {
            return new FirewallRule
            {
                Source = source is null ? new Location() : new Location(source),
                Dest = dest is null ? new Location() : new Location(dest),
                Port = new IntegerRange(start, stop),
                Policy = policy
            };
        }

        // Every concrete packet over a small universe that covers each address and each port boundary used below.
        private static IEnumerable<(Location Source, Location Dest, IntegerRange Port)> Packets()
        {
            foreach (var source in new[] { HostA, HostB })
            {
                foreach (var dest in new[] { HostX, HostY })
                {
                    for (var port = 0; port <= 5; port++)
                    {
                        yield return (new Location(source), new Location(dest), new IntegerRange(port, port));
                    }
                }
            }
        }

        private static void AssertSameDecisions(List<FirewallRule> original, List<FirewallRule> compressed)
        {
            var before = new RulesEngine(original);
            var after = new RulesEngine(compressed);
            foreach (var (source, dest, port) in Packets())
            {
                Assert.That(
                    after.Allowed(source, dest, port),
                    Is.EqualTo(before.Allowed(source, dest, port)),
                    $"{source.Address} -> {dest.Address}:{port.Start}"
                );
            }
        }

        private static IEnumerable<TestCaseData> CompressCases()
        {
            var denyAll = Rule(Policy.Deny, null, null, 0, 5);
            var permitA = Rule(Policy.Permit, HostA, null, 1, 4);
            var permitANarrow = Rule(Policy.Permit, HostA, null, 2, 3);
            var denyANarrow = Rule(Policy.Deny, HostA, null, 2, 3);
            var permitAll = Rule(Policy.Permit, null, null, 0, 5);

            yield return new TestCaseData(new List<FirewallRule>(), new List<FirewallRule>())
                .SetName("Compress_Empty");
            yield return new TestCaseData(new List<FirewallRule> { permitA }, new List<FirewallRule> { permitA })
                .SetName("Compress_LonePermitIsKept");
            yield return new TestCaseData(new List<FirewallRule> { denyAll }, new List<FirewallRule>())
                .SetName("Compress_LoneDenyMatchesDefault");
            yield return new TestCaseData(
                    new List<FirewallRule> { denyAll, permitA, permitANarrow },
                    new List<FirewallRule> { permitA })
                .SetName("Compress_ExistingTestCase");
            yield return new TestCaseData(
                    new List<FirewallRule> { permitA, permitA, permitA },
                    new List<FirewallRule> { permitA })
                .SetName("Compress_DuplicatePermitsCollapseToOne");
            yield return new TestCaseData(
                    new List<FirewallRule> { permitANarrow, permitA },
                    new List<FirewallRule> { permitA })
                .SetName("Compress_LaterWiderRuleShadowsEarlier");
            yield return new TestCaseData(
                    new List<FirewallRule> { permitA, denyANarrow },
                    new List<FirewallRule> { permitA, denyANarrow })
                .SetName("Compress_LaterNarrowDenyIsKept");
            yield return new TestCaseData(
                    new List<FirewallRule> { permitA, denyANarrow, permitANarrow },
                    new List<FirewallRule> { permitA })
                .SetName("Compress_PermitDenyPermitOnSameRange");
            yield return new TestCaseData(
                    new List<FirewallRule> { permitAll, denyAll },
                    new List<FirewallRule>())
                .SetName("Compress_TrailingDenyAllShadowsEverything");
            yield return new TestCaseData(
                    new List<FirewallRule> { denyAll, permitAll },
                    new List<FirewallRule> { permitAll })
                .SetName("Compress_TrailingPermitAllShadowsEverything");
        }

        [TestCaseSource(nameof(CompressCases))]
        public void TestCompress(List<FirewallRule> rules, List<FirewallRule> expected)
        {
            var compressed = RulesEngine.Compress(rules);

            Assert.That(compressed, Is.EqualTo(expected));
            AssertSameDecisions(rules, compressed);
        }

        // Every ordered list of up to three rules from a set that mixes wildcards, specific hosts, nested and
        // overlapping port ranges, and both policies, so any removal that changes a decision is caught.
        [Test]
        public void TestCompressPreservesEveryDecision()
        {
            var sources = new IPAddress?[] { null, HostA };
            var dests = new IPAddress?[] { null, HostX };
            var ports = new[] { (0, 5), (1, 3), (2, 2), (3, 4) };
            var pool = new List<FirewallRule>();
            foreach (var policy in new[] { Policy.Permit, Policy.Deny })
            {
                foreach (var source in sources)
                {
                    foreach (var dest in dests)
                    {
                        foreach (var (start, stop) in ports)
                        {
                            pool.Add(Rule(policy, source, dest, start, stop));
                        }
                    }
                }
            }

            foreach (var a in pool)
            {
                AssertSameDecisions([a], RulesEngine.Compress([a]));
                foreach (var b in pool)
                {
                    AssertSameDecisions([a, b], RulesEngine.Compress([a, b]));
                    foreach (var c in pool)
                    {
                        List<FirewallRule> rules = [a, b, c];
                        AssertSameDecisions(rules, RulesEngine.Compress(rules));
                    }
                }
            }
        }
    }
}