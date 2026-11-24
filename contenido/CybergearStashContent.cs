namespace MySharpVenture;

/// <summary>Identidad persistente de un implante por tier o un quickhack.</summary>
public sealed record CybergearStashDefinition(int CatalogItemId, string Code, string Name, int? Tier,
    CyberwareDefinition? Implant = null, CombatAction? Quickhack = null)
    : ItemDefinition(CatalogItemId, Code, Name, Tier);

internal static class CybergearStashContent
{
    internal static IReadOnlyList<CybergearStashDefinition> All { get; } =
    [
        .. Tiers(CyberwareContent.StartingDeck, 410),
        .. Tiers(CyberwareContent.ExDisk, 420),
        .. Tiers(CyberwareContent.SelfIce, 430),
        .. Tiers(CyberwareContent.Sandevistan, 440),
        Hack(451, "cortocircuito", QuickhackContent.ShortCircuit),
        Hack(452, "sobrecalentamiento", QuickhackContent.Overheat),
        Hack(453, "quemado-sinaptico", QuickhackContent.SynapseBurnout),
        Hack(454, "fallo-de-arma", QuickhackContent.WeaponGlitch),
        Hack(455, "fallo-de-cyberware", QuickhackContent.CyberwareMalfunction),
        Hack(456, "suicidio", QuickhackContent.Suicide),
        Armor,
        Arm(462, CyberwareContent.GorillaArms),
        Arm(463, CyberwareContent.MantisBlades),
        Arm(464, CyberwareContent.Monowire),
        Arm(465, CyberwareContent.ProjectileLaunchSystem),
        .. Tiers(CyberwareContent.SubdermalArmor, 470).Skip(1),
        .. CyberwareContent.ExtraOffers.Skip(4).SelectMany((offer, index) => Tiers(offer.Implant, 500 + 10 * index)),
        .. CyberwareContent.ExtraOffers.Take(4).SelectMany((offer, index) => Tiers(offer.Implant, 700 + 10 * index).Skip(1))
    ];

    internal static CybergearStashDefinition Armor => new(461, "subdermal-armor-t1",
        CyberwareContent.SubdermalArmor.Name, 1, CyberwareContent.SubdermalArmor);
    private static CybergearStashDefinition Arm(int id, CyberwareDefinition implant) =>
        new(id, $"{implant.Code}-t1", implant.Name, 1, implant);

    private static IEnumerable<CybergearStashDefinition> Tiers(CyberwareDefinition initial, int baseId)
    {
        for (CyberwareDefinition? implant = initial; implant is not null; implant = CyberwareContent.NextTier(implant))
            yield return new(baseId + implant.Tier, $"{implant.Code}-t{implant.Tier}", implant.Name, implant.Tier, implant);
    }

    private static CybergearStashDefinition Hack(int id, string code, QuickhackDefinition hack) =>
        new(id, code, hack.Name, null, Quickhack: hack.Action);
}
