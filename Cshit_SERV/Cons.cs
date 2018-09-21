using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Cshit_SERV
{
    class Cons
    {
        public static IPort ToIport(IPEndPoint remote) { return new IPort(remote.Address.ToString(), remote.Port); }

        /// <summary>
        /// Просто отсекается 1ый байт
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetData(byte[] data)
        {
            byte[] nameB = new byte[data.Length - 1];
            Array.Copy(data, 1, nameB, 0, data.Length - 1);
            return Encoding.UTF8.GetString(nameB);
        }

        /// <summary>
        /// Изьятия имени из пакетов XYD (с 10го байта по конец)
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        public static string GetName18b(byte[] pack)
        {
            byte[] nameB = new byte[pack.Length - 18];
            Array.Copy(pack, 18, nameB, 0, pack.Length - 18);
            return Encoding.UTF8.GetString(nameB);
        }

        public static string GetName14b(byte[] pack)
        {
            byte[] nameB = new byte[pack.Length - 14];
            Array.Copy(pack, 14, nameB, 0, pack.Length - 14);
            return Encoding.UTF8.GetString(nameB);
        }

        //Заносим в координаты и имена других персов которые нам передали
        public static void SetPlayer_XYD(ref Objects obj, byte[] data)
        {
            //Копирую имя из пакета
            string name = GetName14b(data);

            byte[] r = new byte[4];
            byte[] d = new byte[1];

            Array.Copy(data, 9, r, 0, 4);
            Array.Copy(data, 13, d, 0, 1);

            float R = BitConverter.ToSingle(r, 0);
            byte D = d[0];
            try
            {
                var entrance = obj.players.First(p => p.Name == name);
                entrance.Rotation = R;
                entrance.Direction = D;

            }
            catch (Exception e)
            {
                Console.WriteLine("SetPlayer_XYD: " + e.Message);
            }
        }

        public static void UseItem(ref Player player, byte[] data)
        {
            byte[] IDb = new byte[4];
            byte[] ID_EXb = new byte[4];

            try
            {
                Array.Copy(data, 1, IDb, 0, 4);
                Array.Copy(data, 5, ID_EXb, 0, 4);

                int id = BitConverter.ToInt32(IDb, 0);
                int id_ex = BitConverter.ToInt32(ID_EXb, 0);
                //Console.WriteLine("Item usage: ID = " + id + ", EX = " + id_ex);
                switch (id)
                {
                    case 0:
                        player.Inventory.Activate(id_ex);
                        break;
                }
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
        }

        public static void Buy(ref Player player, byte[] data)
        {
            byte[] IDb = new byte[4];
            Array.Copy(data, 1, IDb, 0, 4);
            int id = BitConverter.ToInt32(IDb, 0);

            switch (id)
            {
                case 0:
                    if (player.Inventory.Take(69, 100)) player.Inventory.Add(new Potion(PotionType.Health, 1));
                    break;
                case 1:
                    if (player.Inventory.Take(69, 100)) player.Inventory.Add(new Potion(PotionType.Energy, 1));
                    break;
                case 2:
                    if (player.Inventory.Take(69, 100)) player.Inventory.Add(new Potion(PotionType.Speed, 1));
                    break;
            }
        }

        //12		TAKE_QUEST 	| 1-4[Q_ID] 5-8[NPC_ID]
        public static void QuestHandle(ref Player player, byte[] data)
        {
            byte[] IDb = new byte[4];
            byte[] NPC_IDb = new byte[4];
            Array.Copy(data, 1, IDb, 0, 4);
            Array.Copy(data, 5, NPC_IDb, 0, 4);

            int Q_Id = BitConverter.ToInt32(IDb, 0);
            int NPC_Id = BitConverter.ToInt32(NPC_IDb, 0);

            switch (Q_Id)
            {
                case 1:
                    Quest quest = player.Quests.Find(q => q.Id == Q_Id);
                    if(quest.State != quest.FinishState) quest.StateUp();
                    break;
            }
        }
    }
}
