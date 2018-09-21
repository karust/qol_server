using System;
using System.Collections.Generic;
using System.Net;
using System.Diagnostics;
using System.Data.SQLite;

namespace Cshit_SERV
{
    class Cmd
    {
        public byte cmd = 0;
        public byte[] datap = new byte[256];
        public bool changed = false;

        public bool changedP
        {
            get
            {
                bool copy = changed;
                changed = false;
                return copy;
            }
        }
       

        public byte[] data
        {
            get
            {
                return datap;
            }
            set
            {
                datap = value;
            }
        }
    }

    //Наследованный класс, с таймингом пакетов
    class CmdT : Cmd
    {
        new public byte[] datap = new byte[256];
        public float oldX = 0, oldY = 0;
        private float x = 63F, y = 60F;
        public float average = 0F;
        long count = 1;
        
        Stopwatch timer;

        public CmdT()
        {
            timer = Stopwatch.StartNew();
        }

        public long time
        {
            get
            {
                return timer.ElapsedMilliseconds;
            }
        }

        new public byte[] data
        {
            get
            {
                return datap;
            }
            set
            {            
                count++;
                if (count == 11) { count = 1; average = 0F; }
                timer.Restart();
                datap = value;
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
                oldX = x;       
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
                oldY = y;
                y = value;
            }
        }

        public long Count
        {
            get
            {
                return count;
            }

            set
            {
                count = value;
            }
        }
    }

    class Comands
    {
        Cmd CHECKp = new Cmd();
        Cmd BEGINp = new Cmd();
        Cmd ENDp = new Cmd();
        Cmd ALIVEp = new Cmd();
        CmdT SET_XYDp = new CmdT();
        Cmd HITp = new Cmd();

        public Cmd HIT
        {
            get { return HITp; }
            set { HITp = value; }
        }

        public Cmd CHECK
        {
            get { return CHECKp; }
            set { CHECKp = value; }
        }
        public Cmd BEGIN
        {
            get { return BEGINp; }
            set { BEGINp = value; }
        }
        public Cmd END
        {
            get { return ENDp; }
            set { ENDp = value; }
        }
        public Cmd ALIVE
        {
            get { return ALIVEp; }
            set { ALIVEp = value; }
        }
        public CmdT SET_XYD
        {
            get { return SET_XYDp; }
            set { SET_XYDp = value; }
        }
    }

    class Char_Stat
    {
        public Comands packetp = new Comands();
        public string name = "def";
        public string IP = "";
        public int port = 0;
        public bool online = false , finding_thread = false, client_informed = false;
        public int ID = 0;
       
        public Char_Stat(){}
              
        public Comands packet
        {
            get
            {
                return packetp;
            }
            set
            {
                packetp = value;
            }
        }

        public IPEndPoint Addr
        {
            get
            {
                return new IPEndPoint(IPAddress.Parse(IP), port);
            }
        }

    }


    class Objects
    {
        //Переменная в которую заносится последний номер экземпляра предмета в БД
        static public int Last_ItemEX;
        static public int Last_QuestEX;
        static public int Last_NpcEX;

        //Временные данные персонажа связаныне с пакетами, онлайном
        public Dictionary<string, Char_Stat> _Char;

        public Random rnd = new Random();

        public List<Player> players;
        public Enemy[] mobs;
        public List<Item> items;
        public NPC[] NPCs;

        public int[,] map;
        public int[,] objects_map;
        public byte[,] collision_map;

        static SQLiteConnection db_conn;

        public Objects(int MAX_USERS)
        {
            db_conn = new SQLiteConnection("Data Source=Game_DB.db;Version=3;");
            db_conn.Open();
            Last_ItemEX = LastEX("Items");
            Last_QuestEX = LastEX("Quests");
            Last_NpcEX = LastEX("Npc_Ex");

            _Char = new Dictionary<string, Char_Stat>(MAX_USERS);
            players = new List<Player>(MAX_USERS);

            NPCs = new NPC[100];
            SpawnNPC(NPCs);

            mobs = new Enemy[1000];
            SpawnMobs(mobs);

            items = new List<Item>(1000);
            Item.Drop = items;

            Collision_Map();

            Console.Title = "Server: Running";
        }

