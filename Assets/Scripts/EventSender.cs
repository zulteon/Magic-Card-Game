using FishNet.Object;
using UnityEngine;

public class EventSender : NetworkBehaviour
{
    public static EventSender Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /* ====== Summon ====== */
    [Server]
    public void SendSummon(string name, int atk, int hp)
    {
        RpcSummon(name, atk, hp);
    }

    [ObserversRpc]
    private void RpcSummon(string name, int atk, int hp)
    {
      //  GameEvents.Instance.RaiseMinionSummoned(name, atk, hp);
    }

    /* ====== Death ====== */
    [Server]
    public void SendDeath(string name)
    {
        RpcDeath(name);
    }

    [ObserversRpc]
    private void RpcDeath(string name)
    {
      //  GameEvents.Instance.RaiseMinionDied(name);
    }

    /* ====== OnPlay ====== */
    [Server]
    public void SendOnPlay(string name)
    {
        //RpcOnPlay(name);
    }

    [ObserversRpc]
    private void RpcOnPlay(string name)
    {
        //GameEvents.Instance.RaiseMinionPlayed(name);
    }
}
/*Javasolt felosztás
Lokálisan kezelhető (SyncList változás alapján)
Summon

Új elem kerül a board SyncList-be → ebből tudjuk, hogy megidéződött.

Animáció, UI trigger lokálisan.

Buff/Debuff

Ha buff/debuff csak egy külön effektlista (nem az eredeti statot módosítja), és ez a lista kliensen is megvan, akkor lokálban lehet animálni és mutatni.

Damage

Ha a sebzés hatására a HP SyncVar frissül, kliens UI reagálhat anélkül, hogy külön szerverüzenet menne.

Szerver által küldendő (ObserversRpc)
Death

Mert a kliens nem tudja, hogy a minion eltűnése halál, shuffle back, vagy más logikai ok miatt történt.

Kell, hogy egyértelmű trigger legyen animációra/effectre.

Shuffle / Visszakerülés a pakliba

A kliens nem tudja kitalálni a célhelyet és animációt csak a listaváltozásból.

Speciális effektek

Pl. “Mind Control”, “Transform” → több változás egyszerre, kliensnek kontextus kell.

Complex Buff/Debuff

Ha a buff logikája bonyolult, vagy nem csak egy számváltozás, hanem pl. képességet ad/vesz el.*/