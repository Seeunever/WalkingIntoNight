using System.Collections.Generic;
using AnimalCafe.TRPG.Character;
using AnimalCafe.TRPG.Core;
using AnimalCafe.TRPG.Dice;
using UnityEngine;

namespace AnimalCafe.TRPG.Combat
{
    public class CombatManager
    {
        public CombatState State { get; private set; }

        public void ClearEncounter()
        {
            State = null;
        }
        public System.Action OnCombatUpdated;
        public System.Action<string> OnCombatLog;

        public bool IsActive => State != null && !State.ended;

        public void StartEncounter(string encounterId, Investigator investigator)
        {
            var def = CombatEncounterDatabase.Get(encounterId);
            if (def == null)
            {
                Debug.LogWarning($"Unknown encounter: {encounterId}");
                return;
            }

            State = new CombatState { encounterId = encounterId, playerTurn = true };

            var player = new CombatantData
            {
                id = "player",
                displayName = investigator.Name,
                isPlayer = true,
                HP = investigator.HP,
                MaxHP = investigator.MaxHP,
                skillFight = investigator.GetSkill("fight"),
                skillDodge = investigator.GetSkill("dodge")
            };
            State.combatants.Add(player);

            foreach (var enemy in def.enemies)
            {
                State.combatants.Add(CloneEnemy(enemy));
            }

            Log($"遭遇战开始：{def.displayName}");
            OnCombatUpdated?.Invoke();
        }

        static CombatantData CloneEnemy(CombatantData src)
        {
            return new CombatantData
            {
                id = src.id,
                displayName = src.displayName,
                isPlayer = false,
                HP = src.HP,
                MaxHP = src.MaxHP,
                skillFight = src.skillFight,
                skillDodge = src.skillDodge
            };
        }

        public CombatantData GetPlayer() => State?.combatants.Find(c => c.isPlayer);

        public List<CombatantData> GetEnemies() =>
            State?.combatants.FindAll(c => !c.isPlayer && c.HP > 0) ?? new List<CombatantData>();

        public void PlayerAttack(int enemyIndex)
        {
            if (!IsActive || !State.playerTurn) return;

            var player = GetPlayer();
            var enemies = GetEnemies();
            if (player == null || enemyIndex < 0 || enemyIndex >= enemies.Count) return;

            var target = enemies[enemyIndex];
            var result = DiceRoller.SkillCheck(player.skillFight, CheckDifficulty.Regular, "fight");
            if (result.IsSuccess)
            {
                var dmg = Random.Range(1, 9) + Mathf.Max(0, GameStateManager.Instance.Investigator.DamageBonus);
                target.HP -= dmg;
                Log($"{player.displayName} 攻击 {target.displayName} 造成 {dmg} 点伤害。");
            }
            else
            {
                Log($"{player.displayName} 攻击落空。");
            }

            if (target.HP <= 0)
            {
                target.HP = 0;
                Log($"{target.displayName} 被击败。");
            }

            EndPlayerTurn();
        }

        public void PlayerDodge()
        {
            if (!IsActive || !State.playerTurn) return;
            Log("你采取闪避姿态，本回合防御提升（简化）。");
            EndPlayerTurn();
        }

        public void PlayerFlee()
        {
            if (!IsActive) return;
            var result = DiceRoller.SkillCheck(GetPlayer().skillDodge, CheckDifficulty.Regular, "dodge");
            if (result.IsSuccess)
            {
                State.ended = true;
                State.playerFled = true;
                Log("你成功逃离战斗。");
            }
            else
            {
                Log("逃跑失败！");
                EndPlayerTurn();
            }

            OnCombatUpdated?.Invoke();
        }

        void EndPlayerTurn()
        {
            if (State.ended) return;
            State.playerTurn = false;
            EnemyTurn();
            if (!State.ended)
            {
                State.playerTurn = true;
            }

            CheckEnd();
            OnCombatUpdated?.Invoke();
        }

        void EnemyTurn()
        {
            var player = GetPlayer();
            foreach (var enemy in GetEnemies())
            {
                if (player.HP <= 0) break;

                var result = DiceRoller.SkillCheck(enemy.skillFight, CheckDifficulty.Regular, "fight");
                if (result.IsSuccess)
                {
                    var dmg = Random.Range(1, 7);
                    player.HP -= dmg;
                    Log($"{enemy.displayName} 攻击造成 {dmg} 点伤害。");
                }
                else
                {
                    Log($"{enemy.displayName} 攻击落空。");
                }
            }
        }

        void CheckEnd()
        {
            var player = GetPlayer();
            if (player.HP <= 0)
            {
                player.HP = 0;
                State.ended = true;
                State.playerWon = false;
                Log("你倒下了……");
                return;
            }

            if (GetEnemies().Count == 0)
            {
                State.ended = true;
                State.playerWon = true;
                Log("战斗胜利！");
            }
        }

        public void SyncPlayerToInvestigator()
        {
            var player = GetPlayer();
            var inv = GameStateManager.Instance?.Investigator;
            if (player == null || inv == null) return;
            inv.HP = Mathf.Clamp(player.HP, 0, inv.MaxHP);
        }

        void Log(string msg)
        {
            OnCombatLog?.Invoke(msg);
        }
    }
}
