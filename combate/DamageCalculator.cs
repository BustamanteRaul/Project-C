namespace MySharpVenture;

public sealed class DamageCalculator(Random random)
{
    internal const int CriticalPercent = 10;
    internal const int OverweightCriticalPercent = 5;

    internal static int CriticalChance(Entity attacker) =>
        (attacker is Person { IsOverweight: true, Origin: not null } ? OverweightCriticalPercent : CriticalPercent)
        + ((attacker as Person)?.ImplantBonus(implant => implant.CriticalPoints) ?? 0);

    internal bool IsCritical(int percent) => random.Next(100) < percent;

    internal static int CriticalDamage(int damage) => (int)Math.Min(int.MaxValue, (long)damage * 3 / 2);

    public bool Hits(AttackParameters attack)
    {
        attack.Validate();
        return random.NextDouble() < (double)attack.Accuracy;
    }
}
