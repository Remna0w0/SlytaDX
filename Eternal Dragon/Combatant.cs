namespace RemnaBotService.Eternal_Dragon
{
    public class Combatant
    {
        /// <summary>
        /// This is the base class for both the player and the dragon. 
        /// Defines necessary stats and ways to manipulate them upon instantiation.
        /// Flat damage calculations are done here
        /// </summary>
        public string Name { get; set; }
        public int HP { get; set; }

        public int BasePower { get; set; }
        public int BaseBuff { get; set; }
        public double SpellBase { get; set; } = 70;

        // The Enraged status inflicts the Sunder status on the target on the first succesful attack, so we need a bool to track when that happens
        public bool IsFirstEnragedAttack { get; set; } = true;

        // Each status has a name and an associated value. Some status have no associated value, but serve to notify the player of their affects.
        // No status is explicity defined yet, maybe something to come back to if the scope gets larger
        public Dictionary<string, double> Statuses { get; set; } = new Dictionary<string, double>();


        public Combatant(string name, int hp, int basePower)
        {
            Name = name;
            HP = hp;
            BasePower = basePower;
            BaseBuff = 0;
        }

        public int GetCalculatedAttack(bool isSpell)
        {
            // Spells always have the same base power unless the user is using the Brillaint Caststaff, which boosts spell base upon equip
            if (isSpell)
            {
                double spellTotal = SpellBase;
                if (Statuses.ContainsKey("Channeled"))
                {
                    spellTotal *= Statuses["Channeled"];
                }

                if (Statuses.ContainsKey("Enraged"))
                {
                    spellTotal *= Statuses["Enraged"];
                }
                
                
                // Slip Counter will ALWAYS be calculated last
                if (Statuses.ContainsKey("SlipCounter"))
                {
                    spellTotal *= Statuses["SlipCounter"];
                }

                return (int)Math.Max(0, spellTotal);
            }
            else
            {
                Random rand = new Random();

                // base power will always fluctuate, but will never deviate far from original value
                int roll = rand.Next(-5, 6);

                double total = BasePower + BaseBuff + roll;

                if (Statuses.ContainsKey("Channeled"))
                {
                    total *= Statuses["Channeled"];
                }

                if (Statuses.ContainsKey("Enraged"))
                {
                    total *= Statuses["Enraged"];
                }

                // Slip Counter will ALWAYS be calculated last
                if (Statuses.ContainsKey("SlipCounter"))
                {
                    total *= Statuses["SlipCounter"];
                }

                return (int)Math.Max(0, total);
            }
        }
    }
}
