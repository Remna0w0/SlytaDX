namespace RemnaBotService.Eternal_Dragon
{
    public static class WeaponLibrary
    {
        public static Weapon GetWeapon(string name)
        {
            return name switch
            {
                "Stalwart Blade" => new Weapon { Name = "Stalwart Blade", BasePower = 50 },
                "Nightfall Axe" => new Weapon { Name = "Nightfall Axe", BasePower = 80, HasMighty = true, DodgeBonus = -30 },
                "Brilliant Caststaff" => new Weapon { Name = "Brilliant Caststaff", BasePower = 50, CanBlock = false, StartsChanneled = true, SpellBoosted = true },
                "Unseen Daggers" => new Weapon { Name = "Unseen Daggers", BasePower = 35, DodgeBonus = 15, SlipCounterMultiplier = 2.2 },
                _ => new Weapon { Name = "Stalwart Blade", BasePower = 50 }
            };
        }
    }
}
