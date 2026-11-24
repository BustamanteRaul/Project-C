namespace MySharpVenture;

public enum AttributeType { Intelligence, Strength, Agility, TechnicalAbility, Cool }

public enum SkillEffect
{
    WeaponDamage, WeaponAccuracy, BasicDamage, BasicAccuracy, Healing, Experience
}

/// <summary>Habilidad de compra única con bonificación porcentual y dependencia opcional.</summary>
public sealed record SkillDefinition(string Code, string Name, AttributeType Attribute,
    int RequiredAttribute, string? Prerequisite, SkillEffect Effect, int Percent);

/// <summary>Balance provisional de progresión; el personaje no se guarda en SQLite.</summary>
public static class ProgressionContent
{
    public static string AttributeName(AttributeType attribute) => attribute switch
    {
        AttributeType.Intelligence => "Inteligencia",
        AttributeType.Strength => "Fuerza",
        AttributeType.Agility => "Agilidad",
        AttributeType.TechnicalAbility => "Habilidad técnica",
        AttributeType.Cool => "Cool",
        _ => throw new ArgumentOutOfRangeException(nameof(attribute))
    };

    public static string EffectName(SkillEffect effect) => effect switch
    {
        SkillEffect.WeaponDamage => "daño con arma",
        SkillEffect.WeaponAccuracy => "acierto con arma",
        SkillEffect.BasicDamage => "daño básico",
        SkillEffect.BasicAccuracy => "acierto básico",
        SkillEffect.Healing => "curación",
        SkillEffect.Experience => "experiencia",
        _ => throw new ArgumentOutOfRangeException(nameof(effect))
    };

    internal static string ExplainEffect(SkillEffect effect) => effect switch
    {
        SkillEffect.WeaponDamage => "Aumenta el daño de las armas equipadas al acertar.",
        SkillEffect.WeaponAccuracy => "Mejora la probabilidad de acertar con un arma equipada; no aumenta los críticos.",
        SkillEffect.BasicDamage => "Aumenta el daño del golpe básico y de los implantes de brazos.",
        SkillEffect.BasicAccuracy => "Mejora el acierto del golpe básico y de los implantes de brazos.",
        SkillEffect.Healing => "Aumenta la salud recuperada con consumibles, hasta tu máximo.",
        SkillEffect.Experience => "Aumenta la experiencia que recibís al resolver eventos de aventura.",
        _ => throw new ArgumentOutOfRangeException(nameof(effect))
    };

    public static IReadOnlyList<SkillDefinition> Skills { get; } = Array.AsReadOnly(new[]
    {
        new SkillDefinition("analisis", "Análisis", AttributeType.Intelligence, 1, null, SkillEffect.Experience, 10),
        new SkillDefinition("estrategia", "Estrategia", AttributeType.Intelligence, 3, "analisis", SkillEffect.WeaponAccuracy, 5),
        new SkillDefinition("medicina", "Medicina de campaña", AttributeType.Intelligence, 6, "estrategia", SkillEffect.Healing, 15),
        new SkillDefinition("punos", "Puños de acero", AttributeType.Strength, 1, null, SkillEffect.BasicDamage, 20),
        new SkillDefinition("potencia", "Potencia", AttributeType.Strength, 3, "punos", SkillEffect.WeaponDamage, 10),
        new SkillDefinition("resistencia", "Recuperación", AttributeType.Strength, 6, "potencia", SkillEffect.Healing, 20),
        new SkillDefinition("pulso", "Pulso firme", AttributeType.Agility, 1, null, SkillEffect.WeaponAccuracy, 5),
        new SkillDefinition("reflejos", "Reflejos", AttributeType.Agility, 3, "pulso", SkillEffect.BasicAccuracy, 5),
        new SkillDefinition("impulso", "Impulso", AttributeType.Agility, 6, "reflejos", SkillEffect.BasicDamage, 15),
        new SkillDefinition("mantenimiento", "Mantenimiento", AttributeType.TechnicalAbility, 1, null, SkillEffect.WeaponDamage, 10),
        new SkillDefinition("calibracion", "Calibración", AttributeType.TechnicalAbility, 3, "mantenimiento", SkillEffect.WeaponAccuracy, 5),
        new SkillDefinition("primeros-auxilios", "Primeros auxilios", AttributeType.TechnicalAbility, 6, "calibracion", SkillEffect.Healing, 15),
        new SkillDefinition("sangre-fria", "Sangre fría", AttributeType.Cool, 1, null, SkillEffect.WeaponAccuracy, 5),
        new SkillDefinition("golpe-certero", "Golpe certero", AttributeType.Cool, 3, "sangre-fria", SkillEffect.BasicAccuracy, 5),
        new SkillDefinition("oportunidad", "Oportunidad", AttributeType.Cool, 6, "golpe-certero", SkillEffect.Experience, 10)
    });

    public static SkillDefinition Get(string code) =>
        Skills.FirstOrDefault(skill => skill.Code == code)
        ?? throw new ArgumentException("Habilidad desconocida.", nameof(code));

    public static int ExperienceForLevel(int level)
    {
        if (level is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(level));
        return 50 * (level - 1) * level / 2;
    }
}
