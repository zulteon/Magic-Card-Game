public interface ILiveTarget
{
    bool valid {  get; set; }
    int Health { get; set; }
    void Damage(int amount);
    void Heal(int amount);
    bool isTaunt() { return false; }
    int Attack { get; set; }

    public enum Type { hero, minion }
    public enum Race:byte { none, Void, Mounts }
    public Type type { get; set; }
    public Race  race { get; set; }
}

