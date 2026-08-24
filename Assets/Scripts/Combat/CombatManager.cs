using System.Collections.Generic;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Dice;
using UnityEngine;

namespace WalkingIntoNight.TRPG.Combat
{
    public class CombatManager
    {
        readonly System.Func<int, CheckDifficulty, string, int, int, CheckResult> m_skillCheck;
        readonly System.Func<int, int, int> m_randomRange;
        Investigator m_investigator;
        bool m_actionConsumed;

        public CombatState State { get; private set; }
        public System.Action OnCombatUpdated;
        public System.Action<string> OnCombatLog;

        public bool IsActive => State != null && !State.ended;

        public CombatManager(
            System.Func<int, CheckDifficulty, string, int, int, CheckResult> skillCheck = null,
            System.Func<int, int, int> randomRange = null)
        {
            m_skillCheck = skillCheck ?? ((skill, difficulty, skillId, bonusDice, penaltyDice) =>
                DiceRoller.SkillCheck(skill, difficulty, skillId, bonusDice, penaltyDice));
            m_randomRange = randomRange ?? ((minimum, maximum) => Random.Range(minimum, maximum));
        }

        public void ClearEncounter()
        {
            State = null;
            m_investigator = null;
            m_actionConsumed = false;
        }

        public bool TryStartEncounter(string encounterId, Investigator investigator, out string error)
        {
            var def = CombatEncounterDatabase.Get(encounterId);
            if (def == null)
            {
                error = $"未知战斗配置：{encounterId ?? "（空）"}。";
                return false;
            }

            return TryStartEncounter(def, investigator, out error);
        }