        //Спаун мобов по карте мобов
        public void SpawnMobs(Enemy[] mobs)
        {
            bool[] Mobs = new bool[100];
            for (int i = 0; i < Mobs.Length; i++) Mobs[i] = false;

            Map mob_map = new Map("mobs.bmp");
            int[,] digit_mobs = mob_map.MapArray();
            int n_mob = 0;

            for (int i = 0; i < mob_map.X; i++)
                for (int j = 0; j < mob_map.Y; j++)
                    if (digit_mobs[i, j] > 0)
                    {
                        switch (digit_mobs[i, j])
                        {
                            case 1:
                                mobs[n_mob] = new Ghost(i, j, rnd);
                                if (!Mobs[1]) { AddEnemy(mobs[n_mob]); Mobs[1] = true; }
                                n_mob++;
                                break;
                        }
                        Console.Title = "NPC Spawn: " + n_mob;
                    }
            Console.Title = "NPC Spawn: Done";
        }

        //Спаун НПЦ
        public void SpawnNPC(NPC[] npc)
        {
            NPCs[0] = new NPC(76, 63, "Andrea");
            AddNPC(NPCs[0]);

            object[,] Goodies = new object[,] { { new Potion(PotionType.Health, 1), 25 }, { new Potion(PotionType.Energy, 1), 50 } };
            NPCs[1] = new NPC(78, 60, "Vendor", Goodies);
            AddNPC(NPCs[1]);
        }

        //Загрузка карты коллизии
        public void Collision_Map()
        {
            Map MapReader = new Map("map.bmp");
            map = MapReader.MapArray();
            Map ObjMapReader = new Map("obj_map.bmp");
            objects_map = ObjMapReader.MapArray();
            collision_map = new byte[ObjMapReader.X, ObjMapReader.Y];
            for (int i = 0; i < ObjMapReader.X; i++)
            {
                for (int j = 0; j < ObjMapReader.Y; j++)
                {
                    if (map[i, j] == 3 || map[i, j] == 4 || map[i, j] == 5) collision_map[i, j] = 1; else collision_map[i, j] = 0;
                }
            }

        }

        //Инициализация персонажа информацией из бд, если ее нет, то заносится туда новый
        public bool Initialize_Player(string name)
        {
            //Если такой объект существует, удаляем
            if (players.Exists(p => p.Name == name))
            {            
                var player = players.Find(p => p.Name == name);
                players.Remove(player);
            }

            //Ищем данного игрока в БД
            string find_player = "select * from Characters where Name = '"+ name +"';";
            SQLiteCommand command = new SQLiteCommand(find_player, db_conn);
            SQLiteDataReader reader = command.ExecuteReader();

            //Если игрок есть в БД
            if (reader.Read())
            { 
                players.Add(new Player(Convert.ToSingle(reader["X"]), Convert.ToSingle(reader["Y"]), name));
                var player = players.Find(p => p.Name == name);
                player.level = Convert.ToInt32(reader["Lvl"]);
                player.MaxHp = Convert.ToInt32(reader["Max_HP"]);
                player.Hp = Convert.ToInt32(reader["HP"]);
                player.Speed = Convert.ToSingle(reader["Speed"]);
                player.AtkDmg = Convert.ToInt32(reader["Attack"]);
                //Тип респаун
                if (player.Hp <= 0) { player.Hp = 100; }

                //Получаем итемы
                int[] inv_items = Get_PlayerDB_Inventory(name);

                int total = 0;
                for (total = 0; inv_items[total] != int.MinValue; total++);
                for (int i = 0; i < total / 3; i++)
                {
                    switch (inv_items[(i * 3) + 1])
                    {
                        case 0:
                            Item hp = new Potion(PotionType.Health, inv_items[(i * 3) + 2]);
                            DeleteItem(hp.ID_EX);
                            hp.ID_EX = inv_items[i * 3];
                            player.Inventory.Add(hp);
                            break;
                        case 69:
                            Item gold = new Gold(inv_items[(i * 3) + 2]);
                            DeleteItem(gold.ID_EX);
                            gold.ID_EX = inv_items[i * 3];
                            player.Inventory.Add(gold);
                            break;
                    }  
                }

                //Получаем квесты
                int[] quests = Get_PlayerDB_Quests(name);

                for (total = 0; quests[total] != int.MinValue; total++);
                for (int i = 0; i < total / 4; i++)
                {
                    switch (quests[(i*4)+1])
                    {
                        case 1:
                            player.Quests.Add(new DarkSignsQuest(quests[(i * 4) + 0], quests[(i * 4) + 2], quests[(i * 4) + 3]));
                            break;
                    }
                }
            }
            else
            {
                players.Add(new Player(65F, 55F, name));
                var player = players.Find(p => p.Name == name);
                    string new_player = "insert into Characters (Name, Lvl, Max_HP, HP, X, Y, Speed, Attack) values ('"
                    + name +"'," + player.level + "," + player.Hp + "," +  player.MaxHp + "," + player.X + "," + player.Y + "," + player.Speed + "," + player.AtkDmg + ");";
                command = new SQLiteCommand(new_player, db_conn);
                command.ExecuteNonQuery();

                //Инициализируем квесты
                player.Initialize_Quests();

                //Создаем его инвентарь
                int Char_ID = Get_PlayerDB_ID(name);
                string add_inv = "insert into Inventory (Char_ID) values (" + Char_ID + ");";
                command = new SQLiteCommand(add_inv, db_conn);
                command.ExecuteNonQuery();

                player.Inventory.Add(new Gold(100000));
                player.Inventory.Add(new Potion(PotionType.Health, 20));
            }
            return true;
        }

