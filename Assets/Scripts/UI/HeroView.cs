using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class HeroView : MinionView, IPointerClickHandler
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image frameImage;

    public ushort sequenceId;
    ushort heroId;
    void Awake()
    {
        _liveMinion=gameObject.GetComponentInChildren<LiveMinion>();
    }

    public void UpdateHealth(int health)
    {
        healthText.text = health.ToString();
    }
    public void Init(MinionState hero)
    {
        healthText.text=hero.currentHealth.ToString();
        _liveMinion=GetComponent<LiveMinion>();
        _liveMinion.InitFromMinionState(hero);
    }
    public void SwitchSides()
    {
        HeroView[] heroes =
            transform.parent.GetComponentsInChildren<HeroView>(true);

        if (heroes.Length != 2)
        {
            Debug.LogError(
                $"SwitchSides: 2 HeroView kellene, de {heroes.Length} van."
            );
            return;
        }

        HeroView other =
            heroes[0] == this
                ? heroes[1]
                : heroes[0];

        LiveMinion myMinion =
            GetComponent<LiveMinion>();

        LiveMinion otherMinion =
            other.GetComponent<LiveMinion>();

        if (myMinion == null || otherMinion == null)
            return;

        ushort temp = myMinion.sequenceId;

        myMinion.sequenceId =
            otherMinion.sequenceId;

        otherMinion.sequenceId =
            temp;
        HeroView tmp = GameManager.instance.homeHeroView;
        GameManager.instance.homeHeroView = GameManager.instance.enemyHeroView;
        GameManager.instance.enemyHeroView = tmp;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!TargetSelector.instance.IsActive) //ha az attackunk nagyobb mint 0
        {
            _liveMinion.StartAttackClick();
        }
        else
        {

            TargetSelector.instance.TryPick(_liveMinion.sequenceId);
        }
    }
    public override  void SetTargetHighlight(bool on)
    {

    }

}