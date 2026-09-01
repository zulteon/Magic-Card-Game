using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlayerController;

public static class EffectCommands
{
    public static Dictionary<Effect.Type, Action<EffectContext>> Registry = new()
    {
        { Effect.Type.damage, Damage },
        { Effect.Type.heal, Heal },
        { Effect.Type.give, Give },
        { Effect.Type.buff, Buff },
        {Effect.Type.attack,Attack },
        {Effect.Type.charge,Charge },
        {Effect.Type.sleep,Sleep },
        {Effect.Type.copyStats,CopyStats },
        {Effect.Type.summon,Summon },
        {Effect.Type.doubleStats,DoubleStats},
        {Effect.Type.gainEconomy,GainEconomy},
        {Effect.Type.albatros, AlbatrosDeathDance },
        {Effect.Type.debuff, DeBuff },
        {Effect.Type.bodyguard, Bodyguard },
        {Effect.Type.summonHalfOf, SummonHalfStats},
        {Effect.Type.gainEconomyNextTurn, GainEconomyNextTurn},
        {Effect.Type.damageBoardEdges, DamageBoardEdges},
        {Effect.Type.sacrificeAndDamageAll,SacrificeAndDamageAll },
        {Effect.Type.randomDamage,RandomDamage },
        {Effect.Type.returnToHand,ReturnToHand },
        {Effect.Type.cantAttackForTurn,CantAttackForTurn},
        {Effect.Type.damageAndNeighbours,DamageAndNeighbours},
        {Effect.Type.buffAndCantAttack,BuffAndCantAttack},
        {Effect.Type.trueDamage,TrueDamage},
        {Effect.Type.discover,Discover},
        {Effect.Type.sendToFuture,SendToFuture},
        {Effect.Type.umbrella,Umbrella },
        {Effect.Type.syncDance,SyncDance },
        {Effect.Type.minionSwap,MinionSwap},
        {Effect.Type.loanPower,LoanStrength},
        {Effect.Type.buffAndNeighbours,LoanStrength},
        {Effect.Type.damageReduce,DamageReduction},
        {Effect.Type.copyCard,CopyFromEnemyHand},
        {Effect.Type.destroy,Destroy},
        {Effect.Type.discard,Discard},
        {Effect.Type.reActivate,ReActivate},
       
       
       
        // add more as needed
    };
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize()
    {
        Debug.Log($"EffectCommands initialized. Commands: {Registry.Count}");
    }
    public static void AlbatrosDeathDance(EffectContext ctx)
    {
        if (ctx.targetIds == null ||ctx.targetIds.Length==0) return;
        ushort a = ctx.doerId;
        ushort b = ctx.targetIds[0];

        GameManager.instance.GetMinionLogic(a).Buff(ctx.buff.x, ctx.buff.y);
        GameManager.instance.GetMinionLogic(b).Buff(ctx.buff.x, ctx.buff.y);

        GameManager.instance.RegisterWatchedDeath(a, b, ctx.effect.give);
        GameManager.instance.RegisterWatchedDeath(b, a, ctx.effect.give);
    }
    public static void Damage(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
        {
                t.Damage(ctx.value, ctx.doerId);
        }
    }
    public static void RandomDamage(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
        {
            t.Damage(
            UnityEngine.Random.Range(ctx.buff.x, ctx.buff.y+1));
        }
    }
    public static void DamageAndNeighbours(EffectContext ctx)
    {
        ushort main = ctx.targets[0].sequenceId;
        List<ushort> neighbours=GameManager.instance.GetNeighbours(ctx.targets[0].sequenceId, !ctx.playerController.isEnemy.Value);
        List<MinionLogic> list=new List<MinionLogic>();
        list.Add(ctx.targets[0]);
        foreach(var t in neighbours)
            list.Add(GameManager.instance.GetMinionLogic(t));
        foreach(var t in GameManager.instance.SortByBodyGuard(list))
        {
            t.Damage((main == t.sequenceId) ? ctx.value : ctx.buff.x,ctx.doerId);
                
        }
    }
    public static void TrueDamage(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
        {
            t.TrueDamage(ctx.value, ctx.doerId);
        }
    }
    public static void DamageBoardEdges(EffectContext ctx)
    {
        var board = GameManager.instance.GetEnemyBoard(!ctx.playerController.isEnemy.Value);
        if (board.Count == 0) return;
        GameManager.instance.GetMinionLogic(board[0].sequenceId)?.Damage(ctx.value, ctx.doerId);
        GameManager.instance.graveyard.Execute();
        if (board.Count > 1)
            GameManager.instance.GetMinionLogic(board[board.Count - 1].sequenceId)?.Damage(ctx.value, ctx.doerId);
        else { GameManager.instance.GetMinionLogic(board[0].sequenceId)?.Damage(ctx.value, ctx.doerId); }
        GameManager.instance.graveyard.Execute();
    }
    public static void SacrificeAndDamageAll(EffectContext ctx)
    {
        ushort sacrificeId = ctx.targetIds[0];
        var sacrifice = GameManager.instance.GetMinionById(sacrificeId);
        int dmg = sacrifice.attack;

        // előbb a sebzés-adat, MIELŐTT meghal
        GameManager.instance.GetMinionLogic(sacrificeId).Death();

        var enemyBoard = GameManager.instance.GetEnemyBoard(!ctx.playerController.isEnemy.Value).ToList();
        foreach (var m in enemyBoard)
            GameManager.instance.GetMinionLogic(m.sequenceId)?.Damage(dmg, ctx.doerId);
    }
    public static void CantAttackForTurn(EffectContext ctx)
    {
        for(int i=0;i<ctx.value;i++)
        GameManager.instance.OtherPlayer(ctx.playerController).CantAttackForTurn++;
    }
    public static void BuffAndCantAttack(EffectContext ctx)
    {
        foreach (var i in ctx.targets)
        {
            i.Buff(ctx.buff.x, ctx.buff.y);
            GameManager.instance.ChangeMinionById(i.sequenceId, m => { m.canAttack = false; return m; });
        }
    }
    public static void Discard(EffectContext ctx)
    {
        var hand = ctx.playerController.hand;
        if (hand.Count == 0) return;

        int randomIndex = UnityEngine.Random.Range(0, hand.Count);
        ctx.playerController.RemoveCardFromHand(hand[randomIndex]);
    }
    public static void ReActivate(EffectContext ctx)
    {
        foreach (var target in ctx.targets)
        {
            GameManager.instance.ChangeMinionById(target._sequenceId, m =>
            {
                m.canAttack = true;
                return m;
            });
        }
    }
    public static void Umbrella(EffectContext ctx)
    {
        foreach (ushort id in ctx.targetIds)
        {
            var logic = GameManager.instance.GetMinionLogic(id);
            if (logic == null) continue;

            logic.effectBag.Add(ctx.effect, ctx.doerId, EffectRole.Guard,
                charges: -1, toBlock: Effect.Type.damage,
                expiresInTurns: 2);
        }
    }
    public static void LoanStrength(EffectContext ctx)
    {
        foreach (var target in ctx.targets)
        {
            target.Buff(ctx.buff.x, ctx.buff.x);

            ushort capturedId = target._sequenceId;

            GameEvents.Instance.AddDelayedEffect(
                callback: () =>
                {
                    var logic = GameManager.instance.GetMinionLogic(capturedId);
                    if (logic == null || logic.effectBag.IsLocked) return;
                    logic.DeBuff(ctx.doerId, new Vector2Int(ctx.buff.y, ctx.buff.y));
                },
                count: ctx.value,
                clock: GameEvents.EventType.TurnStart,
                ownerId: capturedId
            );
        }
    }
    public static void DamageReduction(EffectContext ctx)
    {
        MinionLogic heroLogic = GameManager.instance.GetMinionLogic(ctx.doerId);

        if (heroLogic == null) return;

        var live = heroLogic.effectBag.Add(ctx.effect, ctx.doerId, EffectRole.Guard,
            charges: ctx.value,
            expiresInTurns: 2);
    }
    public static void CopyFromEnemyHand(EffectContext ctx)
    {
        var enemy = GameManager.instance.GetEnemy(ctx.playerController);
        if (enemy == null || enemy.hand.Count == 0) return;

        var choices = enemy.hand
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(3)
            .Select(c => c.cardId)
            .ToArray();

        ctx.playerController.TargetDiscoverChoicesForCopy(
    ctx.playerController.Owner, choices);
    }
    public static void Destroy(EffectContext ctx)
    {
        foreach (var target in ctx.targets)
            target.Death();
    }
    public static void BuffAndNeighbours(EffectContext ctx)
    {
        if (ctx.targets.Length == 0) return;

        var main = ctx.targets[0];
        bool isAlly = GameManager.instance.isAllyMinion(main._sequenceId);

        // fő célpont: buff * value
        main.Buff(ctx.buff.x * ctx.value, ctx.buff.y * ctx.value);

        // szomszédok: buff simán
        var neighbours = GameManager.instance.GetNeighbours(main._sequenceId, isAlly);
        foreach (var nid in neighbours)
        {
            var logic = GameManager.instance.GetMinionLogic(nid);
            logic?.Buff(ctx.buff.x, ctx.buff.y);
        }
    }
    public static void MinionSwap(EffectContext ctx)
    {
        var ally = GameManager.instance.boardAlly;
        var enemy = GameManager.instance.boardEnemy;
        bool pickFromAlly;
        if (enemy.Count==0||ally.Count==0)
        {
            pickFromAlly=enemy.Count==0? true: false;
        }
        else
        {
            pickFromAlly  = UnityEngine.Random.value < 0.25f && ally.Count > 0;
        }
        
        var board = pickFromAlly ? ally : enemy;

        if (board.Count == 0) return;   // az ellenfélnek sincs lénye, nincs mit csinálni

        int fromIndex = UnityEngine.Random.Range(0, board.Count);
        var picked = board[fromIndex];

        bool wasLeftEdge = fromIndex == 0;
        bool wasRightEdge = fromIndex == board.Count - 1;

        bool goToLeft;
        if (wasLeftEdge) goToLeft = false;
        else if (wasRightEdge) goToLeft = true;   // "ha jobb szélső, akkor a bal szélére"
        else goToLeft = UnityEngine.Random.value < 0.5f;

        board.RemoveAt(fromIndex);

        if (goToLeft) board.Insert(0, picked);
        else board.Add(picked);

        GameManager.instance.SendClientEvent(new ClientEvent
        {
            effectType = (ushort)Effect.Type.minionSwap,
            targetIds = new ushort[] { picked.sequenceId },
            value = goToLeft ? 0 : board.Count - 1
        });
    }
    public static void SyncDance(EffectContext ctx)
    {
        bool targetIsEnemySide = ctx.effect.target == Trigger.Target.enemy;

        // a kijátszó saját oldala a szerver-listák közül:
        bool TargettingEnemy = ctx.playerController.isEnemy.Value;

        var board = (targetIsEnemySide != TargettingEnemy)
        ? GameManager.instance.boardEnemy
        : GameManager.instance.boardAlly;
        
        if (board.Count == 0) return;

        int totalAttack = 0, totalHealth = 0;
        foreach (var m in board)
        {
            totalAttack += m.attack;
            totalHealth += m.currentHealth;
        }
        int avgAttack;
        int avgHealth;
        if (TargettingEnemy)
        {
            avgAttack = RoundHalfDown((float)totalAttack / board.Count);
            avgHealth = RoundHalfDown((float)totalHealth / board.Count);
        }
        else 
        {
            avgAttack = RoundHalfUp((float)totalAttack / board.Count);
            avgHealth = RoundHalfUp((float)totalHealth / board.Count);
        }
        foreach (var m in board.ToList())
        {
            var logic = GameManager.instance.GetMinionLogic(m.sequenceId);
            logic?.SetStats(avgAttack, avgHealth);
        }
    }
    static int RoundHalfDown(float value)
    {
        return Mathf.CeilToInt(value - 0.5f);
    }

