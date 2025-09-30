using UnityEngine;
using FishNet.Serializing;
using FishNet.Managing;

public class SerializationTester : MonoBehaviour
{
    void Start()
    {
        TestCardState();
        TestMinionState();
    }

    void TestCardState()
    {
        CardState original = new CardState
        {
            cardId = 123,
            sequenceId = 456,
            currentCost = 5
        };

        // Szerializálás
        Writer writer = new Writer();
        CardStateSerializer.WriteCardState(writer, original);

        // Writer adatok átvétele
        byte[] data = writer.GetBuffer();
        int length = writer.Length;

        Debug.Log($"[CardState] Serialized bytes: {length}");

        // Deszerializálás
        Reader reader = new Reader();
        reader.Initialize(data, FindAnyObjectByType<NetworkManager>(),Reader.DataSource.Unset);
        CardState copy = CardStateSerializer.ReadCardState(reader);

        // Eredmény ellenőrzése
        Debug.Log($"[CardState] Equal: {original.Equals(copy)}");
        Debug.Log($"Original: ID={original.cardId}, Seq={original.sequenceId}, Cost={original.currentCost}");
        Debug.Log($"Copy:     ID={copy.cardId}, Seq={copy.sequenceId}, Cost={copy.currentCost}");
    }

    void TestMinionState()
    {
        // Eset 1: Minion effektekkel és támadási joggal
        MinionState original1 = new MinionState
        {
            cardId = 789,
            sequenceId = 101,
            currentHealth = 10,
            attack = 4,
            canAttack = true,
            activeEffects = new System.Collections.Generic.List<ushort>
        {
            (ushort)8,
            (ushort)3
        }
        };

        TestMinionStateHelper(original1);

        // Eset 2: Minion effektek és támadási jog nélkül
        MinionState original2 = new MinionState
        {
            cardId = 790,
            sequenceId = 102,
            currentHealth = 5,
            attack = 2,
            canAttack = false,
            activeEffects = null
        };

        TestMinionStateHelper(original2);
    }

    void TestMinionStateHelper(MinionState original)
    {
        // Szerializálás
        Writer writer = new Writer();
        MinionStateSerializer.WriteMinionState(writer, original);
        byte[] data = writer.GetBuffer();
        int length = writer.Length;
        Debug.Log($"[MinionState] Serialized bytes: {length}");

        // Deszerializálás
        Reader reader = new Reader();
        reader.Initialize(data, FindAnyObjectByType<NetworkManager>(), Reader.DataSource.Unset);
        MinionState copy = MinionStateSerializer.ReadMinionState(reader);

        // Eredmény ellenőrzése
        Debug.Log($"[MinionState] Equal: {original.Equals(copy)}");
        Debug.Log($"Original: ID={original.cardId}, Seq={original.sequenceId}, HP={original.currentHealth}, ATK={original.attack}, CanAttack={original.canAttack}, Effects={original.activeEffects?.Count ?? 0}");
        Debug.Log($"Copy:     ID={copy.cardId}, Seq={copy.sequenceId}, HP={copy.currentHealth}, ATK={copy.attack}, CanAttack={copy.canAttack}, Effects={copy.activeEffects?.Count ?? 0}");
    }
}