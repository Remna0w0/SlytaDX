namespace RemnaBotService.Eternal_Dragon
{
    public class Combatant
    {
        public string Name { get; set; }
        public int HP { get; set; }
        public int BasePower { get; set; }
        public int BaseBuff { get; set; }

        public bool IsFirstEnragedAttack { get; set; } = true;

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
            if (isSpell)
            {
                double spellTotal = 70;
                if (Statuses.ContainsKey("Channeled"))
                {
                    spellTotal *= Statuses["Channeled"];
                }

                if (Statuses.ContainsKey("Enraged"))
                {
                    spellTotal *= Statuses["Enraged"];
                }

                if (Statuses.ContainsKey("SlipCounter"))
                {
                    spellTotal *= Statuses["SlipCounter"];
                }

                return (int)Math.Max(0, spellTotal);
            }
            else
            {
                Random rand = new Random();
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


                if (Statuses.ContainsKey("SlipCounter"))
                {
                    total *= Statuses["SlipCounter"];
                }

                return (int)Math.Max(0, total);
            }
        }
    }
}
