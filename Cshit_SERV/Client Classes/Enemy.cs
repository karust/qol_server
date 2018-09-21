using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cshit_SERV
{
    public class Enemy
    {
        protected int id_Ex = Objects.Last_NpcEX;
        static int id = 0;
        string name = "";
        protected string texture;
        float hp;
        protected float maxHp;
        protected float attack, speed;
        protected float x, y;
        protected float base_x, base_y;
        bool active;

        protected int RespawnRate;// Респаун в секундах
        protected long DeathTime;

        public void WorkFunc(List<Player> Players, List<Item> Drop, Random RNG) {
            WorkCycle(Players);
            if (active)
                if (DeathCheck())
                {
                    DropFunc(Drop, RNG);
                }
        }

        public virtual void WorkCycle(List<Player> Player) { }

        public virtual void DropFunc(List<Item> DropList, Random RNG) { }

        public virtual void CalcAnim() { }

        public bool DeathCheck()
        {
            if (hp > 0) return false;
            active = false;
            DeathTime = DateTime.Now.Ticks;
            return true;
        }

        public bool DeathCheckLight()
        {

            if (hp > 0) return false;
            return true;
        }
        
        public float HP
        {
            get
            {
                return hp;
            }

            set
            {
                hp = value;
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

        public virtual float Y
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

        public bool Active
        {
            get
            {
                return active;
            }

            set
            {
                active = value;
            }
        }

        public float MaxHp
        {
            get
            {
                return maxHp;
            }
        }

        public string Texture
        {
            get
            {
                return texture;
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

        public float Attack
        {
            get
            {
                return attack;
            }

            set
            {
                attack = value;
            }
        }

        public float Speed
        {
            get
            {
                return speed;
            }

            set
            {
                speed = value;
            }
        }

        public int Id_Ex
        {
            get
            {
                return id_Ex;
            }

            set
            {
                id_Ex = value;
            }
        }
    }


class Ghost : Enemy
{
    float dy;
    float dx;

    float speed;//= .05f;
    float atk;// = .05f;

    float ddysum = 0;
    float ddy = .01f;
    Player aggro;

    public override float Y {
        get { return y + ddysum; }
        set { y = value; }
    }

    public override void DropFunc(List<Item> DropList, Random RNG) {
        int pool = RNG.Next(100);
        if (pool < 13) DropList.Add(new Potion(PotionType.Health,RNG.Next(1,3),X,Y));
        pool = RNG.Next(100);
        if (pool < 13) DropList.Add(new Gold(X, Y, RNG.Next(30, 100)));
    }

    public override void CalcAnim() {
        ddysum += ddy;
        if (ddysum > .4f || ddysum < -.4f) { ddy *= -1; }
        //Console.WriteLine(ddysum.ToString());
    }

    public override void WorkCycle(List<Player> Player) {
            if (Active)
            {
                foreach (Player player in Player)
                {
                    float dist = (float)Math.Sqrt((player.X - base.X) * (player.X - base.X) + (player.Y - base.Y) * (player.Y - base.Y));
                    if (aggro == null) if (dist < 5 && player.Alive)
                        {
                            aggro = player;
                        }
                        else { }
                    else
                    {
                        float dist1 = (float)Math.Sqrt((aggro.X - base.X) * (aggro.X - base.X) + (aggro.Y - base.Y) * (aggro.Y - base.Y));
                        if (dist1 > 5||!aggro.Alive)
                        {
                            aggro = null;
                            dx = 0;
                            dy = 0;
                        }
                        else
                        {
                            dx = (aggro.X - base.X) / dist1;
                            dy = (aggro.Y - base.Y) / dist1;
                            if (dist1 < 1 && aggro.Alive) aggro.Hp -= atk;
                            if (!aggro.Alive)
                            {
                                aggro = null;
                                dx = 0;
                                dy = 0;
                            }
                        }
                    }
                }
                base.X += dx * speed;
                base.Y += dy * speed;

            }
            else {
            //Console.WriteLine("DeathTime = "+DeathTime.ToString()+";  Now = "+ DateTime.Now.Ticks.ToString() + ";  Sub = "+ (DateTime.Now.Ticks - DeathTime).ToString());
                if (DateTime.Now.Ticks - DeathTime > RespawnRate*10000000) {
                    dx = 0;
                    dy = 0;
                    Y = base_y;
                    X = base_x;
                    HP = MaxHp;
                    aggro = null;
                    Active = true;
                }
            }
    }

       /*public Ghost()
        {
            base.Name = "Ghost";
            base.Id++;
            base.Active = true;
            base.Id = 1;
            base.maxHp = 100;
            base.HP = base.MaxHp;
            base.texture = "Ghost";
            this.Speed = this.speed = .05f;
            this.Attack = this.atk = .1f;
            //base.id_Ex++;
            this.Id_Ex++;
            //Objects.AddExEnemy(this);
        }*/

        public Ghost(float X, float Y,Random RNG)
        {
            base.Name = "Ghost";
            base.Active = true;
            RespawnRate = 15;//В СЕКУНДАХ
            base.Id = 1;//id типа
            ddy = RNG.Next(600,1000)*.00001f;
            base.maxHp = 100;
            base.HP = base.MaxHp;
            base.texture = "Ghost";
            base_x = X;
            base_y = Y;
            base.X = X;
            base.Y = Y;
            this.Speed = this.speed = .05f;
            this.Attack = this.atk = .1f;

            Objects.Last_NpcEX++;
            id_Ex = Objects.Last_NpcEX;
            //Objects.AddExEnemy(this);
        }

        /*public Ghost(float X, float Y)
        {
            base.Name = "Ghost";
            base.Active = true;
            RespawnRate = 15;
            base.Id = 1;
            base.maxHp = 100;
            base.HP = base.MaxHp;
            base.texture = "Ghost";
            base_x = X;
            base_y = Y;
            base.X = X;
            base.Y = Y;
            this.Speed = this.speed = .05f;
            this.Attack = this.atk = .1f;
            //base.id_Ex++;
            this.Id_Ex++;
            //Objects.AddExEnemy(this);
        }*/
}
}