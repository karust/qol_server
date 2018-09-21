using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Net.NetworkInformation;

namespace Cshit_SERV
{
    struct IPort
    {
       public string ip;
       public int port;

        public IPort(string IP, int PORT)
        {
            ip = IP;
            port = PORT;
        }

        public string IP
        { get { return ip; } }

        public int PORT
        { get { return port; } }

    }
    class Game
    {
        public Objects obj;
        Thread listener;
        UdpClient game_listener;
        IPEndPoint remoteIp;
        
        bool game_up = false;

        static int port = 0;
        static int max_users = 100;
        public int total_players = 0;

        List<Thread> session_threads = new List<Thread>();
        Thread per_disconn;
        Task mobsTh;

        //Для механизма важных сообщений, а также фильтра входящих сообщений -  Аддрес клиента:Последнее Сообщение
        Dictionary<IPort, string> clients_iport = new Dictionary<IPort, string>(max_users);

        Dictionary<IPort, byte[]> drop_Handle = new Dictionary<IPort, byte[]>(max_users);
        Dictionary<IPort, byte[]> item_Used = new Dictionary<IPort, byte[]>(max_users);
        Dictionary<IPort, byte[]> grocery_buy = new Dictionary<IPort, byte[]>(max_users);
        Dictionary<IPort, byte[]> quest_Handle = new Dictionary<IPort, byte[]>(max_users);

        public Game(int PORT, int MAX_USERS)
        {
            port = PORT;
            max_users = MAX_USERS;
            obj = new Objects(MAX_USERS);

            for (int i = 0; i < max_users; i++)
                session_threads.Add(new Thread(new ParameterizedThreadStart(Session)));
        }

        public bool Serv_Up()
        {
            if (game_up) { Console.WriteLine("Game server: already UP"); return false; }
            try
            {
                game_up = true;
                remoteIp = null;
                //Задаем порт для прослушивания сообщений для гейм-серва
                game_listener = new UdpClient(port);
                //Избавление от ошибки 10054 (the remote host doesn't have a listener on that port, and bounces an ICMP host unreachable message in response)
                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                game_listener.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                //Начинаем слушать входящие сообщение
                listener = new Thread(Listen_Clients);
                listener.Start();
                //Старт удаления неактивных персов
                per_disconn = new Thread(new ThreadStart(Period_Disconnect));
                per_disconn.Start();
                //Обработка мобов
                mobsTh = new Task(Mobs);
                mobsTh.Start();

                Console.WriteLine("Game server: started");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Game server: Failed to START - " + e.Message);
                return false;
            }
        }

        public bool Serv_Down()
        {
            if (!game_up) { Console.WriteLine("Game server: you must UP server first"); return false; }
            try
            {
                game_up = false;
                if (game_listener != null) game_listener.Close();
                //Stop reading messeages
                listener.Abort();
                //Перестаем дисконнектить
                per_disconn.Join(1);
                //Стоп обработки мобов
                mobsTh.Wait(1);
                for (int i = 0; i < max_users; i++)
                    session_threads[i].Abort();

                Console.WriteLine("Game sever: stopped");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Game server: Failed to STOP - " + e.Message);
                return false;
            }
        }

        //Метод для нахождения и запуска свободного потока в листе
        public int Find_thSpace(string char_name, IPEndPoint addr)
        {
            if (game_up)
            {
                //По количеству макс. пользователей начиная с 0 ID
                for (int ID = 0; ID < max_users; ID++)
                {
                    //Если поток с данным ID не активен, то создаем на его месте
                    if (!session_threads[ID].IsAlive)
                    {
                        session_threads.Insert(ID, new Thread(new ParameterizedThreadStart(Session)));

                        //Инициализация сессионных параметров игрока
                        if (!obj._Char.ContainsKey(char_name)) obj._Char.Add(char_name, new Char_Stat { IP = addr.Address.ToString() });
                        else obj._Char[char_name].IP = addr.Address.ToString();
                        obj._Char[char_name].finding_thread = true;
                        obj._Char[char_name].online = true;
                        obj._Char[char_name].port = addr.Port;
                        obj._Char[char_name].ID = ID;

                        
                        if (!clients_iport.ContainsKey(Cons.ToIport(addr))) clients_iport.Add(Cons.ToIport(addr), null);
                        else clients_iport[Cons.ToIport(addr)] = null;
                        
                        //Словарь дропа
                        if (!drop_Handle.ContainsKey(Cons.ToIport(addr))) drop_Handle.Add(Cons.ToIport(addr), null);
                        else drop_Handle[Cons.ToIport(addr)] = null;
                        
                        //Словарь обновления инвентаря
                        if (!item_Used.ContainsKey(Cons.ToIport(addr))) item_Used.Add(Cons.ToIport(addr), null);
                        else item_Used[Cons.ToIport(addr)] = null;

                        if (!grocery_buy.ContainsKey(Cons.ToIport(addr))) grocery_buy.Add(Cons.ToIport(addr), null);
                        else grocery_buy[Cons.ToIport(addr)] = null;

                        if (!quest_Handle.ContainsKey(Cons.ToIport(addr))) quest_Handle.Add(Cons.ToIport(addr), null);
                        else quest_Handle[Cons.ToIport(addr)] = null;
                       
                        //Создаем обьект игрока с заданным именем, если его нету
                        obj.Initialize_Player(char_name);

                        //Говорим что инвентар поменялся чтобы, персонаж получил итемы при старте
                        var player = obj.players.Find(p => p.Name == char_name);
                        player.Inventory.Changed = true;   
                               
                        total_players++;

                        Console.WriteLine("Game server: Started session with:[" + char_name + "], ID: " + obj._Char[char_name].ID
                            + ", Total online: [" + total_players + "]");
                        session_threads[ID].Start(char_name);
                        obj._Char[char_name].finding_thread = false;
                        return 0;
                    }
                }
                Console.WriteLine("Starting session: Max users");
                //obj._Char[char_name].finding_thread = false;
                return 2;
            }

            else
            {
                Console.WriteLine("Starting session: Game is down");
               // obj._Char[char_name].finding_thread = false;
                return 3;
            }
        }

