using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cshit_SERV
{
    class GameObj
    {
        public  Dictionary<string, string> Clients_IP;
        public Dictionary<string, int> Clients_PORT;
        public Dictionary<string, bool> Clients_ONLINE;
        public Dictionary<string, int> Clients_ID;
        public Dictionary<string, int> Clients_ConnectFailedTimes;

        //Координаты Персонажей
        public Dictionary<string, string[]> Characters_XYD;
        //Координаты Мобов
        public Dictionary<string, string[]> Mobs_XYD;

        public GameObj(int MAX_USERS)
        {
            Clients_IP = new Dictionary<string, string>(MAX_USERS);
            Clients_PORT = new Dictionary<string, int>(MAX_USERS);
            Clients_ONLINE = new Dictionary<string, bool>(MAX_USERS);
            Clients_ID = new Dictionary<string, int>(MAX_USERS);
            Clients_ConnectFailedTimes = new Dictionary<string, int>(MAX_USERS);

            Characters_XYD = new Dictionary<string, string[]>(MAX_USERS);

            Mobs_XYD = new Dictionary<string, string[]>(MAX_USERS * 5);
        }

    }
}