        //Загрузка инфы о персонаже в БД
        public bool UpdateDB_Player(string name)
        {
            var player = players.Find(p => p.Name == name);

            string update_player = "update Characters set Lvl =" + player.level
                                    + ", HP =" + (int)player.Hp
                                    + ", Max_HP =" + (int)player.MaxHp
                                    + ", X =" + (int)player.X
                                    + ", Y =" + (int)player.Y
                                    + " where Name='" + name + "';";
            SQLiteCommand command = new SQLiteCommand(update_player, db_conn);
            command.ExecuteNonQuery();

            return true;
        }

        //Получить ID персонажа из БД
        static public int Get_PlayerDB_ID(string name)
        {
            string find_id = "select Char_ID from Characters where Name = '" + name + "';";
            SQLiteCommand command = new SQLiteCommand(find_id, db_conn);
            SQLiteDataReader reader = command.ExecuteReader();

            //Если игрок есть в БД
            if (reader.Read())
            {
                return Convert.ToInt32(reader[0]);
            }
            else return -1;
        }

        //Заносим в Items новый предмет
        static public bool AddDB_Item(int ID_EX, int ID, int Q)
        {
            string insert_item = "insert into Items (ID_EX, Item_ID, Quantity) values("
                    + ID_EX + "," + ID + "," + Q + ");";
            SQLiteCommand command = new SQLiteCommand(insert_item, db_conn);
            command.ExecuteNonQuery();
            return true;
        }

        //Заносим в Items новый предмет
        static public bool UpdateDB_Item(int ID_EX, int ID, int Q)
        {
            string insert_item = "UPDATE Items"
                + " SET Item_ID = "+ID + ", Quantity = "+Q
                + " WHERE ID_EX = "+ID_EX +";";
            SQLiteCommand command = new SQLiteCommand(insert_item, db_conn);
            command.ExecuteNonQuery();
            return true;
        }

        //Удалить экземпляр предмета из таблицы Items
        static public bool DeleteItem(int ID_EX)
        {
            string delete_item = "DELETE FROM Items WHERE ID_EX = " + ID_EX + ";";
            SQLiteCommand command = new SQLiteCommand(delete_item, db_conn);
            command.ExecuteNonQuery();

            return true;
        }

        //Находит номер последнего экземпляря предмета в БД, чтобы начинать отсчет с него
        public int LastEX(string table)
        {
            string find_last = "SELECT MAX(ID_EX) FROM " + table + ";";
            SQLiteCommand command = new SQLiteCommand(find_last, db_conn);
            SQLiteDataReader reader = command.ExecuteReader();
            try { if (reader.Read()) return Convert.ToInt32(reader[0]) + 1; }
            catch (InvalidCastException) { return 0; }
            return 0;
        }

