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