namespace RemnaBotService
{
    internal class CombatDirector 
    {
       
        public bool startGame = false;
        public bool quitGame = false;
        public int playerHP = 500;
        public int dragonHP = 1000;
        public int blockBuff = 0;
        int dragonAtk;
        int playerAtk;
        int dialogIndex;
        // consider adding arrays of alternate texts for different situations
        int damageBump = random.Next(1, 10);
        int[] damageNums = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
        static readonly Random random = new Random();
        int damageDone;
        public string Block()
        {
            dragonAtk = (damageNums[random.Next(damageNums.Length)] + damageBump);
            // use damageDone variable to cut down equations for Console Writing
            damageDone = dragonAtk / 2;
            playerHP -= damageDone;
            blockBuff += 20;
            dialogIndex = random.Next(DialogContainer.BlockStory.Length);
            string blockStory = DialogContainer.BlockStory[dialogIndex];

            // In  the future, lets make an array of different possible dialogue for these
            return $"\n{blockStory} You took {damageDone} damage!\n";
        }

        public string Brave()
        {
            playerAtk = (damageNums[random.Next(damageNums.Length)] + damageBump + blockBuff);
            dragonAtk = (damageNums[random.Next(damageNums.Length)] + damageBump);
            if (playerAtk > dragonAtk)
            {
                damageDone = playerAtk + (dragonAtk / 2);
                dragonHP -= damageDone;
                // we set block buff to 0 so that it resets after being used. If you still dont win the turn with block buff it does not reset
                blockBuff = 0;
                dialogIndex = random.Next(DialogContainer.BraveSuccessStory.Length);
                string braveStory = DialogContainer.BraveSuccessStory[dialogIndex];
                return $"\n{braveStory} You dealt {damageDone} damage to the Dragon!";


            }
            else if (playerAtk < dragonAtk)
            {
                damageDone = dragonAtk;
                playerHP -= damageDone;
                dialogIndex = random.Next(DialogContainer.BraveFailureStory.Length);
                string braveStory = DialogContainer.BraveFailureStory[dialogIndex];
                return $"\n{braveStory} You took {damageDone} damage!";



            }
            else
            {
                dialogIndex = random.Next(DialogContainer.ClashStory.Length);
                string clashStory = DialogContainer.ClashStory[dialogIndex];
                return $"\n{clashStory} No damage done!";

            }
        }

        public string HPCheck()
        {

            if (playerHP <= 0)
            {
                startGame = false;
                quitGame = true;
                int dialogIndex = random.Next(DialogContainer.DefeatStory.Length);
                string defeat = DialogContainer.DefeatStory[dialogIndex];
                return $"{defeat}\nYour HP: {playerHP} | Dragon's HP: {dragonHP}";
            }
            else if (dragonHP <= 0)
            {
                startGame = false;
                quitGame = true;
                int dialogIndex = random.Next(DialogContainer.VictoryStory.Length);
                string victory = DialogContainer.VictoryStory[dialogIndex];
                return $"{victory}\nYour HP: {playerHP} | Dragon's HP: {dragonHP}";
            }
            else
            {
                return "The battle continues...";
            }

            
        }

    /* For Discord Only
     
        public void DragonSet()
        {
            dragonHP = 0;
            bool isInt;
            do
            {
                Console.Write("Dragon's HP: ");
                string playerInput = Console.ReadLine().ToUpper();
                isInt = int.TryParse(playerInput, out int result);
                if (result <= 0 || !isInt)
                {
                    Console.WriteLine("Invalid input, please enter a number greater than 0");

                }
                dragonHP = result;
            }
            while (dragonHP <= 0 || !isInt);


        }
            
        public void PlayerSet()
        {
            playerHP = 0;
            bool isInt;
            do
            {
                Console.Write("Your HP: ");
                string playerInput = Console.ReadLine().ToUpper();
                isInt = int.TryParse(playerInput, out int result);
                if (result <= 0 || !isInt)
                {
                    Console.WriteLine("Invalid input, please enter a number greater than 0");

                }
                playerHP = result;
            }
            while (playerHP <= 0 || !isInt);
      

        }

      */





        public void Reset()
        {
            playerHP = 500;
            dragonHP = 1000;
            blockBuff = 0;
        }

    }
}