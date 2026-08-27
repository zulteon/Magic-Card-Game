// A "robusztus, minimalista" MinionView
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class MinionView : MonoBehaviour
{
    // Caching a referenciákat a "minimalista" elv szerint
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] protected TextMeshProUGUI healthText;
    [SerializeField] private SpriteRenderer spriteRenderer;
    protected LiveMinion _liveMinion;
    // A view fogadja az adatokat, nem kéri le őket!
    public virtual void Init(string sprite, short attack, ushort health)
    {
        if (spriteRenderer == null) transform.Find("Sprite").GetComponent<SpriteRenderer>();
        // Minimalista megoldás: a view megkapja az adatokat
        // a GameManager vagy egy másik View/Controller osztálytól.
        // spriteRenderer.sprite = ImageManager.GetImage(sprite);
        attackText.text = attack.ToString();
        healthText.text = health.ToString();

        _liveMinion = GetComponent<LiveMinion>();
        
        if (sprite != "")
        {
            if (sprite.Contains(".png"))
                sprite = sprite.Substring(0, sprite.Length - 4);
            print(" Load " + sprite);
            spriteRenderer.sprite = (Sprite)Resources.Load<Sprite>("Sprites/" + sprite);
        }
    }
    // ✨ Csak HP frissítése (ezt hívja az EffectClient)
    public void UpdateHealthVisual(int newHealth)
    {
        
        healthText.text = newHealth.ToString();
        // Opcionálisan: HP bar animáció
        // healthBar.fillAmount = (float)newHealth / maxHealth;
    }
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Escape)) 
        PlayDamageAnimation(3);
    }
    // ✨ Csak Attack frissítése
    public void UpdateAttackVisual(int newAttack)
    {
        attackText.text = newAttack.ToString();
    }

    // ✨ Régi metódus megtartása (backwards compatibility)
    public void UpdateStats(int? attack = null, int? health = null)
    {
        if (attack.HasValue)
            attackText.text = attack.Value.ToString();
        if (health.HasValue)
            healthText.text = health.Value.ToString();
    }

    // ✨ ÚJ: Damage animáció (rázás + floating text)
    public void PlayDamageAnimation(int damageAmount)
    {
        // Rázás effekt
        StartCoroutine(ShakeAnimation());

        // Floating damage szöveg ("-2")
        ShowFloatingText($"-{damageAmount}", Color.red);
    }

    // ✨ ÚJ: Heal animáció (zöld particle + floating text)
    public void PlayHealAnimation(int healAmount)
    {
        // Zöld particle effekt (ha van)
        // healParticle.Play();

        // Floating heal szöveg ("+3")
        ShowFloatingText($"+{healAmount}", Color.green);
    }

    // Helper: Rázás animáció
    private IEnumerator ShakeAnimation()
    {
        Vector3 originalPos = transform.localPosition;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-0.1f, 0.1f);
            float y = UnityEngine.Random.Range(-0.1f, 0.1f);
            transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }
    public IEnumerator PlayBuffAnimation(
    int newAttack,
    int newHealth,
    int buffValue)
    {
        UpdateAttackVisual(newAttack);
        UpdateHealthVisual(newHealth);

        ShowFloatingText(
            $"+{buffValue} BUFF!",
            Color.green
        );

        yield return BuffTextAnimation();
    }
    private IEnumerator BuffTextAnimation()
    {
        Vector3 attackOriginalScale = attackText.transform.localScale;
        Vector3 healthOriginalScale = healthText.transform.localScale;

        Color attackOriginalColor = attackText.color;
        Color healthOriginalColor = healthText.color;

        Color buffColor = new Color(0.3f, 1f, 0.35f);

        float growDuration = 0.15f;
        float shrinkDuration = 0.2f;

        float elapsed = 0f;

        while (elapsed < growDuration)
        {
            float t = elapsed / growDuration;

            float scale = Mathf.Lerp(1f, 1.45f, t);

            attackText.transform.localScale =
                attackOriginalScale * scale;

            healthText.transform.localScale =
                healthOriginalScale * scale;

            attackText.color =
                Color.Lerp(attackOriginalColor, buffColor, t);

            healthText.color =
                Color.Lerp(healthOriginalColor, buffColor, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < shrinkDuration)
        {
            float t = elapsed / shrinkDuration;

            float scale = Mathf.Lerp(1.45f, 1f, t);

            attackText.transform.localScale =
                attackOriginalScale * scale;

            healthText.transform.localScale =
                healthOriginalScale * scale;

            attackText.color =
                Color.Lerp(buffColor, attackOriginalColor, t);

            healthText.color =
                Color.Lerp(buffColor, healthOriginalColor, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        attackText.transform.localScale = attackOriginalScale;
        healthText.transform.localScale = healthOriginalScale;

        attackText.color = attackOriginalColor;
        healthText.color = healthOriginalColor;
    }
    // Helper: Floating text megjelenítése
    private void ShowFloatingText(string text, Color color)
    {
        // TODO: Implementáld a floating text rendszert
        // Például: TextMeshPro object spawn + animáció felfelé
        Debug.Log($"Floating text: {text}");
    }
    private void OnMouseDown()
    {
        if (_liveMinion == null)
            return;

        if (TargetSelector.instance.IsActive)
        {
            TargetSelector.instance.TryPick(_liveMinion.sequenceId);
            return;
        }

        _liveMinion.StartAttackClick();
    }
    private void OnMouseEnter()
    {
        /*MinionInspectView.Instance?.Show(
            minionCard,
            minionState
        );*/
    }

    private void OnMouseExit()
    {
        MinionInspectView.Instance?.Hide();
    }
    public virtual void SetTargetHighlight(bool on)
    {
        // pl. a SpriteRenderer színe, vagy egy kontúr GameObject
        spriteRenderer.color = on ? new Color(1f, 0.6f, 0.6f) : Color.white;
    }
    public GameObject tauntUI;

    public void TauntUI(bool b=true)
    {
        tauntUI.SetActive(b);
    }
    public Vector2Int GetStats()
    {
        Vector2Int vector2Int = new Vector2Int();
        vector2Int.x = Int16.Parse( attackText.text);
        vector2Int.y = Int16.Parse( healthText.text);
        return vector2Int;
    }
    public LiveMinion GetLiveMinion()
    {
        return _liveMinion;
    }
}