    static int RoundHalfUp(float value)
    {
        return Mathf.FloorToInt(value + 0.5f);
    }
    public static void SendToFuture(EffectContext ctx)
    {
        MinionLogic target = ctx.targets[0];
        ushort id = target._sequenceId;
        var gm = GameManager.instance;

        var state = gm.GetMinionById(id);
        bool isAlly = gm.isAllyMinion(id);

        target.effectBag.Lock(ctx.value, state, isAlly);

        gm.RemoveFromBoardSilently(id);
        gm.lockedMinions.Add(target);

        gm.SendClientEvent(new ClientEvent
        {
            effectType = (ushort)Effect.Type.sendToFuture,
            targetIds = new ushort[] { id }
        });
    }
    public static void ReturnToHand(EffectContext ctx)
    {
        foreach (ushort id in ctx.targetIds)
        {
            ctx.playerController.ReturnToHand(id);
        }
    }
    public static void Discover(EffectContext ctx)
    {
        var deck = ctx.playerController.GetDeck();
        if (deck.Count == 0) return;

        var choices = deck.OrderBy(_ => UnityEngine.Random.value).Take(3)
                           .Select(c => c.cardId).ToArray();

        ctx.playerController.TargetDiscoverChoices(ctx.playerController.Owner, choices);
    }
    //A lejárat a kulcs: az expiresOnTurn és a TickExpiry már megvannak a LiveEffect-ben és az EffectBag-ben —
    /// 
    /// /de ellenőrizni kell, hogy a TickExpiry-t hívja-e valami minden kör elején. Ez ugyanaz a hiányzó bekötés, mint a TickDelayed-nél, csak egy másik metódus.
    /// 
    /// <param name="ctx"></param>
    public static void GainEconomyNextTurn(EffectContext ctx)
    {
        if (ctx.effect.target != Trigger.Target.enemy)
            GameManager.instance.GetMinionLogic(ctx.doerId).GainEconomyNextTurn(ctx.value);
        else
            GameManager.instance.OtherPlayer(ctx.playerController).economy.GainEconomyNextTurn(ctx.value);
    }
    public static void SummonHalfStats(EffectContext ctx)
    {
        MinionLogic target = GameManager.instance.GetMinionLogic(ctx.doerId);
        if (target == null) return;

        MinionCard card = CardManager.instance.GetMinion(target.cardId);
        if (card == null) return;

        var buff = GameManager.GetMinionBuff(target.cardId, target);

        int fullAttack = card.attack + buff.x;
        int fullHealth = card.health + buff.y;

        int halfAttack = Mathf.Max(1, fullAttack / 2);
        int halfHealth = Mathf.Max(1, fullHealth / 2);

        ctx.playerController.Summon(card.cardId, overrideAttack: halfAttack, overrideHealth: halfHealth);
    }
    public static void DeBuff(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
            t.DeBuff(ctx.doerId,ctx.effect.buff);
    }
    public static void Charge(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
        {
            t.Charge();
        }
    }
    public static void Bodyguard(EffectContext ctx)
    {
        foreach (ushort id in ctx.targetIds)
        {
            var logic = GameManager.instance.GetMinionLogic(id);
            if (logic == null) continue;
            logic.effectBag.Add(ctx.effect, ctx.doerId, EffectRole.Aura, charges: -1);
        }
    }
    public static void CopyStats(EffectContext ctx)
    {
        if (ctx.targets.Length > 1) {Debug.LogWarning("COpy multiple stats? ");
        return;}
        GameManager.instance.GetMinionLogic(ctx.doerId).CopyStats(ctx.targetIds[0], ctx.buff);
        
    }
    public static void DoubleStats(EffectContext ctx)
    {
        foreach(var t in ctx.targets)
        {
            t.DoubleStats(ctx.doerId, ctx.buff);
        }
    }
    public static void Attack(EffectContext ctx)
    {
        Debug.Log("ctx target size " + ctx.targets.Length);
        GameManager.instance.ExecuteAttack(ctx.doerId,(ushort) ctx.targets[0]._sequenceId);
    }

