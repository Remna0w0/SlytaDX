namespace RemnaBotService
{
    public class DialogContainer
    {
        private static readonly Dictionary<string, string[]> _dialogDatabase = new Dictionary<string, string[]>
        {
// --- PLAYER INTENT PREFACES ---
{
    "Player_Intent_Brave", new string[] {
        "You step forward, putting your entire body weight into a heavy, reckless assault!",
        "With flashing eyes, you unleash a brutal, offensive onslaught with your weapon!"
    }
},
{
    "Player_Intent_Block", new string[] {
        "You firmly plant your feet and hoist your guard, bracing for impact.",
        "Anticipating a strike, you pull back into a rigid defensive posture."
    }
},
{
    "Player_Intent_Spell", new string[] {
        "Mystic energy crackles around your hands as you begin weaving a complex spell structure.",
        "You hold your weapon aloft, drawing raw arcane energy directly from the room."
    }
},
{
    "Player_Intent_Dodge", new string[] {
        "You keep your weight light on your toes, preparing to weave away from danger.",
        "You focus entirely on the dragon's center of mass, looking for an evasive window."
    }
},

// --- DRAGON INTENT PREFACES ---
{
    "Dragon_Intent_Brave", new string[] {
        "The Dragon lets out a deafening roar and lunges forward with predatory ferocity!",
        "Baring its massive fangs, the Dragon sweeps its heavy claws forward in a terrifying assault!"
    }
},
{
    "Dragon_Intent_Block", new string[] {
        "The Dragon curls inward, wrapping its heavily armored wings around its body like a fortress.",
        "The ancient beast tenses its scales, shifting into an immovable defensive stance."
    }
},
{
    "Dragon_Intent_Spell", new string[] {
        "An eerie, ancient glow sparks deep within the Dragon's throat as it channels localized magic.",
        "Runes of primordial fire ignite along the Dragon's spine as it commands the arcane rules of the room."
    }
},
{
    "Dragon_Intent_Dodge", new string[] {
        "Despite its massive size, the Dragon unfurls its wings to lightly hover, ready to dance backwards.",
        "The beast shifts its weight seamlessly, preparing to slide away from your trajectory."
    }
},

// --- DEFENSE RESOLUTION: PLAYER ---
{
    "Player_Block_Mitigate", new string[] {
        "Your guard catches the brunt of the hit, absorbing a chunk of the damage but rattling your armor!"
    }
},
{
    "Player_Block_Neutralize", new string[] {
        "An incredible block! You cleanly stonewall the attack, completely neutralizing the incoming damage!"
    }
},
{
    "Player_Block_Failure", new string[] {
        "CRITICAL DEFENSIVE FAILURE! Your guard collapses completely, leaving you entirely unprotected!"
    }
},
{
    "Player_Dodge_Success", new string[] {
        "You execute a perfect evasion! Slip Counter is activated, maximizing your counter-offensive potency!"
    }
},
{
    "Player_Dodge_Failure", new string[] {
        "Your dodge falls short; you lose your footing and take the hit head-on mid-evasion!"
    }
},

// --- DEFENSE RESOLUTION: DRAGON ---
{
    "Dragon_Block_Mitigate", new string[] {
        "The Dragon's guard mitigates a piece of your attack, but your weapon still cleaves deep into its hide."
    }
},
{
    "Dragon_Block_Neutralize", new string[] {
        "The Dragon perfectly absorbs your attack against its iron scales, taking absolutely no damage!"
    }
},
{
    "Dragon_Block_Failure", new string[] {
        "CRITICAL FAILURE! The Dragon miscalculates its guard, exposing its vulnerable underbelly completely!"
    }
},
{
    "Dragon_Dodge_Success", new string[] {
        "The Dragon fluidly weaves past your strike, eyes flashing as it logs a Slip Counter against you!"
    }
},
{
    "Dragon_Dodge_Failure", new string[] {
        "The beast is too slow to move; your weapon hits the clumsy dragon dead-center mid-flight!"
    }
},

// --- MAGICAL FLOW: PLAYER ---
{
    "Player_Spell_Success_Channeled", new string[] {
        "Your arcane surge overpowers the opposition! You successfully harness the magical flow and become **Channeled**!"
    }
},
{
    "Player_Spell_Overpowered_Empowered", new string[] {
        "Your spell is completely disrupted by a superior force! The ambient magical energy snaps back, making the Dragon **Empowered**!"
    }
},

// --- MAGICAL FLOW: DRAGON ---
{
    "Dragon_Spell_Success_Channeled", new string[] {
        "The Dragon's magical pressure crushes your initiative! The ancient beast absorbs the ambient flow and becomes **Channeled**!"
    }
},
{
    "Dragon_Spell_Overpowered_Empowered", new string[] {
        "You manage to pierce through the Dragon's casting focus! The backfiring arcane feedback leaves you **Empowered**!"
    }
},



{
    "PlayerFlee", new string[] {
        "Terrified and out of your depth, you drop your weapons and bolt through the exit, knowing you stand no chance!"
    }
 },

// --- ENDGAME RESOLUTIONS ---
{
    "PlayerVictory", new string[] {
        "With a final, shattering strike, you pierce the Dragon's heart. The ancient beast lets out a final, agonizing roar before crashing heavily to the stone floor. You stand victorious, the dragon slayer of legend!",
        "The primordial fire within the beast's throat finally flickers out. As the massive dragon collapses into the dust of its own lair, a profound silence falls over the cavern. Against all odds, you have survived and conquered!",
        "Your weapon cleaves cleanly through the creature's final defenses. The Eternal Dragon falls, its hoard is yours, and your name will echo through history for generations to come!"
    }
},
{
    "PlayerDefeat", new string[] {
        "The Dragon's overwhelming fury proves too much to withstand. A devastating blow shatters your guard, sending you collapsing into the darkness as the beast towers over you in triumph.",
        "Your vision fades as the dragon's shadow completely envelops the arena. Your valiant effort ends here, leaving your story as a tragic warning to future adventurers who dare challenge the eternal beast.",
        "With a terrifying roar, the dragon delivers a crushing final impact. Your weapon slips from your hands as you fall to the stone floor—the ancient beast remains undefeated."
    }
}
        };

        private static readonly Random random = new Random();
        public static string GetText(string storyPointer)
        {
            if (!_dialogDatabase.ContainsKey(storyPointer))
            {
                return "The battle rages on, the air trembling from the combatants' ferocity...";
            }
            string[] textOptions = _dialogDatabase[storyPointer];
            int dialogIndex = random.Next(textOptions.Length);
            return textOptions[dialogIndex];
        }


    }
}