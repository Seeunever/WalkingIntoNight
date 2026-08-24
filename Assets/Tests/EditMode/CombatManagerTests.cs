using System.Collections.Generic;
using NUnit.Framework;
using WalkingIntoNight.TRPG.Character;
using WalkingIntoNight.TRPG.Combat;
using WalkingIntoNight.TRPG.Dice;

namespace WalkingIntoNight.TRPG.Tests.EditMode
{
    public class CombatManagerTests
    {
        [Test]
        public void TryStartEncounter_UnknownId_FailsWithoutActiveState()
        {
            var manager = CreateManager();

            Assert.That(manager.TryStartEncounter("missing", Investigator(), out var error), Is.False);

            Assert.That(error, Does.Contain("未知战斗配置"));
            Assert.That(manager.State, Is.Null);
            Assert.That(manager.IsActive, Is.False);
        }

        [Test]
        public void TryStartEncounter_EmptyEnemyList_FailsWithoutActiveState()
        {
            var manager = CreateManager();
            var empty = new CombatEncounterDefinition
            {
                id = "empty",
                displayName = "空遭遇",
                enemies = new List<CombatantData>()
            };

            Assert.That(manager.TryStartEncounter(empty, Investigator(), out var error), Is.False);

            Assert.That(error, Does.Contain("没有有效敌人"));
            Assert.That(manager.State, Is.Null);
        }

        [Test]
        public void PlayerAttack_KillsLastEnemy_EndsWithVictoryAndClampedHp()
        {
            var manager = CreateManager(
                checks: new Queue<bool>(new[] { true }),
                randomRange: (minimum, maximum) => maximum - 1);
            Assert.That(manager.TryStartEncounter("shadow_rat", Investigator(), out _), Is.True);
            var actionVersion = manager.State.turnNumber;

            Assert.That(manager.PlayerAttack(0, actionVersion), Is.True);

            Assert.That(manager.State.ended, Is.True);
            Assert.That(manager.State.playerWon, Is.True);
            Assert.That(manager.State.playerFled, Is.False);
            Assert.That(manager.State.combatants.Find(c => !c.isPlayer).HP, Is.EqualTo(0));
        }

        [Test]
        public void PlayerDefeat_ClampsAndSynchronizesInvestigatorHp()
        {
            var investigator = Investigator(hp: 5);
            var manager = CreateManager(
                checks: new Queue<bool>(new[] { false, true }),
                randomRange: (minimum, maximum) => maximum - 1);
            Assert.That(manager.TryStartEncounter("shadow_rat", investigator, out _), Is.True);

            Assert.That(manager.PlayerAttack(0, manager.State.turnNumber), Is.True);
            manager.SyncPlayerToInvestigator();

            Assert.That(manager.State.ended, Is.True);
            Assert.That(manager.State.playerWon, Is.False);
            Assert.That(manager.GetPlayer().HP, Is.EqualTo(0));
            Assert.That(investigator.HP, Is.EqualTo(0));
        }

        [Test]
        public void PlayerDodge_AppliesPenaltyForEnemyRoundThenResets()
        {
            var observedPenaltyDice = new List<int>();
            var manager = new CombatManager(
                (skill, difficulty, skillId, bonusDice, penaltyDice) =>
                {
                    observedPenaltyDice.Add(penaltyDice);
                    return Result(false);
                },
                (minimum, maximum) => minimum);
            Assert.That(manager.TryStartEncounter("shadow_rat", Investigator(), out _), Is.True);

            Assert.That(manager.PlayerDodge(manager.State.turnNumber), Is.True);

            Assert.That(observedPenaltyDice, Is.EqualTo(new[] { 1 }));
            Assert.That(manager.State.playerDodging, Is.False);
            Assert.That(manager.State.playerTurn, Is.True);
            Assert.That(manager.State.turnNumber, Is.EqualTo(2));
        }

        [Test]
        public void PlayerFlee_SuccessEndsEncounterAndPublishesOneUpdate()
        {
            var manager = CreateManager(new Queue<bool>(new[] { true }));
            Assert.That(manager.TryStartEncounter("shadow_rat", Investigator(), out _), Is.True);
            var updates = 0;
            manager.OnCombatUpdated += () => updates++;

            Assert.That(manager.PlayerFlee(manager.State.turnNumber), Is.True);

            Assert.That(manager.State.ended, Is.True);
            Assert.That(manager.State.playerFled, Is.True);
            Assert.That(manager.State.playerWon, Is.False);
            Assert.That(updates, Is.EqualTo(1));
        }

        [Test]
        public void PlayerFlee_FailureRunsEnemyRoundAndPublishesOneUpdate()
        {
            var manager = CreateManager(new Queue<bool>(new[] { false, false }));
            Assert.That(manager.TryStartEncounter("shadow_rat", Investigator(), out _), Is.True);
            var updates = 0;
            manager.OnCombatUpdated += () => updates++;

            Assert.That(manager.PlayerFlee(manager.State.turnNumber), Is.True);

            Assert.That(manager.State.ended, Is.False);
            Assert.That(manager.State.playerFled, Is.False);
            Assert.That(manager.State.turnNumber, Is.EqualTo(2));
            Assert.That(updates, Is.EqualTo(1));
        }

        [Test]
        public void OldActionVersion_CannotActAgainOnNewTurn()
        {
            var manager = CreateManager(new Queue<bool>(new[] { false, false }));
            Assert.That(manager.TryStartEncounter("shadow_rat", Investigator(), out _), Is.True);
            var firstTurn = manager.State.turnNumber;

            Assert.That(manager.PlayerAttack(0, firstTurn), Is.True);
            Assert.That(manager.State.turnNumber, Is.EqualTo(2));
            Assert.That(manager.PlayerAttack(0, firstTurn), Is.False);
            Assert.That(manager.State.turnNumber, Is.EqualTo(2));
        }

        static CombatManager CreateManager(
            Queue<bool> checks = null,
            System.Func<int, int, int> randomRange = null)
        {
            checks ??= new Queue<bool>();
            return new CombatManager(
                (skill, difficulty, skillId, bonusDice, penaltyDice) =>
                    Result(checks.Count > 0 && checks.Dequeue()),
                randomRange ?? ((minimum, maximum) => minimum));
        }

        static CheckResult Result(bool success)
        {
            return new CheckResult
            {
                ResultType = success
                    ? CheckResultType.RegularSuccess
                    : CheckResultType.Failure
            };
        }

        static Investigator Investigator(int hp = 12)
        {
            var investigator = new Investigator
            {
                Name = "战斗测试调查员",
                STR = 10,
                SIZ = 10,
                HP = hp,
                MaxHP = 12,
                SAN = 50,
                MaxSAN = 50,
                MP = 10,
                MaxMP = 10
            };
            investigator.Skills["fight"] = 60;
            investigator.Skills["dodge"] = 60;
            return investigator;
        }
    }
}