        //Получаем вещи в инвентаре 
        static public int[] Get_PlayerDB_Inventory(string player_name)
        {
            int[] IdEX_ID_Q = new int[21 * 3];
            for (int i = 0; i < 21 * 3; i++) { IdEX_ID_Q[i] = int.MinValue; }

            int ID = Get_PlayerDB_ID(player_name);

            for (int i = 0; i < 21; i++)
            {
                string get_IdEX = "select Cell_"+i + " from Inventory where Char_ID = " + ID + " AND Cell_"+i+ " IS NOT NULL;";
                SQLiteCommand command = new SQLiteCommand(get_IdEX, db_conn);
                SQLiteDataReader reader = command.ExecuteReader();
                //Записываю Экземпляры в каждую 3ую позицию
                if (reader.Read()) IdEX_ID_Q[i*3] = Convert.ToInt32(reader[0]);

                string get_ID_Q = "select Item_ID, Quantity from Items where ID_EX = " + IdEX_ID_Q[i * 3] + ";";
                command = new SQLiteCommand(get_ID_Q, db_conn);
                reader = command.ExecuteReader();
                //Записываю Экземпляры в каждую 3ую позицию
                if (reader.Read())
                {
                    IdEX_ID_Q[(i * 3) + 1] = Convert.ToInt32(reader[0]);
                    IdEX_ID_Q[(i * 3) + 2] = Convert.ToInt32(reader[1]);
                }
                else break;
            }
                return IdEX_ID_Q;
        }

        //Обновляем в указанной ячейке Inventory новый предмет 
        static public bool UpdateDB_PlayerInvCell(string name, int ID_EX, int Cell)
        {
            string update_inventory = "UPDATE Inventory"
                                    + " SET Cell_" + Cell + "=" + ID_EX
                                    + " WHERE Char_ID = (SELECT Char_ID FROM Characters WHERE Name = '" + name + "');";
                                    
            SQLiteCommand command = new SQLiteCommand(update_inventory, db_conn);
            command.ExecuteNonQuery();
            return true;
        }

        //Заносим в Quests новый quest
        static public bool AddDB_Quest(string Char, int ID_EX, int Quest_ID, int Reward, int Stage = 0, int Count = 0)
        {
            int player_Id = Get_PlayerDB_ID(Char);

            string insert_item = "insert into Quests (ID_EX, Quest_ID, Stage, Count, Reward_ID) values("
                    + ID_EX + ","+ Quest_ID + "," + Stage + "," + Count + "," + Reward +");";
            SQLiteCommand command = new SQLiteCommand(insert_item, db_conn);
            command.ExecuteNonQuery();

            insert_item = "insert into Char_Quests (Char_ID, Quest_EX) values("
                    + player_Id + "," + ID_EX + ");";
            command = new SQLiteCommand(insert_item, db_conn);
            command.ExecuteNonQuery();
            return true;
        }

        //Update в Quests
        static public bool UpdateDB_Quest(int ID_EX, int Quest_ID, int Stage, int Count)
        {
            string update_item = "UPDATE Quests"
                + " SET Stage = " + Stage + ", Count = " + Count
                + " WHERE ID_EX = " + ID_EX + ";";
            SQLiteCommand command = new SQLiteCommand(update_item, db_conn);
            command.ExecuteNonQuery();
            return true;
        }

        //Getting player Quests from DB
        static public int[] Get_PlayerDB_Quests(string player_name)
        {
            int[] Quests = new int[4*5];
            for (int i = 0; i < 4 * 5; i++)  Quests[i] = int.MinValue;

            int player_id = Get_PlayerDB_ID(player_name);
            int total = 0;

            string get_QuestIDs = "select Quest_EX from Char_Quests where Char_ID = " + player_id + ";";
            SQLiteCommand command = new SQLiteCommand(get_QuestIDs, db_conn);
            SQLiteDataReader reader = command.ExecuteReader();

            //Каждая 4 позиция это Quest_EX
            if (reader.Read())
                for (total = 0; total < reader.FieldCount; total++)
                    Quests[total * 4] = Convert.ToInt32(reader[total]);
            else return Quests = new int[]{ 0 };

            for (int i = 0; i < total; i++)
            {
                string get_other = "select Quest_ID, Stage, Count from Quests where ID_EX = " + Quests[i * 4] + ";";
                command = new SQLiteCommand(get_other, db_conn);
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    Quests[(i * 4) + 1] = Convert.ToInt32(reader[0]);
                    Quests[(i * 4) + 2] = Convert.ToInt32(reader[1]);
                    Quests[(i * 4) + 3] = Convert.ToInt32(reader[2]);
                }
                else break;
            }
            return Quests;
        }

