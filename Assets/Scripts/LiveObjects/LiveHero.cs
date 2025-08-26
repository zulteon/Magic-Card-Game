using UnityEngine;

public class LiveHero : MonoBehaviour,ILiveTarget
{
    public int Health { get; set; }
    public int Attack { get; set; }
    public bool valid {  get; set; }
    public Hero h;
    public int maxHealth;
    public ILiveTarget.Type type {  get; set; }
    public ILiveTarget.Race race{  get; set; }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        type = ILiveTarget.Type.hero;
        race=0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Init(Hero h)
    {
        this.h = h;
        Health = h.Health;
    }
    public void Damage(int dmg)
    {
        print("Hero is damaged"+dmg.ToString());
        Health -= dmg;
        if (Health < 0) Die();
    }
    public void Heal(int heal)
    {
        Health += heal;
        if (Health > maxHealth) Health = maxHealth;
    }
    public void Die()
    {

    }
}