        public bool TryStartEncounter(
            CombatEncounterDefinition definition,
            Investigator investigator,
            out string error)
        {
            error = null;
            if (IsActive)
            {
                error = "已有战斗正在进行。";
                return false;
            }

            // An ended encounter must never leak into a later failed start attempt.
            if (State != null)
                ClearEncounter();

            if (investigator == null)
            {
                error = "无法开始战斗：调查员不存在。";
                return false;
            }

            if (investigator.MaxHP < 1 || investigator.HP < 1 || investigator.HP > investigator.MaxHP)
            {
                error = "无法开始战斗：调查员生命值无效。";
                return false;
            }

            if (definition == null || string.IsNullOrWhiteSpace(definition.id))
            {
                error = "无法开始战斗：配置不存在或缺少 ID。";
                return false;
            }

            if (definition.enemies == null || definition.enemies.Count == 0)
            {
                error = $"无法开始战斗：{definition.displayName ?? definition.id} 没有有效敌人。";
                return false;
            }

            foreach (var enemy in definition.enemies)
            {
                if (enemy == null || string.IsNullOrWhiteSpace(enemy.id) ||
                    enemy.MaxHP < 1 || enemy.HP < 1 || enemy.HP > enemy.MaxHP)
                {
                    error = $"无法开始战斗：{definition.displayName ?? definition.id} 包含无效敌人数据。";
                    return false;
                }
            }

            m_investigator = investigator;
            m_actionConsumed = false;
            State = new CombatState
            {
                encounterId = definition.id,
                playerTurn = true,
                turnNumber = 1
            };

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

            foreach (var enemy in definition.enemies)
            {
                State.combatants.Add(CloneEnemy(enemy));
            }

            Log($"遭遇战开始：{definition.displayName ?? definition.id}");
            NotifyUpdated();
            return true;
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

        public bool PlayerAttack(int enemyIndex, int actionVersion = 0)
        {
            if (!CanTakePlayerAction(actionVersion)) return false;

            var player = GetPlayer();
            var enemies = GetEnemies();
            if (player == null || enemyIndex < 0 || enemyIndex >= enemies.Count) return false;

            m_actionConsumed = true;
            var target = enemies[enemyIndex];
            var result = m_skillCheck(player.skillFight, CheckDifficulty.Regular, "fight", 0, 0);
            if (result != null && result.IsSuccess)
            {
                var dmg = m_randomRange(1, 9) + Mathf.Max(0, m_investigator?.DamageBonus ?? 0);
                target.HP = Mathf.Max(0, target.HP - dmg);
                Log($"{player.displayName} 攻击 {target.displayName} 造成 {dmg} 点伤害。");
            }
            else
            {
                Log($"{player.displayName} 攻击落空。");
            }

            if (target.HP == 0)
            {
                Log($"{target.displayName} 被击败。");
            }

            EndPlayerTurn();
            return true;
        }

        public bool PlayerDodge(int actionVersion = 0)
        {
            if (!CanTakePlayerAction(actionVersion)) return false;

            m_actionConsumed = true;
            State.playerDodging = true;
            Log("你采取闪避姿态：本轮所有敌方攻击承受 1 个惩罚骰。");
            EndPlayerTurn();
            return true;
        }

        public bool PlayerFlee(int actionVersion = 0)
        {
            if (!CanTakePlayerAction(actionVersion)) return false;

            var player = GetPlayer();
            if (player == null) return false;

            m_actionConsumed = true;
            var result = m_skillCheck(player.skillDodge, CheckDifficulty.Regular, "dodge", 0, 0);
            if (result != null && result.IsSuccess)
            {
                State.ended = true;
                State.playerFled = true;
                State.playerWon = false;
                State.playerTurn = false;
                Log("你成功逃离战斗。");
                NotifyUpdated();
            }
            else
            {
                Log("逃跑失败！");
                EndPlayerTurn();
            }

            return true;
        }

        void EndPlayerTurn()
        {
            if (State == null || State.ended) return;

            State.playerTurn = false;
            EnemyTurn();
            CheckEnd();
            if (!State.ended)
            {
                State.turnNumber++;
                State.playerTurn = true;
                m_actionConsumed = false;
            }

            NotifyUpdated();
        }

        void EnemyTurn()
        {
            var player = GetPlayer();
            if (player == null)
            {
                State.playerDodging = false;
                return;
            }

            var penaltyDice = State.playerDodging ? 1 : 0;
            foreach (var enemy in GetEnemies())
            {
                if (player.HP <= 0) break;

                var result = m_skillCheck(
                    enemy.skillFight,
                    CheckDifficulty.Regular,
                    "fight",
                    0,
                    penaltyDice);
                if (result != null && result.IsSuccess)
                {
                    var dmg = m_randomRange(1, 7);
                    player.HP = Mathf.Max(0, player.HP - dmg);
                    Log($"{enemy.displayName} 攻击造成 {dmg} 点伤害。");
                }
                else
                {
                    Log($"{enemy.displayName} 攻击落空。");
                }
            }

            State.playerDodging = false;
        }

        void CheckEnd()
        {
            var player = GetPlayer();
            if (player == null || player.HP <= 0)
            {
                if (player != null) player.HP = 0;
                State.ended = true;
                State.playerWon = false;
                State.playerTurn = false;
                Log("你倒下了……");
                return;
            }

            if (GetEnemies().Count == 0)
            {
                State.ended = true;
                State.playerWon = true;
                State.playerTurn = false;
                Log("战斗胜利！");
            }
        }

        public void SyncPlayerToInvestigator()
        {
            var player = GetPlayer();
            var inv = m_investigator;
            if (player == null || inv == null) return;
            inv.HP = Mathf.Clamp(player.HP, 0, inv.MaxHP);
        }

        bool CanTakePlayerAction(int actionVersion)
        {
            if (!IsActive || !State.playerTurn || m_actionConsumed) return false;
            return actionVersion <= 0 || actionVersion == State.turnNumber;
        }

        void NotifyUpdated()
        {
            OnCombatUpdated?.Invoke();
        }

        void Log(string msg)
        {
            OnCombatLog?.Invoke(msg);
        }
    }
}
