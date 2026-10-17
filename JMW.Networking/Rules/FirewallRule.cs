using JMW.Types;

namespace JMW.Networking.Rules;

public class FirewallRule
{
    public required Location Source { get; set; }
    public required Location Dest { get; set; }
    public required IntegerRange Port { get; set; }
    public Policy Policy { get; set; } = Policy.Deny;
}