    public static void Heal(EffectContext ctx)
    {
        if (ctx == null) { Debug.LogError("Nincs Context!"); return; }
        if (ctx.effect == null) { Debug.LogError("Nincs Effect adat a Contextben!"); return; }
        if (ctx.targets == null) { Debug.LogError("Nincs Target lista a Contextben!"); return; }

        foreach (var t in ctx.targets)
        {
            t.Heal(ctx.value, ctx.doerId); // ✨ Átadjuk a doerId-t!
        }
    }
    public static void Buff(EffectContext ctx)
    {
        if (ctx?.effect == null) { Debug.LogError("Buff: nincs Effect."); return; }
        if (ctx.targetIds == null) { Debug.LogError("Buff: nincs target lista."); return; }

        int attackBonus = ctx.buff.x;
        int healthBonus = ctx.buff.y;

        foreach (ushort id in ctx.targetIds)
            GameManager.instance.AddStats(id, attackBonus, healthBonus);
    }
    public static void Summon(EffectContext ctx)
    {
        bool toHome = true;
        if (ctx.effect.target == Trigger.Target.enemy || ctx.effect.target == Trigger.Target.ally)
            toHome = ctx.effect.target == Trigger.Target.ally;
        else
            Debug.LogWarning("A Summon effect nincs beállítva hogy home vagy enemy summon");

        int count = Mathf.Max(1, ctx.value);   // hány darabot idézzen

        for (int i = 0; i < count; i++)
            ctx.playerController.Summon(ctx.effect.summonableId, toHome);
    }
    public static void Sleep(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
            t.effectBag.Add(ctx.effect, t._sequenceId, EffectRole.Trigger,
                            charges: ctx.value);
    }
    public static void Give(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
        {
            if (t is ILiveTarget lt)
            {
                // például buffolás, hatás adás stb.
                lt.Attack += ctx.effect.buff.x;
                lt.Health += ctx.effect.buff.y;
            }
        }
    }
    public static void GainEconomy(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
            t.GainEconomy(ctx.doerId,ctx.value);
    }
    // További parancsok ide jöhetnek
}
