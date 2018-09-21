using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Cshit_SERV
{
    class Serv
    {
        static int port = 0;
        static int max_users = 100;
        public int total_players = 0;
        private int[] nsend_packets;

        static Thread listen;
        static Thread dismiss;
        List<Thread> session_threads = new List<Thread>();

        UdpClient listener;
        UdpClient session_listener;

        IPEndPoint remoteIp;

        bool Serv_UP = true;
        
        Dictionary<string, string> Clients_IP;
        Dictionary<string, int> Clients_PORT;
        Dictionary<string, bool> Clients_ONLINE;
        Dictionary<string, int> Clients_ID;
        Dictionary<string, int> Clients_ConnectFailedTimes;

        //Координаты Персонажей
        static Dictionary<string, string[]> Characters_XYD;
        //Координаты Мобов
        static Dictionary<string, string[]> Mobs_XYD;

        public Serv(int PORT, int MAX_USERS)
        {
            port = PORT;
            max_users = MAX_USERS;
            nsend_packets = new int[MAX_USERS];

            Clients_IP = new Dictionary<string, string>(MAX_USERS);
            Clients_PORT = new Dictionary<string, int>(MAX_USERS);
            Clients_ONLINE = new Dictionary<string, bool>(MAX_USERS);
            Clients_ID = new Dictionary<string, int>(MAX_USERS);
            Clients_ConnectFailedTimes = new Dictionary<string, int>(MAX_USERS);

            Random rnd = new Random();
            Characters_XYD = new Dictionary<string, string[]>(MAX_USERS);
            //foreach (var item in Characters_XYD.Keys){  Characters_XYD.Add(item, new string[] {rnd.Next(50,65).ToString() , rnd.Next(50, 65).ToString(), "W" });}

            Mobs_XYD = new Dictionary< string, string[]> (MAX_USERS*5);
           //foreach (var item in Characters_XYD.Keys){  Characters_XYD.Add(item, new string[] { rnd.Next(30, 45).ToString(), rnd.Next(30, 45).ToString(), "W" });}

            for (int i = 0; i < MAX_USERS; i++)
                session_threads.Add(new Thread(new ParameterizedThreadStart(Session)));

            listener = new UdpClient(port); // UdpClient для получения данных  
            remoteIp = null; // адрес входящего подключения
        }

        //Запуск серва, если ошибка то true
        public bool Serv_Up()
        {
            try
            {
                Serv_UP = true;
                listen = new Thread(new ThreadStart(Listen_Connections));
                listen.Start();

                //dismiss = new Thread(new ThreadStart(Dismiss));
                //dismiss.Start();
                return false;
            }
            catch (Exception e)
            {
                return true;
                Console.WriteLine("Failed to START server {0}", e.Message);

            }
        }

        public bool Serv_Down()
        {
            try
            {
                Serv_UP = false;
                if (listener != null) listener.Close();
                if (listen != null) listen.Join();
                //if (dismiss != null) dismiss.Join();
                return false;
            }
            catch (Exception e)
            {
                return true;
                Console.WriteLine("Failed to STOP server {0}", e.Message);
            }
        }

        public void Listen_Connections()
        {
            string msg = "", data = "", cmd = "";
            int ID = 0;
            Console.WriteLine("Started to listen");
            try
            {
                while (Serv_UP)
                {
                    byte[] pack = listener.Receive(ref remoteIp);
                    msg = Encoding.UTF8.GetString(pack);
                    cmd = Cons.cmd(msg);
                    data = Cons.data(msg);//Пока не заменил на это для удобства
                    Console.WriteLine(cmd + "-" + remoteIp.Address + "::" + remoteIp.Port + "  " + data);

                    switch (cmd)
                    {
                        //Проверка сервера на живость
                        case "CHECK":
                            Send("ALIVE", "", remoteIp);
                            break;

                        //Клиент хочет начать сессию
                        case "BEGIN":                     
                            if (Clients_ONLINE.ContainsKey(data) && Clients_ONLINE[data] == true)
                                Send("DENIED", "", remoteIp);
                            else
                            {
                                if (ID == max_users)
                                    Send("MAX_ONLINE", total_players.ToString(), remoteIp);
                                else
                                {
                                    //Ищим свободный поток для клиента
                                    for (ID = 0; ID < max_users; ID++)
                                    {
                                        if (!session_threads[ID].IsAlive)
                                        {
                                            session_threads.Insert(ID, new Thread(new ParameterizedThreadStart(Session)));

                                            Clients_IP[data] = remoteIp.Address.ToString();
                                            Clients_PORT[data] = remoteIp.Port;
                                            Clients_ONLINE[data] = true;
                                            Clients_ID[data] = ID;
                                            Clients_ConnectFailedTimes[data] = 0;

                                            session_threads[ID].Start(data);
                                            break;
                                        }
                                    }
                                }                             
                            }
                            break;

                        //Клиент хочет кончить сессию
                        case "END":
                            if (Clients_ONLINE.ContainsKey(Cons.data(msg)) && Clients_ONLINE[Cons.data(msg)] == true)
                            {
                                Clients_ONLINE[Cons.data(msg)] = false;
                                total_players--;
                                session_threads[Clients_ID[Cons.data(msg)]].Join();
                                session_threads[Clients_ID[Cons.data(msg)]].Interrupt();
                                session_threads[Clients_ID[Cons.data(msg)]].Abort();
                                Console.WriteLine("END1   " + Clients_ID[Cons.data(msg)] + "  " + session_threads[Clients_ID[Cons.data(msg)]].IsAlive + " Total online: " + total_players);
                                Send("SUCCESSFUL", "", remoteIp);
                            }
                            else Send("ALREADY_OFFLINE", "", remoteIp);
                            break;
                    }
                }
            }
            catch (SocketException ex)
            {
                Listen_Connections();
            }
        }

        //Пототовая функция для прослушки новых соединений
        public void Session(object data)
        {
            total_players++;
            string name = (string)data;
            Console.WriteLine("     Started session with: " + name + "::ID:  " + Clients_ID[name] + " Total online: " + total_players);

            IPEndPoint addr = new IPEndPoint(IPAddress.Parse(Clients_IP[name]), Clients_PORT[name]);

            if (Characters_XYD.ContainsKey(name));//Characters_XYD[name] = new string[] { 55.ToString(), 55.ToString(), "W" };
            else Characters_XYD.Add(name, new string[] { 55.ToString(), 55.ToString(), "W" });

            Send("ACCEPTED", "", addr);

            try
            {
                while (Clients_ONLINE[name] == true)
                {
                    foreach (var item in Mobs_XYD.Keys)
                    {
                        Thread.Sleep(5);
                        Send("MOB_XYD",
                             item + "¶X" + Mobs_XYD[item].GetValue(0) + "¶Y" + Mobs_XYD[item].GetValue(1) + "¶D" + Mobs_XYD[item].GetValue(2),
                            addr);
                    }

                    foreach (var item in Characters_XYD.Keys)
                    {
                        Thread.Sleep(10);
                        Send("CH_XYD",
                             item + "¶X" + Characters_XYD[item].GetValue(0) + "¶Y" + Characters_XYD[item].GetValue(1) + "¶D" + Characters_XYD[item].GetValue(2),
                            addr);
                    }

                    byte[] pack = listener.Receive(ref remoteIp);
                    string msg = Encoding.UTF8.GetString(pack);
                    
                    switch (Cons.cmd(msg))
                    {
                        case "SET_XYD":
                            Console.WriteLine(name + "     inSession: " + msg);
                            Cons.Parse_Char_XYD(ref Characters_XYD, Cons.data(msg));
                            break;
                    }
                }
            }
            catch (Exception e)
            { Console.WriteLine(e.Message); }
        }

        //Отключение игроков которые не отвечают(не активно)
        public void Dismiss()
        {
            string player = "";
            int Attempt = 0;
            try
            {
                while (Serv_UP)
                {
                    //Проходим по всем клиентам, которые онлайн
                    foreach (var item in Clients_ONLINE.Keys)
                    {
                        if (Clients_ONLINE[item] == true)
                        {
                            IPEndPoint addr = new IPEndPoint(IPAddress.Parse(Clients_IP[item]), Clients_PORT[item]);
                            //Если поток на прослушку не начат, то начинаем
                            for (Attempt = 0; Attempt < 5; Attempt++)
                            {
                                Console.WriteLine(Attempt + ") Checking:" + item + "  IP: " + Clients_IP[item] + "  Port: " + Clients_PORT[item]);
                                Send("ALIVE_CHECK", "", addr);
                                Console.WriteLine(1);
                                Thread.Sleep(20);
                                byte[] pack = listener.Receive(ref remoteIp);
                                Console.WriteLine(2);
                                string msg = Cons.cmd(Encoding.UTF8.GetString(pack));
                                player = item;
                                Console.WriteLine("dfgdfgdfgdf  "+ player);
                                if (msg == "ALIVE")
                                {
                                    Clients_ONLINE[player] = true;
                                    Console.WriteLine("He is alive" + Clients_ID[player] + "Online: " + total_players);
                                    Clients_ConnectFailedTimes[player] = 0;
                                }
                                else if (Clients_ConnectFailedTimes[player] == 4)
                                {
                                    Clients_ONLINE[player] = false;
                                    session_threads[Clients_ID[player]].Join();
                                    session_threads[Clients_ID[player]].Interrupt();
                                    session_threads[Clients_ID[player]].Abort();
                                    total_players--;
                                    Console.WriteLine("Disconnected" + Clients_ID[player] + "Online: " + total_players);
                                }
                            }
                        }

                    }
                    Thread.Sleep(3000);
                }
            }
            catch (SocketException e)
            {
                Clients_ConnectFailedTimes[player]++;
                Dismiss();
                Console.WriteLine(e.Message);
            }
        }
        //Метод отправки сообщений
        public void Send(string command, string msg, IPEndPoint addr)
        {
            int pointer = 0;
            
            byte[] s_num = new byte[6];
            byte[] s_comm = new byte[6];
            byte[] s_data = new byte[500];
            byte[] s_packet = new byte[512];

            s_num = Encoding.UTF8.GetBytes("packet=" + nsend_packets.ToString() + ";");
            s_comm = Encoding.UTF8.GetBytes("command=" + command + ";");
            s_data = Encoding.UTF8.GetBytes("data=" + msg);

            for (int i = 0; i < s_num.Length; i++, pointer++) s_packet[pointer] = s_num[i];
            for (int i = 0; i < s_comm.Length; i++, pointer++) s_packet[pointer] = s_comm[i];
            for (int i = 0; i < s_data.Length; i++, pointer++) s_packet[pointer] = s_data[i];

            try
            {
                listener.Send(s_packet, pointer, addr); //можно указать s_packet.Length, чтобы отправлять весь пакет (с пустотами)
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение в отправке: " + ex.ToString() + "\n  " + ex.Message);
            }
        }
    }
}
