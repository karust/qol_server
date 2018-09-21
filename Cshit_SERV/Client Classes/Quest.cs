using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cshit_SERV
{
    class Quest
    {
        public static int counter_idEx = Objects.Last_QuestEX;
        int id_ex = 0;

        int id;
        int state = 0;
        int counter = 0;
        bool finished = false;
        bool updClient = false;
        int finishState;
        string name;

        bool popUp = false;

        List<Item> reward=new List<Item>();

        string[] desc;

        public virtual void QuestCheck(Player player)
        {
        }


        public virtual bool FinishCheck(Player player)
        {
            if (finishState == state) {
                foreach (Item item in Reward) {
                    player.Inventory.Add(item);
                }
                finished = true;
                return true;
            }
            return false;
        }

        public void StateUp() {
            state++;
            Objects.UpdateDB_Quest(Id_ex,Id,State,Counter);
            UpdClient = true;
        }

        /*protected void PopUpFunc() {
            if (PopUp) return;
            Task.Factory.StartNew(() =>
            {
                PopUp = true;
                Thread.Sleep(4000);
                PopUp = false;
                
            });
        }*/

        public int State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
            }
        }

        public bool Finished
        {
            get
            {
                return finished;
            }

            set
            {
                finished = value;
            }
        }

        public int FinishState
        {
            get
            {
                return finishState;
            }

            set
            {
                finishState = value;
            }
        }

        public string[] Desc
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

        public bool PopUp
        {
            get
            {
                return popUp;
            }

            set
            {
                popUp = value;
            }
        }

        public List<Item> Reward
        {
            get
            {
                return reward;
            }

            set
            {
                reward = value;
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

        public int Counter
        {
            get
            {
                return counter;
            }

            set
            {
                counter = value;
            }
        }

        public bool UpdClient
        {
            get
            {
                return updClient;
            }

            set
            {
                updClient = value;
            }
        }

        public int Id_ex
        {
            get
            {
                return id_ex;
            }

            set
            {
                id_ex = value;
            }
        }
    }  

    class DarkSignsQuest : Quest {
        int startkills;
        int killlimit = 7;
        int counter = 0;
        int counter_copy = 0;

        //Costyl
        bool Constr_Type = false;

        //Конструктор для новых игроков
        public DarkSignsQuest(Player player)
        {
            Constr_Type = false;
            this.Id_ex = counter_idEx++;
            base.Reward.Add(new Gold(696969, true));
            base.FinishState = 4;
            base.Name = "Dark Signs";
            base.Desc = new string[base.FinishState];
            base.Id = 1;
            Objects.AddDB_Quest(player.Name, this.Id_ex, base.Id, 1, State, Counter);
        }

        //Конструктор для старых игроков с обновлением данных
        public DarkSignsQuest(int id_Ex, int State, int Counter)
        {
            Constr_Type = true;
            this.Id_ex = id_Ex;
            this.State = State;
            counter = Counter;

            base.Reward.Add(new Gold(696969, true));
            base.FinishState = 4;
            base.Name = "Dark Signs";
            base.Desc = new string[base.FinishState];
            base.Id = 1;
        }

        public override void QuestCheck(Player player)
        {
            //Bagosy =\
            if (counter < 0) counter = 0;

            switch (State) {
                case 1:
                    {
                        startkills = player.KilledEnemies[1];                       
                        StateUp();
                        break;
                    }
                case 2: {
                        if (counter != player.KilledEnemies[1] - startkills)
                        {
                            if (Constr_Type) { player.KilledEnemies[1] = counter; Constr_Type = false; }

                            counter = player.KilledEnemies[1] - startkills;
                            base.Counter = counter;
                            if (counter != counter_copy) { Objects.UpdateDB_Quest(Id_ex, Id, State, Counter); UpdClient = true; }
                            counter_copy = counter;
                               
                            if (counter >= killlimit) StateUp();
                        }
                        break;
                    }
                case 4:
                    {
                        UpdClient = true;
                        FinishCheck(player);
                        break;
                    }
                default: break;
            }
        }
    }
}
