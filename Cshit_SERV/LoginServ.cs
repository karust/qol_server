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
    class LoginServ
    {
        private int port = 0;
        int nlogSendPacks = 0;   
        bool Login_UP = true;
        UdpClient log_listen;
        IPEndPoint remoteIp;
        Thread login_th;
        
        GameServ game_Serv;
        GameObj game_Obj;

        int max_users = 0;

        public LoginServ(int PORT, int MaxClients, GameServ Identf_GameServ)
        {
            //Аддрес лог серва
            port = PORT;
            log_listen = new UdpClient(port);
            remoteIp = null;
            
            //Берем адрес гейм серва для связки его с данным логом
            game_Serv = Identf_GameServ;
            //Устанавливаем нужные тут параметры гейм серва
            game_Serv.total_players = 0;

            max_users = MaxClients;

            //Связываем с обьектами серва
            game_Obj = game_Serv.obj;             
        }

        public bool Login_Up()
        {
            login_th = new Thread(new ThreadStart(Listen_Connections));

            try
            {
                login_th.Start();
               // Console.WriteLine("Login started");
                return false;
            }

            catch (Exception ex)
            {
                Console.WriteLine("Faled to start login: " + ex.Message);
                return true;            
            }
        }

        public bool Login_Down()
        {
            try
            {
                Login_UP = false;
                if (log_listen != null) log_listen.Close();
                if (login_th != null) login_th.Abort();

                Console.WriteLine("Log stopped");
                return false;
            }

            catch (Exception ex)
            {
                Console.WriteLine("Failed to stop login: "+ex.Message);
                return true;
            }
        }

        public void Listen_Connections()
        {
            string msg = "", data = "", cmd = "";
            int ID = 0;
            Console.WriteLine("Log started");

            try
            {
                while (Login_UP)
                {
                    byte[] pack = log_listen.Receive(ref remoteIp);
                    msg = Encoding.UTF8.GetString(pack);
                    cmd = Cons.cmd(msg);
                    data = Cons.data(msg);
                    Console.WriteLine(cmd + "-" + remoteIp.Address + "::" + remoteIp.Port + "  " + data);

                    switch (cmd)
                    {
                        //Проверка сервера на живость
                        case "CHECK":
                            Send("ALIVE", "", remoteIp);
                            break;

                        //Клиент хочет начать сессию
                        case "BEGIN":
                            if (game_Obj.Clients_ONLINE.ContainsKey(data) && game_Obj.Clients_ONLINE[data] == true)
                                Send("DENIED", "", remoteIp);
                            else
                            {
                                if (ID == max_users)
                                    Send("MAX_ONLINE", game_Serv.total_players.ToString(), remoteIp);
                                else
                                {
                                    //Запускаем поток-сессию для нового клиента
                                    if(game_Serv.Find_thSpace(ref ID, data, remoteIp)) Send("STARTED", ID.ToString(), remoteIp);
                                    else Send("MAX_ONLINE", game_Serv.total_players.ToString(), remoteIp);
                                    /*//Ищим свободный поток для клиента
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
                                    }*/
                                }
                            }
                            break;

                        //Клиент хочет кончить сессию
                        case "END":
                            //Конец сессии нужно перенести на гейм серв
                          /*  if (Clients_ONLINE.ContainsKey(Cons.data(msg)) && Clients_ONLINE[Cons.data(msg)] == true)
                            {
                                Clients_ONLINE[Cons.data(msg)] = false;
                                total_players--;
                                session_threads[Clients_ID[Cons.data(msg)]].Join();
                                session_threads[Clients_ID[Cons.data(msg)]].Interrupt();
                                session_threads[Clients_ID[Cons.data(msg)]].Abort();
                                Console.WriteLine("END1   " + Clients_ID[Cons.data(msg)] + "  " + session_threads[Clients_ID[Cons.data(msg)]].IsAlive + " Total online: " + total_players);
                                Send("SUCCESSFUL", "", remoteIp);
                            }
                            else Send("ALREADY_OFFLINE", "", remoteIp);*/
                            break;
                    }
                }
            }
            catch (SocketException ex)
            {
                Listen_Connections();
            }
        }

        public void Send(string command, string msg, IPEndPoint addr)
        {
            int offset = 0;

            byte[] s_num = new byte[6];
            byte[] s_comm = new byte[6];
            byte[] s_data = new byte[500];
            byte[] s_packet = new byte[512];

            s_num = Encoding.UTF8.GetBytes("packet=" + nlogSendPacks.ToString() + ";");
            s_comm = Encoding.UTF8.GetBytes("command=" + command + ";");
            s_data = Encoding.UTF8.GetBytes("data=" + msg);

            for (int i = 0; i < s_num.Length; i++, offset++) s_packet[offset] = s_num[i];
            for (int i = 0; i < s_comm.Length; i++, offset++) s_packet[offset] = s_comm[i];
            for (int i = 0; i < s_data.Length; i++, offset++) s_packet[offset] = s_data[i];

            try
            {
                log_listen.Send(s_packet, offset, addr); //можно указать s_packet.Length, чтобы отправлять весь пакет (с пустотами)
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение в отправке: " + ex.ToString() + "\n  " + ex.Message);
            }
        }

    }
}