        void Listen_Clients()
        {
            
            //Создание лог. файла
            //DateTime dt = DateTime.Now;
            //string writePath = @"Logs ["+dt.Hour.ToString()+"h "+dt.Minute.ToString()+"m] ["+dt.ToLongDateString()+"].txt";
            //StreamWriter sw = new StreamWriter(writePath, false, Encoding.Default);
            
            string name = "";
            byte[] costyl = new byte[512];
            byte[] pack = new byte[512];
            Console.WriteLine("Game server: Started listen");
            try
            {
                while (game_up)
                {
                    pack = game_listener.Receive(ref remoteIp);

                    //Запись логов
                    //sw.WriteLine("[" + dt.DayOfWeek + " " + dt.ToLongTimeString() + "]: " +pack[0].ToString()+"::"+Cons.GetData(pack));                    
                    //Console.WriteLine("Pack len = " + pack.Length + " " + Cons.GetData(pack) + "name=" + Cons.GetName13b(pack) + "|");

                    //Проверка легитимности пакета
                    if (!clients_iport.ContainsKey(Cons.ToIport(remoteIp))) { Console.WriteLine("Packet from unregistred IP:Port"); continue; }

                    switch (pack[0])
                    {
                        //END  
                        case 3:
                            name = Cons.GetData(pack);
                            if (obj._Char.ContainsKey(name) &&
                                (remoteIp.ToString() == obj._Char[name].Addr.ToString() && remoteIp.Port == obj._Char[name].Addr.Port))
                            {
                                Array.Copy(pack, 1, obj._Char[name].packet.END.data, 0, pack.Length - 1);
                                obj._Char[name].packet.END.changed = true;
                            }
                            break;

                        //SET_XYD
                        case 4:
                            name = Cons.GetName14b(pack);
                            if (obj._Char.ContainsKey(name) &&
                                (remoteIp.ToString() == obj._Char[name].Addr.ToString() && remoteIp.Port == obj._Char[name].Addr.Port))
                            {                            
                                Cons.SetPlayer_XYD(ref obj, pack);
                                Array.Copy(pack, 1, costyl, 0, pack.Length - 1);//Записывается с пустотами  
                                obj._Char[name].packet.SET_XYD.data = costyl;
                                obj._Char[name].packet.SET_XYD.changed = true;
                            }
                            break;
                                               
                        //PLAYER_END_OK
                        case 6:
                            clients_iport[Cons.ToIport(remoteIp)] = Cons.GetData(pack);
                            break;

                        //HIT: 1[type of attack] 2-5[x] 6-9[y] 10[npc or player] 11-end[id or name]
                        case 7:                          
                            foreach (var item in obj._Char.Keys)
                            {
                                if (obj._Char[item].port == remoteIp.Port && obj._Char[item].IP == remoteIp.Address.ToString())
                                {
                                    var entrance = obj.players.First(p => p.Name == obj._Char[item].name);
                                    entrance.AttackFunc(obj.mobs);
                                }
                            }
                            break;

                        //DROP_TAKEN | 1-4[id] 5-8[ex_id]
                        case 8:
                            if (drop_Handle[Cons.ToIport(remoteIp)] != null) continue;
                            drop_Handle[Cons.ToIport(remoteIp)] = pack;
                            break;

                       //INV_UPDATED | 1 - 4[items_updated]
                        case 9:
                            if (clients_iport[Cons.ToIport(remoteIp)] != null) continue;
                            clients_iport[Cons.ToIport(remoteIp)] = Cons.GetData(pack);
                            break;

                        //ITEM_USED	| 1-4[id] 5-8[id_ex]
                        case 10:
                            if (item_Used[Cons.ToIport(remoteIp)] != null) continue;
                            item_Used[Cons.ToIport(remoteIp)] = pack;
                            break;

                        //11		GROCERY_BUY | 1-4[id]
                        case 11:
                            if (grocery_buy[Cons.ToIport(remoteIp)] != null) continue;
                            grocery_buy[Cons.ToIport(remoteIp)] = pack;
                            break;

                        //12		TAKE_QUEST 	| 1-4[Q_ID] 5-8[NPC_ID]
                        case 12:
                            if (quest_Handle[Cons.ToIport(remoteIp)] != null) continue;
                            quest_Handle[Cons.ToIport(remoteIp)] = pack;           
                            break;

                        //13		QUESTS_ALL	| 1-end[same_data]
                        case 13:
                            if (clients_iport[Cons.ToIport(remoteIp)] != null) continue;
                            clients_iport[Cons.ToIport(remoteIp)] = Cons.GetData(pack);
                            break;

                        //14		QUEST_UPDATED	| 1-end[same_data]
                        case 14:
                            if (clients_iport[Cons.ToIport(remoteIp)] != null) continue;
                            clients_iport[Cons.ToIport(remoteIp)] = Cons.GetData(pack);
                            break;

                        //15      NPCxyd_REQUEST | 1 - end[same_data]
                        case 15:
                            //Console.WriteLine("15: " + Cons.GetData(pack));
                            if (clients_iport[Cons.ToIport(remoteIp)] != null) continue;
                            clients_iport[Cons.ToIport(remoteIp)] = Cons.GetData(pack);
                            break;

                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Game catch: Listner:: while["+remoteIp+"]" + e.ToString() + "code:" + e.ErrorCode);
                if (game_up) Listen_Clients();
            }
        }

        //Для каждого игрока создается отдельный поток с сессией
        void Session(object char_name)
        {         
            string name = (string)char_name;
            IPEndPoint addr = new IPEndPoint(IPAddress.Parse(obj._Char[name].IP), obj._Char[name].port);
            bool IsExit = false;

            bool CharsXYD = false;
            bool MyXYD = false;
            bool MobsTH = false;
            bool DroppedTH = false;
            bool DropHandle = false;
            bool Inventory = false;
            bool Quests = false;
            bool NpcTH = false;
            try
            {
                List<Player> players = new List<Player>(obj.players);
                var entrance = players.Find(r => r.Name == name);

                //Цикл обновления инфы на клиенте
                while (obj._Char.ContainsKey(name) && obj._Char[name].online == true)
                {
                    if (obj._Char[name].packet.END.changedP == true)
                    {
                        //Указываем что происходит выход,
                        //Чтобы при повторных пакетах о выходе не вызывать функцию выхода еще раз
                        if (!IsExit)
                        {
                            IsExit = true;
                            Send(252, new byte[] { 0 }, addr);
                            Disconnect(name);
                        }
                    }

                    //Отправка координат всех игроков, после того как были обновлены свои координаты игрока    
                    if (CharsXYD == false)
                    {
                        CharsXYD = true;
                        Task.Run(() =>
                        {
                            Console.WriteLine("Started CharsXYD Thread, id thread: " + Task.CurrentId);
                            while (obj._Char[name].online == true)
                            {
                                players = new List<Player>(obj.players);

                                foreach (var player in players)
                                {
                                    if (player == null || player.Name == name) continue;

                                    float X = player.X - entrance.X;
                                    float Y = player.Y - entrance.Y;

                                    if (Math.Sqrt(X * X + Y * Y) < 12d)
                                    {
                                        Send_CHXYD(player.X, player.Y, player.Rotation, player.Direction, player.Hp, player.Name, addr);//CH_XYD
                                    }
                                }
                                Thread.Sleep(10);
                            }
                        });
                    }

                    //Передача своих координат
                    if (MyXYD == false)
                    {
                        MyXYD = true;
                        Task.Run(() =>
                        {
                            Console.WriteLine("     Started MyXYD Thread, id thread: " + Task.CurrentId);
                            while (obj._Char[name].online == true)
                            {
                                entrance.CheckDeath();
                                switch (entrance.Direction)
                                {
                                    case 0: break;
                                    case 1: entrance.MovedByControl(false, false, false, true, obj.collision_map); break;
                                    case 2: entrance.MovedByControl(true, false, false, true, obj.collision_map); break;
                                    case 3: entrance.MovedByControl(true, false, false, false, obj.collision_map); break;
                                    case 4: entrance.MovedByControl(true, true, false, false, obj.collision_map); break;
                                    case 5: entrance.MovedByControl(false, true, false, false, obj.collision_map); break;
                                    case 6: entrance.MovedByControl(false, true, true, false, obj.collision_map); break;
                                    case 7: entrance.MovedByControl(false, false, true, false, obj.collision_map); break;
                                    case 8: entrance.MovedByControl(false, false, true, true, obj.collision_map); break;
                                }
                                try
                                {
                                    if (item_Used[Cons.ToIport(addr)] != null)
                                    {
                                        Cons.UseItem(ref entrance, item_Used[Cons.ToIport(remoteIp)]);
                                        item_Used[Cons.ToIport(addr)] = null;
                                        entrance.Inventory.Changed = true;
                                    }
                                }
                                catch (Exception e) { Console.WriteLine(e.ToString()); }

                                SendMyXYD(entrance.X, entrance.Y, entrance.Rotation, entrance.Direction, entrance.Hp, name, addr);
                                Thread.Sleep(10);
                            }
                        });
                    }

                    //Передача координат мобов
                    if (MobsTH == false)
                    {
                        MobsTH = true;
                        Task.Run(() =>
                        {
                            Console.WriteLine("     Started mobs Thread, id thread: " + Task.CurrentId);
                            while (obj._Char[name].online == true)
                            {
                                for (int i = 0; i < obj.mobs.Length; i++)
                                {
                                    if (entrance == null || obj.mobs[i] == null) continue;
                                    float X = obj.mobs[i].X - entrance.X;
                                    float Y = obj.mobs[i].Y - entrance.Y;

                                    if (Math.Sqrt(X * X + Y * Y) < 12d)
                                    {
                                        Send_MobXYD(obj.mobs[i].X, obj.mobs[i].Y, 0, 0, obj.mobs[i].HP, obj.mobs[i].Id, i, addr);//CH_XYD
                                    }
                                    // Thread.Sleep(1);
                                }
                                Thread.Sleep(150);
                            }
                        });
                    }

                    //Передача координат NPC
                    if (NpcTH == false)
                    {
                        NpcTH = true;
                        Task.Run(() =>
                        {
                            bool[] NPCsent = new bool[100];
                            for (int i = 0; i < 100; i++) NPCsent[i] = false;
                            try
                            {
                                Console.WriteLine("     Started NPC Thread, id thread: " + Task.CurrentId);
                                while (obj._Char.ContainsKey(name) && obj._Char[name].online == true)
                                {
                                    for (int i = 0; i < obj.NPCs.Length; i++)
                                    {
                                        if (entrance == null || obj.NPCs[i] == null) continue;
                                        float X = obj.NPCs[i].X - entrance.X;
                                        float Y = obj.NPCs[i].Y - entrance.Y;

                                        if (NPCsent[i] == false && (Math.Sqrt(X * X + Y * Y) < 12d))
                                        {
                                            Send_NPCXYDs(obj.NPCs[i].X, obj.NPCs[i].Y, 0, 0, obj.NPCs[i].Id, obj.NPCs[i].Name, addr);
                                            NPCsent[i] = true;
                                        }
                                        else
                                        {
                                            if (!(Math.Sqrt(X * X + Y * Y) < 12d)) NPCsent[i] = false;
                                        }
                                    }
                                    Thread.Sleep(150);
                                }
                            }
                            catch (Exception e) { Console.WriteLine(e.ToString()); }
                        });
                    }

                    //Передача дропа
                    if (DroppedTH == false)
                    {
                        DroppedTH = true;
                        Task.Run(() =>
                        {
                            Console.WriteLine("     Started drop sender Thread, id thread: " + Task.CurrentId);
                            while (obj._Char[name].online == true)
                            {
                                for (int i = 0; i < obj.items.Count; i++)
                                {
                                    if (entrance == null || obj.items[i] == null) continue;
                                    float X = obj.items[i].X - entrance.X;
                                    float Y = obj.items[i].Y - entrance.Y;

                                    if ((Math.Sqrt(X * X + Y * Y) < 12d))
                                    {
                                        if (obj.items[i].Dropped) Send_Drop(obj.items[i].X, obj.items[i].Y, obj.items[i].Id, obj.items[i].Quantity, obj.items[i].ID_EX, addr);
                                        else if (obj.items[i].Taken) Del_Drop(obj.items[i].ID_EX, addr);
                                    }
                                }
                                Thread.Sleep(100);
                            }
                        });
                    }

                    //Обработка запросов на лут персонажа
                    if (DropHandle == false)
                    {
                        DropHandle = true;
                        Task.Run(() =>
                        {
                            Console.WriteLine("     Started drop handler Thread, id thread: " + Task.CurrentId);
                            while (obj._Char[name].online == true)
                            {
                                if (drop_Handle[Cons.ToIport(addr)] != null)
                                {
                                    try
                                    {
                                        //Console.WriteLine("Item handling");

                                        byte[] id = new byte[4];
                                        byte[] ex = new byte[4];

                                        Array.Copy(drop_Handle[Cons.ToIport(addr)], 1, id, 0, 4);
                                        Array.Copy(drop_Handle[Cons.ToIport(addr)], 5, ex, 0, 4);

                                        int ID = BitConverter.ToInt32(id, 0);
                                        int EX = BitConverter.ToInt32(ex, 0);

                                        Item item = obj.items.Find(i => (i.Id == ID && i.ID_EX == EX));

                                        if (item == null)
                                        {
                                            //Console.WriteLine("Item not found");
                                            Send(245, "NO" + Cons.GetData(drop_Handle[Cons.ToIport(addr)]), remoteIp);
                                            drop_Handle[Cons.ToIport(addr)] = null;
                                            continue;
                                        }

                                        if (item.Dropped == false)
                                        {
                                            //Console.WriteLine("Item not dropped");
                                            Send(245, "NO" + Cons.GetData(drop_Handle[Cons.ToIport(addr)]), remoteIp);
                                            drop_Handle[Cons.ToIport(addr)] = null;
                                            continue;
                                        }
                                        if (entrance.Inventory.Items.Contains(item) == true)
                                        {
                                            //Console.WriteLine("Item is in Inventory");
                                            Send(245, "NO" + Cons.GetData(drop_Handle[Cons.ToIport(addr)]), remoteIp);
                                            drop_Handle[Cons.ToIport(addr)] = null;
                                            continue;
                                        }

                                        float X = item.X - entrance.X;
                                        float Y = item.Y - entrance.Y;

                                        if (Math.Sqrt(X * X + Y * Y) < 1.5d)
                                        {
                                            item.Dropped = false;
                                            item.Taken = true;
                                            entrance.Inventory.Add(item);

                                            obj.items.Remove(item);

                                            //Console.WriteLine("DROP_TAKEN:: To:" + remoteIp + "Data:" + Cons.GetData(drop_Handle[Cons.ToIport(addr)]));
                                            Send(245, Cons.GetData(drop_Handle[Cons.ToIport(addr)]), remoteIp);

                                            drop_Handle[Cons.ToIport(addr)] = null;

                                        }
                                        else
                                        {
                                            //Console.WriteLine("Far distance");
                                            drop_Handle[Cons.ToIport(addr)] = null;
                                        }
                                    }
                                    catch (Exception e) { Console.WriteLine(e.ToString()); }
                                }

                                Thread.Sleep(1);
                            }
                        });
                    }

                    //Обработка инвентаря
                    if (Inventory == false)
                    {
                        Inventory = true;
                        Task.Run(() =>
                        {
                            Console.WriteLine("     Started Inventory handler Thread, id thread: " + Task.CurrentId);
                            while (obj._Char[name].online == true)
                            {
                                if (entrance.Inventory.Changed == true)
                                {
                                    List<int> Id = new List<int>();
                                    List<int> Ex = new List<int>();
                                    List<int> Quantity = new List<int>();
                                    int total = 0;

                                    for (int i = 0; i < entrance.Inventory.Items.Length; i++, total++)
                                    {
                                        if (entrance.Inventory.Items[i] == null) break;

                                        //Console.WriteLine("Adding: Item_" + i + ", ID = " + entrance.Inventory.Items[i].Id + ", EX = "+ entrance.Inventory.Items[i].ID_EX +", Q = " + entrance.Inventory.Items[i].Quantity);
                                        Id.Add(entrance.Inventory.Items[i].Id);
                                        Ex.Add(entrance.Inventory.Items[i].ID_EX);
                                        Quantity.Add(entrance.Inventory.Items[i].Quantity);
                                    }
                                    UpdateInv(total, Id, Ex, Quantity, addr);
                                    entrance.Inventory.Changed = false;
                                }
                                if (grocery_buy[Cons.ToIport(addr)] != null)
                                {
                                    Cons.Buy(ref entrance, grocery_buy[Cons.ToIport(addr)]);
                                    grocery_buy[Cons.ToIport(addr)] = null;
                                }
                                Thread.Sleep(50);
                            }
                        });
                    }

                    if (Quests == false)
                    {
                        Quests = true;
                        Task.Run(() =>
                        {
                            Console.WriteLine("     Started Quests handler Thread, id thread: " + Task.CurrentId);
                            List<int> Id = new List<int>();
                            List<int> St = new List<int>();
                            List<int> Quantity = new List<int>();
                            int total = entrance.Quests.Count;

                            for (int i = 0; i < total; i++)
                            {
                                Id.Add(entrance.Quests[i].Id);
                                St.Add(entrance.Quests[i].State);
                                Quantity.Add(entrance.Quests[i].Counter);
                            }
                            Initialize_Quests(total, Id, St, Quantity, addr);

                            while (obj._Char[name].online == true)
                            {
                                entrance.CheckQuests();
                                try
                                {
                                    if (quest_Handle[Cons.ToIport(addr)] != null)
                                    {
                                        Cons.QuestHandle(ref entrance, quest_Handle[Cons.ToIport(addr)]);
                                        quest_Handle[Cons.ToIport(addr)] = null;
                                    }

                                    for (int i = 0; i < entrance.Quests.Count; i++)
                                    {
                                        if (entrance.Quests[i].UpdClient)
                                        {
                                            Quest_Update(entrance.Quests[i].Id, entrance.Quests[i].State, entrance.Quests[i].Counter, addr);
                                            entrance.Quests[i].UpdClient = false;
                                        }
                                    }
                                    Thread.Sleep(100);
                                }
                                catch (Exception e) { Console.WriteLine(e.ToString()); }
                            }
                        });
                    }
                    Thread.Sleep(1);
                }
            }

            catch (SocketException e)
            {
                Console.WriteLine("     Game catch: Socket ex in session [" + char_name + "]" + ":: " + e.Message + "code:" + e.ErrorCode);
                if (obj._Char[name].online == true) Session(char_name);
            }
            catch (Exception e) {
                //Console.WriteLine(e.ToString());
            } 
        }

        //Закрытие потока по ID
        public bool Disconnect(string char_name)
        {
            if (obj._Char.ContainsKey(char_name)) obj._Char[char_name].online = false;
            else return false;

            Console.WriteLine("Setting char in DB: " + obj.UpdateDB_Player(char_name));

            Console.WriteLine("Disconnection: [" + char_name + "] try to Disconnect");            
            try
            {
                Send_EndPlayer(char_name);
                if(session_threads[obj._Char[char_name].ID].IsAlive) session_threads[obj._Char[char_name].ID].Join(1);
                bool g = clients_iport.Remove(new IPort(obj._Char[char_name].IP, obj._Char[char_name].port));
                Console.WriteLine("Disconnection: Removal result = " + g);

                var entrance = obj.players.First(p => p.Name == char_name);
                entrance.Alive = false;
                obj.players.Remove(entrance);
                obj._Char.Remove(char_name);
                
                total_players--;
                Console.WriteLine("Disconnection: [" + char_name + "] has Disconnected, Online: [" + total_players+"]");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Disconnection: Exeception in Disconnect: " + e.ToString());
                return false;
            }
        }
            
        //Периодическая проверка времени получения последнего пакета, и отключение если долго не получал его
        void Period_Disconnect()
        {
            while (game_up)
            {
                try
                {
                    List<Player> copy = new List<Player>(obj.players);
                    foreach (var player in copy)
                    {
                        //Если такого пакета не было уже более 10 секунд, то отключение
                        if (obj._Char.ContainsKey(player.Name) && obj._Char[player.Name].packet.SET_XYD.time > 5000) Disconnect(player.Name);
                    }
                    //Каждые 5сек проверяем время данного пакета
                    Thread.Sleep(5000);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Game serv: Period_Disconn:: " + e.Message);
                }
            }
        }

        //Обработка мобов
        void Mobs()
        {
            Console.WriteLine("[Mobs handler] thread - Started");
            while (game_up == true)
            {
                for (int i = 0; i < obj.mobs.Length; i++)
                {
                    if (obj.mobs[i] == null) continue;
                    obj.mobs[i].WorkFunc(obj.players, obj.items, obj.rnd);
                }
                Thread.Sleep(50);
            }
            Console.WriteLine("[Mobs handler] thread - Stopped");
        }

        //Метод отправки сообщений
        public void Send(byte command, byte[] msg, IPEndPoint addr)
        {
            byte[] s_packet = new byte[1 + msg.Length];
            for (int i = 1; i < msg.Length + 1; i++) s_packet[i] = msg[i - 1];
            s_packet[0] = command;

            try
            {
                game_listener.Send(s_packet, s_packet.Length, addr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Game serv: SendB exception:: " + addr + ex.ToString() + "\n " + ex.Message);
            }
        }
        public void Send(byte command, string message, IPEndPoint addr)
        {
            byte[] msg = Encoding.UTF8.GetBytes(message);

            byte[] s_packet = new byte[1 + msg.Length];
            for (int i = 1; i < msg.Length + 1; i++) s_packet[i] = msg[i - 1];
            s_packet[0] = command;

            try
            {
                game_listener.Send(s_packet, s_packet.Length, addr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Game serv: SendM exception:: " + addr + ex.ToString() + "\n " + ex.Message);
            }
        }

        public async void SendS(byte command, byte[] msg, IPEndPoint addr)
        {
            byte[] s_packet = new byte[1 + msg.Length];
            for (int i = 1; i < msg.Length + 1; i++) s_packet[i] = msg[i - 1];
            s_packet[0] = command;

            Ping pinger = new Ping();
            PingReply reply = pinger.Send(addr.Address);

            try
            {
                for (int i = 0; i < 100; i++)
                {
                    game_listener.Send(s_packet, s_packet.Length, addr);

                    //Console.WriteLine("Game SENDS:[" + i + "] To:" + addr + " Message:[" + Encoding.UTF8.GetString(msg) + "]");

                    await Task.Delay((int)reply.RoundtripTime + 1);

                    //Console.WriteLine("Received:[" + clients_iport[Cons.ToIport(addr)] + "]");

                    if (clients_iport.ContainsKey(Cons.ToIport(addr)) && clients_iport[Cons.ToIport(addr)] == Encoding.UTF8.GetString(msg)) { clients_iport[Cons.ToIport(remoteIp)] = null; break; }                   
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Game serv: SendS exception: To::" + addr + "::"+ ex.ToString());
            }
        }

        public bool Send_CHXYD(float X, float Y, float Rotation, byte Direction, float HP, string char_name, IPEndPoint addr)
        {
            byte[] Xb = BitConverter.GetBytes(X);
            byte[] Yb = BitConverter.GetBytes(Y);
            byte[] RotationB = BitConverter.GetBytes(Rotation);
            byte[] Char_Name = Encoding.UTF8.GetBytes(char_name);
            byte[] HpB = BitConverter.GetBytes(HP);
            int name_len = Char_Name.Length;

            byte[] output = new byte[4 + 4 + 4 + 1 + 4 + name_len];

            Array.Copy(Xb, output, 4);
            Array.Copy(Yb, 0, output, 4, 4);
            Array.Copy(RotationB, 0, output, 8, 4);
            output[12] = Direction;//(0-нету, 1-право, 2-вверх, 3- влево, 4-вниз 
            Array.Copy(HpB, 0, output, 13, 4);
            Array.Copy(Char_Name, 0, output, 17, name_len);

            Send(251, output, addr);
            return true;
        }

        /// <summary>
        /// Отправляет данные об игроке всем, кто онлайн
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Send_InfoPlayer(string name, byte lvl, float MaxHp, IPEndPoint addr)
        {
            byte[] MaxHpB = BitConverter.GetBytes(MaxHp);
            byte[] Char_Name = Encoding.UTF8.GetBytes(name);
            int name_len = Char_Name.Length;

            byte[] output = new byte[1 + 4 + name_len];//склеиваем все в один массив 

            output[1] = lvl;
            Array.Copy(MaxHpB, 0, output, 2, 4);
            Array.Copy(Char_Name, 0, output, 6, name_len);

            SendS(250, output, addr);//PLAYER_INFO
            return true;
        }

        /// <summary>
        /// Всем, кто онлайн сообщаяем, что данный игрок вышел из игры
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Send_EndPlayer(string name)
        {
            byte[] nameB = Encoding.UTF8.GetBytes(name);

            //Проходимся по всем онлайн игрокам и посылаем им имя отключившегося игрока
            foreach (var item in obj._Char.Keys)
            {
                if (obj._Char[item].online == true && item != name)
                {
                    Console.WriteLine("Tell Disconn to:[" + item +"] about:["+name+"]" );
                    IPEndPoint addr = new IPEndPoint(IPAddress.Parse(obj._Char[item].IP), obj._Char[item].port);
                    SendS(249, nameB, addr);
                }
            }
            return true;
        }

        //248		MOB_XYD		| 1-4[x] 5-8[y] 8-11[rotation] 12[d] 13-16[hp] 17-20[type] 21-end[id]
        public bool Send_MobXYD(float X, float Y, float Rotation, byte Direction, float HP, int Type, int ID, IPEndPoint addr)
        {
            byte[] Xb = BitConverter.GetBytes(X);
            byte[] Yb = BitConverter.GetBytes(Y);
            byte[] RotationB = BitConverter.GetBytes(Rotation);
            byte[] HpB = BitConverter.GetBytes(HP);
            byte[] typeB = BitConverter.GetBytes(Type);
            byte[] idB = BitConverter.GetBytes(ID);

            byte[] output = new byte[4 + 4 + 4 + 1 + 4 + 4 + 4];

            Array.Copy(Xb, output, 4);
            Array.Copy(Yb, 0, output, 4, 4);
            Array.Copy(RotationB, 0, output, 8, 4);
            output[12] = Direction;//(0-нету, 1-право, 2-вверх, 3- влево, 4-вниз 
            Array.Copy(HpB, 0, output, 13, 4);
            Array.Copy(typeB, 0, output, 17, 4);
            Array.Copy(idB, 0, output, 21, 4);

            Send(248, output, addr);
            return true;
        }

        //247		DROP_SET 	| 1-4[x] 5-8[y] 9-12[item] 13-16[quantity] 17-end[id_ex]
        public bool Send_Drop(float X, float Y, int item, int Q, int Id, IPEndPoint addr)
        {
            byte[] Xb = BitConverter.GetBytes(X);
            byte[] Yb = BitConverter.GetBytes(Y);
            byte[] ItemB = BitConverter.GetBytes(item);
            byte[] Qb = BitConverter.GetBytes(Q);
            byte[] Idb = BitConverter.GetBytes(Id);

            byte[] output = new byte[4 + 4 + 4 + 4 + 4];

            Array.Copy(Xb, output, 4);
            Array.Copy(Yb, 0, output, 4, 4);
            Array.Copy(ItemB, 0, output, 8, 4);
            Array.Copy(Qb, 0, output, 12, 4);
            Array.Copy(Idb, 0, output, 16, 4);

            Send(247, output, addr);
            return true;
        }

        //246 		SEND_MYXYD	| 1-4[x] 5-8[y] 8-11[rotation] 12[d] 13-16[hp] 17-end[char_name]
        public bool SendMyXYD(float X, float Y, float Rotation, byte Direction, float HP, string char_name, IPEndPoint addr)
        {
            byte[] Xb = BitConverter.GetBytes(X);
            byte[] Yb = BitConverter.GetBytes(Y);
            byte[] RotationB = BitConverter.GetBytes(Rotation);
            byte[] Char_Name = Encoding.UTF8.GetBytes(char_name);
            byte[] HpB = BitConverter.GetBytes(HP);
            int name_len = Char_Name.Length;

            byte[] output = new byte[4 + 4 + 4 + 1 + 4 + name_len];

            Array.Copy(Xb, output, 4);
            Array.Copy(Yb, 0, output, 4, 4);
            Array.Copy(RotationB, 0, output, 8, 4);
            output[12] = Direction;
            Array.Copy(HpB, 0, output, 13, 4);
            Array.Copy(Char_Name, 0, output, 17, name_len);

            Send(246, output, addr);
            return true;
        }

        //242     INV_UPDATE  | 1-4[q_all] 5-8[id] 9-12[ex] 13-16[q]...
        public bool UpdateInv(int Total_Items, List<int> ID, List<int> EX, List<int> Q, IPEndPoint addr)
        {
            byte[] output = new byte[4 + (12 * Total_Items)];

            byte[] TotalB = BitConverter.GetBytes(Total_Items);
            Array.Copy(TotalB, 0, output, 0, 4);

            for (int i = 0; i < Total_Items; i++)
            {
                byte[] IDb = BitConverter.GetBytes(ID[i]);
                byte[] EXb = BitConverter.GetBytes(EX[i]);
                byte[] Qb = BitConverter.GetBytes(Q[i]);

                Array.Copy(IDb, 0, output, (i * 12) + 4, 4);
                Array.Copy(EXb, 0, output, (i * 12) + 8, 4);
                Array.Copy(Qb, 0, output, (i * 12) + 12, 4);
            }

            SendS(242, output, addr);
            return true;
        }

        //243		ALL_QUESTS	| 1-4[q_all] 5-8[id] 9-12[stage] 13-16[q]... 
        public bool Initialize_Quests(int Total_Items, List<int> ID, List<int> St, List<int> Q, IPEndPoint addr)
        {
            byte[] output = new byte[4 + (12 * Total_Items)];

            byte[] TotalB = BitConverter.GetBytes(Total_Items);
            Array.Copy(TotalB, 0, output, 0, 4);

            for (int i = 0; i < Total_Items; i++)
            {
                byte[] IDb = BitConverter.GetBytes(ID[i]);
                byte[] EXb = BitConverter.GetBytes(St[i]);
                byte[] Qb = BitConverter.GetBytes(Q[i]);

                Array.Copy(IDb, 0, output, (i * 12) + 4, 4);
                Array.Copy(EXb, 0, output, (i * 12) + 8, 4);
                Array.Copy(Qb, 0, output, (i * 12) + 12, 4);
            }

            SendS(243, output, addr);
            return true;
        }

        //244		DEL_DROP	| 1-4[id_ex]
        public bool Del_Drop(int ID_EX, IPEndPoint addr)
        {
            byte[] ID_EXb = BitConverter.GetBytes(ID_EX);
            byte[] output = new byte[4];

            Array.Copy(ID_EXb, output, 4);

            Send(244, output, addr);
            return true;
        }

        //241		QUEST_UPD	| 1-4[id] 5-8[st] 9-12[q]
        public bool Quest_Update(int ID, int St, int Q, IPEndPoint addr)
        {
            byte[] output = new byte[4 + 4 + 4];

            byte[] IDb = BitConverter.GetBytes(ID);
            byte[] EXb = BitConverter.GetBytes(St);
            byte[] Qb = BitConverter.GetBytes(Q);

            Array.Copy(IDb, output, 4);
            Array.Copy(EXb, 0, output, 4, 4);
            Array.Copy(Qb, 0, output, 8, 4);

            SendS(241, output, addr);
            return true;
        }

        //240 	    NPC_XYD		| 1-4[x] 5-8[y] 9-12[rotation] 13[d] 14-17[id] 18-end[char_name]
        public bool Send_NPCXYDs(float X, float Y, float Rotation, byte Direction, int ID, string char_name, IPEndPoint addr)
        {
            byte[] Xb = BitConverter.GetBytes(X);
            byte[] Yb = BitConverter.GetBytes(Y);
            byte[] RotationB = BitConverter.GetBytes(Rotation);
            byte[] Char_Name = Encoding.UTF8.GetBytes(char_name);
            byte[] IDB = BitConverter.GetBytes(ID);
            int name_len = Char_Name.Length;

            byte[] output = new byte[4 + 4 + 4 + 1 + 4 + name_len];

            Array.Copy(Xb, output, 4);
            Array.Copy(Yb, 0, output, 4, 4);
            Array.Copy(RotationB, 0, output, 8, 4);
            output[12] = Direction;//(0-нету, 1-право, 2-вверх, 3- влево, 4-вниз 
            Array.Copy(IDB, 0, output, 13, 4);
            Array.Copy(Char_Name, 0, output, 17, name_len);

            SendS(240, output, addr);
            return true;
        }
    }
}
