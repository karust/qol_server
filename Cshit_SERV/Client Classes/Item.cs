using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cshit_SERV
{
    public enum PotionType {
        Health,
        Energy,
        Speed
    }

    public class Item : ICloneable
    {
        static List<Item> drop;    

        public static int counter = Objects.Last_ItemEX;

        float x=0, y=0;
        bool dropped=false;
        bool taken = false;

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        int id;

        virtual public void WorkFunc(Player player) {}

        private int maxQuantity;
        int quantity = 0;

        int Id_ex = 0;

        private string desc;

        private string name;
        private float nameR, nameG, nameB;

        private string texture;

        public void DropItem(float X, float Y)
        {
            x = X;
            y = Y;
            dropped = true;
            taken = false;
            drop.Add(this);
        }

        public bool QuantityLowCheck() {
            if (quantity <= 0)  return true; 
            return false;
        }

        public bool QuantityHighCheck()
        {
            if (quantity >= MaxQuantity) return true;
            return false;
        }



        public int Quantity
        {
            get
            {
                return quantity;
            }

            set
            {
                quantity = value;
            }
        }

        public string Texture
        {
            get
            {
                return texture;
            }

            set
            {
                texture = value;
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

        public int MaxQuantity
        {
            get
            {
                return maxQuantity;
            }

            set
            {
                maxQuantity = value;
            }
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

        public bool Dropped
        {
            get
            {
                return dropped;
            }

            set
            {
                if (value == true) Taken = false;
                dropped = value;
            }
        }

        static public List<Item> Drop
        {
            get
            {
                return drop;
            }

            set
            {
                drop = value;
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

        public float NameR
        {
            get
            {
                return nameR;
            }

            set
            {
                nameR = value;
            }
        }

        public float NameG
        {
            get
            {
                return nameG;
            }

            set
            {
                nameG = value;
            }
        }

        public float NameB
        {
            get
            {
                return nameB;
            }

            set
            {
                nameB = value;
            }
        }

        public string Desc
        {
            get
            {
                return desc;
            }

            set
            {
                desc = value;
            }
        }

        public int ID_EX
        {
            get
            {
                return Id_ex;
            }

            set
            {
                Id_ex = value;
            }
        }

        public bool Taken
        {
            get
            {
                return taken;
            }

            set
            {
                taken = value;
            }
        }
    }

    public class Gold : Item
    {
        public Gold(float X, float Y, int quantity)
        {
            this.ID_EX = counter++;
            base.X = X;
            base.Y = Y;
            base.Id = 69;
            base.MaxQuantity = int.MaxValue;
            base.Dropped = true;
            base.Quantity = quantity;
            base.Name = "Gold";
            base.Desc = "Makes you happy?";
            base.NameR = 1;
            base.NameG = 1;
            base.NameB = .3f;
            base.Texture = "ItemGold";

            Objects.AddDB_Item(ID_EX, Id, quantity);
            
        }


        public Gold(int quantity)
        {
            this.ID_EX = counter++;
            base.Id = 69;
            base.MaxQuantity = int.MaxValue;
            base.Quantity = quantity;
            base.Name = "Gold";
            base.Desc = "Makes you happy?";
            base.NameR = 1;
            base.NameG = 1;
            base.NameB = .3f;
            base.Texture = "ItemGold";

            Objects.AddDB_Item(ID_EX, Id, quantity);
        }

        public Gold(int quantity, bool quest)
        {
            base.Id = 69;
            base.MaxQuantity = int.MaxValue;
            base.Quantity = quantity;
            base.Name = "Gold";
            base.Desc = "Makes you happy?";
            base.NameR = 1;
            base.NameG = 1;
            base.NameB = .3f;
            base.Texture = "ItemGold";
        }
    }

    public class Potion : Item
    {
        PotionType potionType;

        public Potion(PotionType _potionType, int quantity, float x, float y)
        {
            this.ID_EX = counter++;
            base.Dropped = true;
            base.X = x;
            base.Y = y;
            potionType = _potionType;
            base.MaxQuantity = 20;
            base.Quantity = quantity;
            if (potionType == PotionType.Health)
            {
                base.Name = "Health Potion";
                base.Desc = "Restore 20HP";
                base.NameR = .1f;
                base.NameG = 1f;
                base.NameB = .1f;
                base.Texture = "ItemHP0";
                Id = 0;

                Objects.AddDB_Item(ID_EX, Id, quantity);
            }
            if (potionType == PotionType.Speed)
            {
                base.Name = "Speed Potion";
                base.Desc = "Restore 20HP";
                base.NameR = .1f;
                base.NameG = 1f;
                base.NameB = .1f;
                base.Texture = "ItemSpeed0";
                Id = 1;

                Objects.AddDB_Item(ID_EX, Id, quantity);
            }
            if (potionType == PotionType.Energy)
            {
                base.Name = "Energy Potion";
                base.Desc = "Restore 20HP";
                base.NameR = .1f;
                base.NameG = 1f;
                base.NameB = .1f;
                base.Texture = "ItemEnergy0";
                Id = 2;

                Objects.AddDB_Item(ID_EX, Id, quantity);
            }
        }


        public Potion(PotionType _potionType, int quantity)
        {
            this.ID_EX = counter++;
            base.Dropped = false;
            potionType = _potionType;
            base.MaxQuantity = 20;
            base.Quantity = quantity;
            if (potionType == PotionType.Health)
            {
                base.Name = "Health Potion";
                base.Desc = "Restore 20HP";
                base.NameR = .1f;
                base.NameG = 1f;
                base.NameB = .1f;
                base.Texture = "ItemHP0";
                Id = 0;

                Objects.AddDB_Item(ID_EX, Id, quantity);
            }
            if (potionType == PotionType.Speed)
            {
                base.Name = "Speed Potion";
                base.Desc = "Restore 20HP";
                base.NameR = .1f;
                base.NameG = 1f;
                base.NameB = .1f;
                base.Texture = "ItemSpeed0";
                Id = 1;

                Objects.AddDB_Item(ID_EX, Id, quantity);
            }
            if (potionType == PotionType.Energy)
            {
                base.Name = "Energy Potion";
                base.Desc = "Restore 20HP";
                base.NameR = .1f;
                base.NameG = 1f;
                base.NameB = .1f;
                base.Texture = "ItemEnergy0";
                Id = 2;

                Objects.AddDB_Item(ID_EX, Id, quantity);
            }
        }

        public override void WorkFunc(Player player)
        {
                if (potionType == PotionType.Health)
                {
                    if (player.Alive)
                    {
                        player.Hp += 20;
                        if (player.Hp > player.MaxHp + player.HpUp) player.Hp = player.MaxHp + player.HpUp;
                        base.Quantity--;
                    }
                }
        }
    }
}
