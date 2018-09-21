using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cshit_SERV
{
    public class Inventory
    {
        Item[] items;
        Player player;
        int width=4;
        int height = 6;
        bool changed = false;

        public Inventory(Item[] _items,Player _player) {
            items = _items;
            player = _player;
        }


        public bool Take(int id,int quantity) {
            Decimal sum=0;
            for (int i = 0; i < Size; i++) {
                if (items[i] != null)
                {
                    if (items[i].Id == id)
                    {
                        sum += items[i].Quantity;
                    }
                }
            }
            if (quantity > sum) return false;
            for (int i = 0; i < Size; i++) {
                if (items[i] != null)
                {
                    if (items[i].Id == id)
                    {
                        if (quantity == items[i].Quantity)
                        {
                            items[i] = null;
                            Objects.DeleteItem(items[i].ID_EX);
                            return true;
                        }
                        if (quantity > items[i].Quantity)
                        {
                            quantity -= items[i].Quantity;
                            items[i] = null;
                            Objects.DeleteItem(items[i].ID_EX);
                        }
                        if (quantity < items[i].Quantity)
                        {
                            items[i].Quantity -= quantity;
                            Objects.UpdateDB_Item(items[i].ID_EX, items[i].Id, items[i].Quantity);
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        public Inventory(Player _player)
        {
            items = new Item[Size];
            player = _player;
        }


        public void Activate(int ID_EX)
        {
            Task.Factory.StartNew(() =>
            {
                //Item item = items.First(p => p.ID_EX == ID_EX);
                int index = int.MaxValue;
                for (int i = 0; i < items.Length; i++)
                    if (items[i].ID_EX == ID_EX)
                    {
                        index = i;
                        break;
                    }
                if (index == int.MaxValue) return;
                //Item item = items.First(p => p.ID_EX == ID_EX);
                items[index].WorkFunc(player);
                if (items[index].QuantityLowCheck())
                {
                    items[index] = null;
                }
            });
        }

        public void Add(Item item)
        {
            if (item.Quantity == 0) return;
            for (int i = 0; i < Size; i++)
            {
                if (items[i] == null) continue;
                if (items[i].Id == item.Id && items[i].Quantity != items[i].MaxQuantity)
                {
                    int quantity = items[i].Quantity;
                    uint sum = (uint)(items[i].Quantity + item.Quantity);
                    if (sum > items[i].MaxQuantity)
                    {
                        int maxQuantity = items[i].MaxQuantity;
                        item.Quantity -= maxQuantity - quantity;
                        items[i].Quantity = maxQuantity;
                        Objects.UpdateDB_Item(items[i].ID_EX, item.Id, items[i].Quantity);
                        Objects.UpdateDB_Item(item.ID_EX, item.Id, item.Quantity);
                        this.Add(item);
                    }
                    else
                    {
                        items[i].Quantity += item.Quantity;
                        Objects.UpdateDB_Item(items[i].ID_EX, item.Id, items[i].Quantity);
                        Objects.DeleteItem(item.ID_EX);
                    }

                    Changed = true;
                    return;
                }
            }

            for (int i = 0; i < Size; i++)
            {
                if (items[i] == null)
                {
                        items[i] = (Item)item.Clone();
                        if (items[i].QuantityHighCheck())
                        {
                            int maxQuantity = items[i].MaxQuantity;
                            item.Quantity -= maxQuantity;
                            items[i].Quantity = maxQuantity;
                            Objects.UpdateDB_Item(items[i].ID_EX, item.Id, items[i].Quantity);
                            Objects.UpdateDB_Item(item.ID_EX, item.Id, item.Quantity);
                            this.Add(item);
                        }
                        Objects.UpdateDB_PlayerInvCell(player.Name, item.ID_EX, i);
                        Changed = true;
                        return;         
                }
            }
            item.DropItem(player.X,player.Y);
        }


        public void Delete(int Invid) {
            items[Invid] = null;
            Changed = true;
        }

        public void Drop(int Invid)
        {
            items[Invid].DropItem(player.X,player.Y);
            items[Invid] = null;
            Changed = true;
        }

        public void DropOne(int Invid) {
            Item drop = (Item)items[Invid].Clone();
            drop.Quantity = 1;
            drop.DropItem(player.X, player.Y);
            items[Invid].Quantity -=1;
            if (items[Invid].QuantityLowCheck()) items[Invid] = null;
            Changed = true;
        }

        public Item[] Items
        {
            get
            {
                return items;
            }

            set
            {
                items = value;
            }
        }

        public int Size
        {
            get
            {
                return Height*width;
            }
        }

        public int Width
        {
            get
            {
                return width;
            }

            set
            {
                width = value;
            }
        }

        public int Height
        {
            get
            {
                return height;
            }

            set
            {
                height = value;
            }
        }

        public bool Changed
        {
            get
            {
                return changed;
            }

            set
            {
                changed = value;
            }
        }
    }
}
