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
    public float YHeight = 3f;
    public float minusYheight = -3f;

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
        if(id<2) return id==0?GameManager.instance.homeHeroView.GetLiveMinion(): GameManager.instance.enemyHeroView.GetLiveMinion();
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
    public void ReturnMinionToHand(ushort sequenceId)
    {
        foreach (var list in new[] { home, abroad })
        {
            for (int i = 0; i < list.Count; i++)
            {
                var lm = list[i].GetComponent<LiveMinion>();
                if (lm == null || lm.sequenceId != sequenceId) continue;

                var go = list[i];
                list.RemoveAt(i);
                StartCoroutine(FlyToHandAndDestroy(go));
                Arrangecards();
                return;
            }
        }
        Debug.LogWarning($"[BoardManager] ReturnMinionToHand: {sequenceId} nincs a listában.");
    }

    private IEnumerator FlyToHandAndDestroy(GameObject go)
    {
        float t = 0f, dur = 0.4f;
        var tr = go.transform;
        Vector3 start = tr.position;
        Vector3 handPos = new Vector3(0, -5f, 0);   // ahol a kéz UI-ja van, igazítsd a sajátodhoz

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            tr.position = Vector3.Lerp(start, handPos, p);
            tr.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.3f, p);
            yield return null;
        }
        Destroy(go);
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
            .Init(def.sprite, minion.attack, minion.currentHealth);

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
    
    [SerializeField]
    float arcHeight = 1.2f;
    public int GetIndexOfMinion(GameObject go,bool enemy)
    {
        int index = 0;
        foreach (var i in enemy ? abroad : home)
        {
            if (i == go) return index;
            index++;
        }
        return -1;
    }
    public IEnumerator AnimateArcTo(ushort sequenceId, int newIndex)
    {
        var lm = GetLiveMinion(sequenceId);
        if (lm == null) yield break;

        var go = lm.gameObject;
        bool isEnemySide = !GameManager.instance.isAllyMinion(sequenceId);
        if (!GameManager.instance.AreWeHomePlayer()) isEnemySide = !isEnemySide;

        var board = isEnemySide ? abroad : home;
        int myIndex = GetIndexOfMinion(go, isEnemySide);

        if (myIndex == newIndex || myIndex > board.Count - 1 || newIndex > board.Count - 1)
        {
            RebuildSide(true);
            RebuildSide(false);
            yield break;
        }

        bool toLeft = myIndex > newIndex;

        float y = isEnemySide ? YHeight : minusYheight;
        Vector3 start = go.transform.position;
        Vector3 target = CalculatePositionAtIndex(newIndex, board.Count, y);
        float liftDirection = !isEnemySide ? -1f : 1f;
        Vector3 mid = new Vector3((start.x + target.x) / 2f, start.y + arcHeight * liftDirection, 0);

        float shiftAmount = margin + minionsize;

        Transform whomSwapWith = board[newIndex].transform;
        Vector3 tmpPosition = whomSwapWith.position;

        int minionsBetween = Mathf.Abs(myIndex - newIndex) - 1;
        Transform[] thingsToMove = new Transform[Mathf.Max(0, minionsBetween)];
        Vector3[] thingsToMovePositions = new Vector3[Mathf.Max(0, minionsBetween)];
        int ind = 0;

        Vector3 whomDirection = tmpPosition + new Vector3((toLeft ? shiftAmount : -shiftAmount), 0, 0);
        Vector3 whomSwapMidArc = new Vector3(
            (tmpPosition.x + whomDirection.x) / 2f,
            tmpPosition.y - liftDirection * arcHeight * 0.58f,   // ELLENTÉTES irány, kisebb ív
            0);

        if (minionsBetween > 0)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (toLeft && i > newIndex && i < myIndex)
                {
                    thingsToMove[ind] = board[i].transform;
                    thingsToMovePositions[ind] = board[i].transform.position;
                    ind++;
                }
                else if (!toLeft && i < newIndex && i > myIndex)
                {
                    thingsToMove[ind] = board[i].transform;
                    thingsToMovePositions[ind] = board[i].transform.position;
                    ind++;
                }
            }
        }

        float t = 0f, dur = 3.5f;
        float startDelay = 0.55f;
        
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            float delayedK = (t - startDelay * dur) / ((1f - startDelay) * dur);
            go.transform.position = QuadraticBezier(start, mid, target, k);
            if (delayedK > 0f)
            {
                whomSwapWith.position = QuadraticBezier(tmpPosition, whomSwapMidArc, whomDirection, delayedK);

                for (int i = 0; i < thingsToMove.Length; i++)
                {
                    thingsToMove[i].position = thingsToMovePositions[i] +
                         new Vector3((toLeft ? shiftAmount : -shiftAmount) * delayedK*delayedK, 0, 0);
                }
            }

            yield return null;
        }

        go.transform.position = target;
        RebuildSide(!isEnemySide);
        Arrangecards();
    }

    private Vector3 CalculatePositionAtIndex(int index, int count, float y)
    {
        float size = margin * (count - 1) + minionsize * count;
        float startingPoint = -size / 2 + minionsize / 2;
        float pos = startingPoint + index * (margin + minionsize);
        return new Vector3(pos, y, 0);
    }

    private Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        Vector3 ab = Vector3.Lerp(a, b, t);
        Vector3 bc = Vector3.Lerp(b, c, t);
        return Vector3.Lerp(ab, bc, t);
    }
}