using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    List<GameObject> home = new List<GameObject>(), abroad = new List<GameObject>();

    float margin = 0.5f;
    float minionsize = 1.7f;
    float YHeight = 3f;
    float minusYheight = -3f;

    Transform boardMinions;
    public GameObject minionPrefab;
    public static BoardManager instance;

    // A Complete-hez tudni kell, MELYIK SyncList tartozik hozzánk.
    // A PlayerController állítja be, amikor feliratkozik (mert a szereptől függ).
    private bool _homeIsAlly = true;

    private void Awake()
    {
        instance = this;
        boardMinions = new GameObject("BoardMinions").transform;
        boardMinions.transform.parent = transform;
        new GameObject("Home").transform.parent = boardMinions;
        new GameObject("Abroad").transform.parent = boardMinions;
    }

    /// <summary>A PlayerController hívja, amikor kiderült, melyik oldalon áll.</summary>
    public void SetSide(bool homeIsAlly) => _homeIsAlly = homeIsAlly;
    public void AddToBoard(GameObject minion, bool toHome = true)
    {
        if (minion == null) print("whyy");
        if (toHome) home.Add(minion);
        else abroad.Add(minion);
        Arrangecards();
    }

    // ───────── A KÉT BELÉPÉSI PONT ─────────
    // Csak annyit csinálnak, hogy megmondják, melyik oldal.
    // A tényleges logika közös, hogy ne lehessen elgépelni.

    public void OnBoardChangeHome(SyncListOperation op, int index, MinionState oldItem, MinionState newItem, bool asServer)
        => HandleBoardChange(op, index, newItem, asServer, true);

    public void OnBoardChangeEnemy(SyncListOperation op, int index, MinionState oldItem, MinionState newItem, bool asServer)
        => HandleBoardChange(op, index, newItem, asServer, false);

    private void HandleBoardChange(SyncListOperation op, int index, MinionState newItem, bool asServer, bool isHome)
    {
        /*Debug.Log($"RemoveAt: index={op.ToString()}, sequenceId={newItem.sequenceId}, cardId={newItem.cardId}");
        if (asServer) return;

        List<GameObject> list = isHome ? home : abroad;

        switch (op)
        {
            case SyncListOperation.Add:
                list.Add(CreateMinionUI(newItem, isHome));
                break;

            case SyncListOperation.RemoveAt:
                if (index < list.Count)
                {
                    Destroy(list[index]);
                    list.RemoveAt(index);
                }
                Debug.Log($"RemoveAt: index={index}, sequenceId={newItem.sequenceId}, cardId={newItem.cardId}");
                break;

            case SyncListOperation.Clear:
                foreach (var go in list) Destroy(go);
                list.Clear();
                break;

           

            // Set: szándékosan nincs kezelve. A vizuális frissítés az
            // EffectClient sorából jön, különben a szerver előrébb járna
            // és animáció nélkül ugrana a szám.
            case SyncListOperation.Set:
                break;
        }

        Arrangecards();*/
    }

    private void RebuildSide(bool isHome)
    {
        List<GameObject> list = isHome ? home : abroad;

        foreach (var go in list) Destroy(go);
        list.Clear();

        var gm = GameManager.instance;
        if (gm == null) return;

        // melyik GameManager-lista tartozik ehhez az oldalhoz
        IList<MinionState> source = isHome
            ? (_homeIsAlly ? gm.boardAlly : gm.boardEnemy)
            : (_homeIsAlly ? gm.boardEnemy : gm.boardAlly);

        for (int i = 0; i < source.Count; i++)
            list.Add(CreateMinionUI(source[i], isHome));
    }

    public MinionView GetMinion(ushort id)
    {
        var lm = GetLiveMinion(id);
        return lm != null ? lm.GetComponent<MinionView>() : null;
    }
    public void SpawnMinion(MinionState state, bool isHome)
    {
        List<GameObject> list = isHome ? home : abroad;
        list.Add(CreateMinionUI(state, isHome));
        Arrangecards();          // ← enélkül (0,0,0)-ban marad
    }

    public void DestroyMinion(ushort sequenceId)
    {
        foreach (var list in new[] { home, abroad })
        {
            for (int i = 0; i < list.Count; i++)
            {
                var lm = list[i].GetComponent<LiveMinion>();
                if (lm == null || lm.sequenceId != sequenceId) continue;

                var go = list[i];
                list.RemoveAt(i);
                StartCoroutine(DieAndDestroy(go));
                Arrangecards();          // ← a többiek összezáródnak
                return;
            }
        }
        Debug.LogWarning($"[BoardManager] DestroyMinion: {sequenceId} nincs a listában.");
    }

    private IEnumerator DieAndDestroy(GameObject go)
    {
        float t = 0f, dur = 0.4f;
        var tr = go.transform;
        Vector3 start = tr.localScale;

        while (t < dur)
        {
            t += Time.deltaTime;
            tr.localScale = start * (1f - t / dur);
            tr.Rotate(0, 0, 720f * Time.deltaTime);
            yield return null;
        }
        Destroy(go);
    }
    public LiveMinion GetLiveMinion(ushort id)
    {
        foreach (Transform side in boardMinions)
        {
            foreach (Transform minionTransform in side)
            {
                LiveMinion lm = minionTransform.GetComponent<LiveMinion>();
                if (lm != null && lm.sequenceId == id)
                    return lm;
            }
        }

        Debug.LogWarning($"[GetLiveMinion] Nem találtam a(z) {id} ID-t a Home/Abroad csoportokban.");
        return null;
    }

    public GameObject CreateMinionUI(MinionState minion, bool home = true)
    {
        GameObject newMinion = Instantiate(minionPrefab, boardMinions.GetChild(home ? 0 : 1));
        newMinion.GetComponent<LiveMinion>().InitFromMinionState(minion);

        var def = CardManager.instance.GetMinion(minion.cardId);
        if (def == null)
        {
            Debug.LogError($"[CreateMinionUI] Nincs MinionCard ehhez: {minion.cardId}");
            return newMinion;
        }

        newMinion.GetComponent<MinionView>()
            .Initialize(def.sprite, minion.attack, minion.currentHealth);

        return newMinion;
    }

    void Arrangecards()
    {
        Layout(home, minusYheight);
        Layout(abroad, YHeight);
    }

    private void Layout(List<GameObject> list, float y)
    {
        int count = list.Count;
        if (count == 0) return;

        float size = margin * (count - 1) + minionsize * count;
        float startingPoint = -size / 2 + minionsize / 2;

        for (int i = 0; i < count; i++)
        {
            if (list[i] == null) continue;
            float pos = startingPoint + i * (margin + minionsize);
            list[i].transform.position = new Vector3(pos, y, 0);
        }
    }
}