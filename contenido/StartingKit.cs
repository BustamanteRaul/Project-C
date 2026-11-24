namespace MySharpVenture;

internal static class StartingKit
{
    internal const int HealingCount = 2;
    internal const int AttributePoints = 5;
    internal const int SkillPoints = 1;
    internal const CombatAction Quickhack = CombatAction.ShortCircuit;
    internal static string WeaponCode(OriginPath origin) => origin switch
    {
        OriginPath.Corpo => "pistola-t1",
        OriginPath.Street => "subfusil-t1",
        OriginPath.Nomad => "escopeta-corredera-t1",
        _ => throw new ArgumentOutOfRangeException(nameof(origin), "Origen inválido.")
    };

    internal static string SkillCode(OriginPath origin) => origin switch
    {
        OriginPath.Corpo => "analisis",
        OriginPath.Street => "sangre-fria",
        OriginPath.Nomad => "punos",
        _ => throw new ArgumentOutOfRangeException(nameof(origin), "Origen inválido.")
    };
}
