using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemnaBotService.Eternal_Dragon
{
    public abstract class BaseDragon
    {
        public int playerHP = 500;
        public int dragonHP = 1000;
        public int blockBuff = 0;
        int dragonAtk;
        int playerAtk;
    }
}
