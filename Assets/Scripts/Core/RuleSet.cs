using System;

namespace LuckyStep
{
    /// <summary>
    /// Data describing which step counts are lucky and which are deadly. Pure C#, no engine dependency.
    /// Deadly always wins over lucky (a number that is both counts as deadly).
    /// </summary>
    /// <example>
    /// <code>
    /// bool lucky = RuleSet.Default.IsLucky(14);   // true
    /// bool deadly = RuleSet.World4.IsDeadly(24);  // true
    /// </code>
    /// </example>
    public sealed class RuleSet
    {
        /// <summary>Default rules: multiples of 7 lucky, exactly 13 deadly.</summary>
        public static readonly RuleSet Default = new RuleSet("default", 7, new[] { 13 }, -1, startValue: 0);

        /// <summary>World 4 rules: multiples of 8 lucky, any number containing the digit 4 deadly, 13 harmless.</summary>
        public static readonly RuleSet World4 = new RuleSet("world4", 8, new int[0], 4, startValue: 5);

        private readonly int _luckyDivisor;
        private readonly int[] _deadlyExact;
        private readonly int _deadlyDigit;

        /// <summary>Name used by level files in the "rules:" header.</summary>
        public string Name { get; }

        /// <summary>Tiles a dice tile flings the pawn on a normal step.</summary>
        public int DiceNormalDistance { get; }

        /// <summary>Tiles a dice tile flings the pawn on a lucky step.</summary>
        public int DiceLuckyDistance { get; }

        /// <summary>
        /// Counter value a level starts at, that the black cat resets the counter to, and that is the floor while the
        /// broken mirror counts down. It is never lucky (0 is never lucky; 5 is not a multiple of 8).
        /// </summary>
        public int StartValue { get; }

        /// <summary>Creates a rule set.</summary>
        /// <param name="name">Name used by level files.</param>
        /// <param name="luckyDivisor">Positive multiples of this are lucky; 0 or less disables lucky steps.</param>
        /// <param name="deadlyExact">Counts that are deadly when equal exactly.</param>
        /// <param name="deadlyDigit">A decimal digit 0-9 that makes any count containing it deadly; -1 for none.</param>
        /// <param name="diceNormalDistance">Dice fling length on a normal step.</param>
        /// <param name="diceLuckyDistance">Dice fling length on a lucky step.</param>
        /// <param name="startValue">Start / cat-reset / mirror-floor counter value.</param>
        public RuleSet(string name, int luckyDivisor, int[] deadlyExact, int deadlyDigit,
            int diceNormalDistance = 1, int diceLuckyDistance = 6, int startValue = 0)
        {
            StartValue = startValue;
            DiceNormalDistance = diceNormalDistance;
            DiceLuckyDistance = diceLuckyDistance;
            Name = name;
            _luckyDivisor = luckyDivisor;
            _deadlyExact = deadlyExact;
            _deadlyDigit = deadlyDigit;
        }

        /// <summary>Looks up a built-in rule set by name (case-insensitive). Returns null if unknown.</summary>
        public static RuleSet FindByName(string name)
        {
            if (string.Equals(name, Default.Name, StringComparison.OrdinalIgnoreCase)) return Default;
            if (string.Equals(name, World4.Name, StringComparison.OrdinalIgnoreCase)) return World4;
            return null;
        }

        /// <summary>True if the count is lucky. 0 is never lucky. Ignores deadliness; callers check IsDeadly first.</summary>
        public bool IsLucky(int count)
        {
            return count > 0 && _luckyDivisor > 0 && count % _luckyDivisor == 0;
        }

        /// <summary>True if the count kills.</summary>
        public bool IsDeadly(int count)
        {
            for (int i = 0; i < _deadlyExact.Length; i++)
            {
                if (_deadlyExact[i] == count) return true;
            }
            if (_deadlyDigit < 0) return false;
            if (count == 0) return _deadlyDigit == 0;
            for (int n = count; n > 0; n /= 10)
            {
                if (n % 10 == _deadlyDigit) return true;
            }
            return false;
        }
    }
}
