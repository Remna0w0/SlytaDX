namespace RemnaBotService.Eternal_Dragon
{
    public class Weapon
    {
        public string Name { get; set; }
        public int BasePower { get; set; }
        public bool HasMighty { get; set; }
        public bool CanBlock { get; set; } = true;
        public int DodgeBonus { get; set; } = 0;
        public bool SpellBoosted { get; set; } = false;
        public double SlipCounterMultiplier { get; set; } = 1.75;
        public bool StartsChanneled { get; set; } = false;
    }
}