        static public bool AddNPC(NPC npc)
        {
            string add_npc = "insert into NPC (NPC_Name, Agr, Max_HP, Attack, Speed) values('"
                    + npc.Name + "'," + 0 + "," + 0 + "," + 0 + "," + 0 + ");";

            SQLiteCommand command = new SQLiteCommand(add_npc, db_conn);
            command.ExecuteNonQuery();
            return true;
        }

        static public bool AddEnemy(Enemy enemy)
        {
            string add_npc = "insert into NPC (NPC_Name, Agr, Max_HP, Attack, Speed) values('"
                    + enemy.Name + "'," + 1 + "," + enemy.MaxHp + "," + enemy.Attack + "," + enemy.Speed + ");";

            SQLiteCommand command = new SQLiteCommand(add_npc, db_conn);
            command.ExecuteNonQuery();
            return true;
        }

        static public bool AddExNPC(NPC npc)
        {
            string add_npcEx = "insert into NPC_Ex (ID_EX, Name, X, Y, HP) values("
                                     +  npc.Id_Ex + ",'" + npc.Name + "', " + npc.X + ", " + npc.Y + ", " + 0 + ");";

            SQLiteCommand command = new SQLiteCommand(add_npcEx, db_conn);
            command.ExecuteNonQuery();
            return true;
        }

        static public bool AddExEnemy(Enemy enemy)
        {
            string add_npcEx = "insert into NPC_Ex (ID_EX, Name, X, Y, HP) values("
                                     + enemy.Id_Ex + ",'" + enemy.Name + "'," + enemy.X + "," + enemy.Y + "," + enemy.HP + ");";

            SQLiteCommand command = new SQLiteCommand(add_npcEx, db_conn);
            command.ExecuteNonQuery();
            return true;
        }

        static public bool UpdateExNPC(NPC npc)
        {
            string Update_NPC_Ex = "UPDATE NPC_Ex"
                                   + " SET X = " + npc.X + ", Y = " + npc.Y + ", HP = " + 0
                                   + " WHERE ID_EX = '" + npc.Id_Ex + "');";

            SQLiteCommand command = new SQLiteCommand(Update_NPC_Ex, db_conn);
            command.ExecuteNonQuery();
            return true;
        }

        static public bool UpdateExEnemy(Enemy enemy)
        {
            string Update_NPC_Ex = "UPDATE NPC_Ex"
                                   + " SET X = " + enemy.X + ", Y = " + enemy.Y + ", HP = " + enemy.HP
                                   + " WHERE ID_EX = " + enemy.Id_Ex + ";";

            SQLiteCommand command = new SQLiteCommand(Update_NPC_Ex, db_conn);
            command.ExecuteNonQuery();
            return true;
        }

        /// <summary>
        /// Получить Agr, Max_Hp, Attack, Speed
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static public int[] Get_NPC_Info(string name)
        {
            int[] info = new int[5];
            string find_info = "select Agr, Max_Hp, Attack, Speed from NPC where NPC_Name = '" + name + "';";
            SQLiteCommand command = new SQLiteCommand(find_info, db_conn);
            SQLiteDataReader reader = command.ExecuteReader();

            if (reader.Read())
            {
                info[0] = Convert.ToInt32(reader[0]);
                info[1] = Convert.ToInt32(reader[1]);
                info[2] = Convert.ToInt32(reader[2]);
                info[3] = Convert.ToInt32(reader[3]);
            }

            return info;
        }

        /// <summary>
        /// Получить X, Y, HP
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static public int[] Get_NPC_EX_Info(int id, string name)
        {
            int[] info = new int[3];
            string find_info = "select X, Y, HP from NPC_Ex where Name = '" + name + "' AND NPC_Ex_ID = " + id + ";";
            SQLiteCommand command = new SQLiteCommand(find_info, db_conn);
            SQLiteDataReader reader = command.ExecuteReader();

            if (reader.Read())
            {
                info[0] = Convert.ToInt32(reader[0]);
                info[1] = Convert.ToInt32(reader[1]);
                info[2] = Convert.ToInt32(reader[2]);
            }

            return info;
        }

    }
}
