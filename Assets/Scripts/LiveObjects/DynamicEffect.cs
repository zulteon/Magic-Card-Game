public class DynamicEffect
{
    public ushort EffectId { get; private set; }
    public ushort Duration { get; private set; }
    public ushort Value { get; private set; }

    public DynamicEffect(ushort effectId, ushort value, ushort duration)
    {
        EffectId = effectId;
        Value = value;
        Duration = duration;
        
    }

    public void UpdateDynamic(ushort value = 65535, ushort duration = 65535)
    {
        // Csak akkor frissítjük az értéket, ha az különbözik a "nulla" értéktõl
        if (value != 65535)
            Value = value;

        if (duration != 65535)
            Duration = duration;
    }
}