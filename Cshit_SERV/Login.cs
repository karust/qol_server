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

    class Login
    {
        Game game_Serv;
        Objects game_Obj;

        //Для механизма важных сообщений    Аддрес клиента:Сообщение
        Dictionary<IPort, string> clients_iport_login = new Dictionary<IPort, string>(1000);

        UdpClient log_listen = null;
        IPEndPoint remoteIp;
        Thread login_th;

        private int port = 0;
        bool login_UP = false;

        public Login(int PORT, Game This_GameServ)
        {
            //Адрес логин серва
            port = PORT;
            //log_listen = new UdpClient(port);
            remoteIp = null;

            //Для управления\общения с гейм сервом нужна ссылка на него
            game_Serv = This_GameServ;
            //Инициализируем некоторые параметры
            game_Serv.total_players = 0;
            //Связываем с обьектами серва
            game_Obj = game_Serv.obj;
        }

        public bool Login_Up()
        {
            if (login_UP) { Console.WriteLine("Login server: Already UP"); return false; }
            try
            {
                login_UP = true;
                remoteIp = null;
                log_listen = new UdpClient(port);

                login_th = new Thread(new ThreadStart(Listen_Connections));
                login_th.Start();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Login server: Failed to START -" + ex.Message);
                return false;
            }
        }

        public bool Login_Down()
        {
            if (!login_UP) { Console.WriteLine("Login server: you must UP server first"); return false; }
            try
            {
                login_UP = false;
                if (log_listen != null) log_listen.Close();
                if (login_th != null) login_th.Abort();
                Console.WriteLine("Login server: stopped");
                return false;
            }

            catch (Exception ex)
            {
                Console.WriteLine("Login server: Failed to stop - " + ex.Message);
                return true;
            }
        }

        public void Listen_Connections()
        {
            string name = "";
            Console.WriteLine("Login server: started");

            try
            {
                while (login_UP)
                {
                    byte[] pack = log_listen.Receive(ref remoteIp);

                    Console.WriteLine("Login server: [" + Encoding.UTF8.GetString(pack) + "] from:" + remoteIp.Address + "::" + remoteIp.Port);
                    if (!clients_iport_login.ContainsKey(Cons.ToIport(remoteIp))) clients_iport_login.Add(Cons.ToIport(remoteIp), null);

                    switch (pack[0])
                    {
                        //1		CHECK 		| --
                        case 1:
                            Send(255, "ALIVE", remoteIp);
                            break;

                        //2		BEGIN 		| 1-end[char_name]
                        case 2:
                            name = Cons.GetData(pack);
                            //Если содержится игрок с таким именем и он онлайн
                            if (game_Obj._Char.ContainsKey(name) && game_Obj._Char[name].online == true)
                            {
                                if (game_Obj._Char[name].finding_thread == false && game_Obj._Char[name].client_informed == true) Send(253, "Session is active", remoteIp);
                                //Если отключили игрока, то ставиться флаг онлайна - false.
                                //game_Obj._Char[name].online = !game_Serv.Disconnect(name);
                            }
                            else
                            {
                                //Запускаем поток-сессию для нового клиента
                                int result = game_Serv.Find_thSpace(name, remoteIp);
                                switch (result)
                                {
                                    case 0://Успешно
                                        SendS(254, game_Obj._Char[name].ID.ToString(), remoteIp, name);
                                        game_Obj._Char[name].name = name;
                                        break;
                                    case 1:
                                        Send(253, "Char is disconnecting, " + game_Serv.total_players.ToString(), remoteIp);//Удалил вариант с этой ошибкой
                                        break;
                                    case 2:
                                        Send(253, "Max online= " + game_Serv.total_players.ToString(), remoteIp);
                                        break;
                                    case 3:
                                        Send(253, "Game server is down, " + game_Serv.total_players.ToString(), remoteIp);
                                        break;
                                }
                            }
                            break;

                        //5  	  	BEGIN_OK 	| 1[id]
                        case 5:
                            //Записывается дата сюда, надо для механизма важных сообщений                          
                            Console.WriteLine("BEGIN_OK:  " + clients_iport_login[Cons.ToIport(remoteIp)]);
                            clients_iport_login[Cons.ToIport(remoteIp)] = Cons.GetData(pack);
                            break;
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Login server: socket ex in listener:: " + ex.Message);
                if (login_UP) Listen_Connections();
            }
        }

        public void Send(byte command, byte[] msg, IPEndPoint addr)
        {
            byte[] s_packet = new byte[1 + msg.Length];
            for (int i = 1; i < msg.Length + 1; i++) s_packet[i] = msg[i - 1];
            s_packet[0] = command;

            try
            {
                log_listen.Send(s_packet, s_packet.Length, addr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение в отправке по аддресу: " + addr + ex.ToString() + "\n " + ex.Message);
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
                log_listen.Send(s_packet, s_packet.Length, addr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение в отправке по аддресу: " + addr + ex.ToString() + "\n " + ex.Message);
            }
        }

        //Сенд для важных сообщений
        //Послаю клиенту сообщение пока он не ответит - ОК+Сообщение которое я ему отправил
        public async void SendS(byte command, string message, IPEndPoint addr, string name)
        {
            byte[] msg = Encoding.UTF8.GetBytes(message);
            byte[] s_packet = new byte[1 + msg.Length];
            for (int i = 1; i < msg.Length + 1; i++) s_packet[i] = msg[i - 1];
            s_packet[0] = command;

            try
            {
                for (int i = 0; i < 100; i++)
                {
                    //Console.WriteLine("ImpSend: " + i + " | " + addr + "::" + message);
                    log_listen.Send(s_packet, s_packet.Length, addr);
                    await Task.Delay(10);
                    //Console.WriteLine("Received: " + i + " | " + addr + ":: " + clients_iport_login[Cons.ToIport(addr)]);
                    if (clients_iport_login[Cons.ToIport(addr)] == "OK") { game_Obj._Char[name].client_informed = true; clients_iport_login[Cons.ToIport(addr)] = null; break; }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение в отправке по аддресу: " + addr + "::" + ex.ToString());
            }
        }
    }
}
