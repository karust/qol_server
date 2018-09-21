using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cshit_SERV
{
    //public delegate void NPCClickFuncDel(Player player, NPC ThisNPC);
    //public delegate string NPCCalcAnimDel(NPC ThisNPC);
    //public delegate bool NPCExMarkDel(Player player);


    public class NPC
    {
        int id_Ex = Objects.Last_NpcEX;
        int id;

        bool trade = false;
        string dialogMenuText = "";

        bool vendor=false;

        Item[] goods;
        public int[] prices;

        string name;

        List<string> dialogs;

        float x;
        float y;

        string[] textures;

        public NPC(float X, float Y, string _name, object[,] _goods)
        {
            vendor = true;
            Goods = new Item[_goods.Length / 2];
            prices = new int[_goods.Length / 2];
            for (int i = 0; i < _goods.Length/2; i++) {
                Goods[i] = (Item)_goods[i, 0];
                prices[i] = (int)_goods[i, 1];
            }
            name = _name;
            x = X;
            y = Y;
            id = 1;

            Objects.Last_NpcEX++;
            id_Ex = Objects.Last_NpcEX;
            Objects.AddExNPC(this);
        }

        public NPC(float X, float Y, string _name)
        {
            vendor = true;
            name = _name;
            x = X;
            y = Y;
            id = 0;

            Objects.Last_NpcEX++;
            id_Ex = Objects.Last_NpcEX;
            Objects.AddExNPC(this);
        }

        public void Sell(Player player, int index) {
            Item clone = (Item)goods[index].Clone();
            if(player.Inventory.Take(69, prices[index]))
            player.Inventory.Add(clone);
        }

        public void CheckQuests() {

        }

        public float X
        {
            get
            {
                return x;
            }

            set
            {
                x = value;
            }
        }

        public float Y
        {
            get
            {
                return y;
            }

            set
            {
                y = value;
            }
        }

        public string[] Texture
        {
            get
            {
                return textures;
            }

            set
            {
                textures = value;
            }
        }

        public List<string> Dialogs
        {
            get
            {
                return dialogs;
            }

            set
            {
                dialogs = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public bool Vendor
        {
            get
            {
                return vendor;
            }

            set
            {
                vendor = value;
            }
        }

        public bool Trade
        {
            get
            {
                return trade;
            }

            set
            {
                trade = value;
            }
        }

        public string DialogMenuText
        {
            get
            {
                return dialogMenuText;
            }

            set
            {
                dialogMenuText = value;
            }
        }

        public int[] Prices
        {
            get
            {
                return prices;
            }

            set
            {
                prices = value;
            }
        }

        public Item[] Goods
        {
            get
            {
                return goods;
            }

            set
            {
                goods = value;
            }
        }

        public int Id_Ex
        {
            get
            {
                return id_Ex;
            }

            set
            {
                id_Ex = value;
            }
        }

        public int Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
            }
        }
    }
}
