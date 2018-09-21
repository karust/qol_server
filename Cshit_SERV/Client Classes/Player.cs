using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Cshit_SERV
{
    public class Player
    {
        public NPC LastNPC;


        byte direction = 0;

        bool online = true;

        int id;
        float x=32, y=32;
        float sendX = float.MinValue, sendY = float.MinValue;
        float rotation;
        float basespeed = .05f;
        float speedbuff = 0F;
        float hp = 100;
        float maxHp = 100;
        float hpUp=0;

        public int level = 1;//

        string name;

        int[] killedEnemies = new int[100];

        float sX, sY;

        bool up = false;
        bool dialogMenu=false;
        bool down = false;
        bool left = false;
        bool right = false;
        bool attack = false;
        bool alive = true;
        float atkDmg = 28;

        string texture;
        
        int frameskip = 0;
        int frameskiplimit;
        int RunAnState = 0;
        int attackAtState = 0;
        int attackFrames = 2;
        int attackRate = 200;

        Inventory inventory;
        List<Quest> quests=new List<Quest>();

        public void CheckDeath() {
            if (Hp <= 0) alive = false;
        }

        public void CalcRotation(int X, int Y, int x, int y) {
            rotation=(float)(Math.Atan2((Y - y) , (X - x)) / Math.PI * 180); 
        }

        public Player(float pX, float pY, Inventory _inventory, string _name)
        {
            name = _name;             
            x = pX; y = pY;
            sX = x; sY = y;
            inventory = _inventory;
        }

        public void Initialize_Quests()
        {
            quests.Add(new DarkSignsQuest(this));
        }

        public void Interpolation() {
            if (Math.Abs(sX - x) > basespeed || Math.Abs(sY - y) > basespeed)
            {
                x += (sX - x) * basespeed*3;
                y += (sY - y) * basespeed*3;
            }
            else {
                return; }
        }


        public Player(float pX, float pY,string _name)
        {
            name = _name;
            //quests.Add(new DarkSignsQuest(this));
            x = pX; y = pY;
            inventory = new Inventory(this);
        }
        
        internal void CheckQuests()
        {
            foreach (Quest quest in quests) {
                if(quest.State!=0 && !quest.Finished) quest.QuestCheck(this);
            }
        }

        public void MovedByControl(bool W, bool A, bool S, bool D, byte[,] map) {
            float speed = basespeed + speedbuff;
            if (alive)
            {
                if (W && !A && !D && !S) if (CheckCollisionU(map))
                    {
                        Y += speed;
                        UP = true;
                    }
                    else UP = false;
                else UP = false;

                if (A && !W && !S && !D) if (CheckCollisionL(map))
                    {
                        X -= speed;
                        LEFT = true;
                    }
                    else LEFT = false;
                else LEFT = false;

                if (S && !A && !D && !W) if (CheckCollisionD(map))
                    {
                        Y -= speed;
                        DOWN = true;
                    }
                    else DOWN = false;
                else DOWN = false;

                if (D && !W && !S && !A) if (CheckCollisionR(map))
                    {
                        X += speed;
                        RIGHT = true;
                    }
                    else RIGHT = false;
                else RIGHT = false;


                bool diagonal = false;

                if (!LEFT && !RIGHT && !UP && !DOWN) diagonal = true;

                if (W && D && !A && !S)
                {
                    if (CheckCollisionU(map))
                    {
                        Y += speed * 0.707f;
                        if (diagonal) RIGHT = true;
                    }
                    if (CheckCollisionR(map))
                    {
                        X += speed * 0.707f;
                        if (diagonal) RIGHT = true;
                    }
                }
                if (W && A && !D && !S)
                {
                    if (CheckCollisionU(map))
                    {
                        Y += speed * 0.707f;
                        if (diagonal) LEFT = true;
                    }
                    if (CheckCollisionL(map))
                    {
                        X -= speed * 0.707f;
                        if (diagonal) LEFT = true;
                    }
                }
                if (S && D && !A && !W)
                {
                    if (CheckCollisionD(map))
                    {
                        Y -= speed * 0.707f;
                        if (diagonal) RIGHT = true;
                    }
                    if (CheckCollisionR(map))
                    {
                        X += speed * 0.707f;
                        if (diagonal) RIGHT = true;

                    }
                }
                if (S && A && !D && !W)
                {
                    if (CheckCollisionD(map))
                    {
                        Y -= speed * 0.707f;
                        if (diagonal) LEFT = true;
                    }
                    if (CheckCollisionL(map))
                    {
                        X -= speed * 0.707f;
                        if (diagonal) LEFT = true;
                    }
                }
            }
        }

        public void AttackFunc(Enemy[] EnemyList)
        {
            if(!Attack&&alive)Task.Run(() => 
            {
                try
                {
                    Attack = true;
                    float rotationtoenemy;
                    for (int i = 0; i < AttackFrames; i++)
                    {
                        AttackAtState = i;
                        Thread.Sleep(attackRate);
                    }

                    for (int i = 0; i < EnemyList.Length; i++)
                    {
                        if (EnemyList[i] == null) break;
                        if (!EnemyList[i].Active) continue;
                        rotationtoenemy = (float)(Math.Atan2((EnemyList[i].Y - y), (EnemyList[i].X - x)) / Math.PI * 180);
                        if (Math.Sqrt((x - EnemyList[i].X) * (x - EnemyList[i].X) + (y - EnemyList[i].Y) * (y - EnemyList[i].Y)) < 1.8f&&
                            (Math.Abs(rotation-rotationtoenemy)<50||Math.Abs(rotationtoenemy-rotation)>310))
                        {
                            EnemyList[i].HP -= AtkDmg;
                            if (EnemyList[i].DeathCheckLight()) KilledEnemies[EnemyList[i].Id]++;
                        }
                    }
                    Attack = false;
                }
                catch (Exception e) { Console.WriteLine(e.ToString()); }
            });
        }

        public bool CheckCollisionR(byte[,] map) {
            float speed = basespeed + speedbuff;
            int X = (int)(x + speed +.3f);
            int Y = (int)y;
            if(X<0||X>map.GetUpperBound(0)||Y<0||Y>map.GetUpperBound(1))return true;
            if (map[X, Y] == 1) return false;
            return true;
        }

        public bool CheckCollisionL(byte[,] map)
        {
            float speed = basespeed + speedbuff;
            int X = (int)(x - speed - .3f);
            int Y = (int)y;
            if (X < 0 || X > map.GetUpperBound(0) || Y < 0 || Y > map.GetUpperBound(1)) return true;
            if (map[X, Y] == 1) return false;
            return true;
        }

        public bool CheckCollisionU(byte[,] map)
        {
            float speed = basespeed + speedbuff;
            int X = (int)x;
            int Y = (int)(y+speed + .18f);
            if (X < 0 || X > map.GetUpperBound(0) || Y < 0 || Y > map.GetUpperBound(1)) return true;
            if (map[X, Y] == 1) return false;
            return true;
        }

        public bool CheckCollisionD(byte[,] map)
        {
            float speed = basespeed + speedbuff;
            int X = (int)x;
            int Y = (int)(y - speed - .1f);
            if (X < 0 || X > map.GetUpperBound(0) || Y < 0 || Y > map.GetUpperBound(1)) return true;
            if (map[X, Y] == 1) return false;
            return true;
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
                sX = value;
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
                y = value;
                sY = value;
            }
        }

        public float Rotation
        {
            get
            {
                return rotation;
            }

            set
            {
                rotation = value;
            }
        }

        public float SpeedBuff
        {
            get
            {
                return speedbuff;
            }

            set
            {
                speedbuff = value;
            }
        }

        public bool UP
        {
            get
            {
                return up;
            }

            set
            {
                up = value;
            }
        }

        public bool DOWN
        {
            get
            {
                return down;
            }

            set
            {
                down = value;
            }
        }

        public bool LEFT
        {
            get
            {
                return left;
            }

            set
            {
                left = value;
            }
        }

        public bool RIGHT
        {
            get
            {
                return right;
            }

            set
            {
                right = value;
            }
        }

        public int RunAtState
        {
            get
            {
                return RunAnState;
            }

            set
            {
                RunAnState = value;
            }
        }

        public bool Attack
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

        public int AttackFrames
        {
            get
            {
                return attackFrames;
            }

            set
            {
                attackFrames = value;
            }
        }

        public int AttackRate
        {
            get
            {
                return attackRate;
            }

            set
            {
                attackRate = value;
            }
        }

        public int AttackAtState
        {
            get
            {
                return attackAtState;
            }

            set
            {
                attackAtState = value;
            }
        }

        public float Hp
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

        public float MaxHp
        {
            get
            {
                return maxHp;
            }

            set
            {
                maxHp = value;
            }
        }

        public bool Alive
        {
            get
            {
                return alive;
            }

            set
            {
                alive = value;
            }
        }

        public float HpUp
        {
            get
            {
                return hpUp;
            }

            set
            {
                hpUp = value;
            }
        }

        public Inventory Inventory
        {
            get
            {
                return inventory;
            }

            set
            {
                inventory = value;
            }
        }

        public int[] KilledEnemies
        {
            get
            {
                return killedEnemies;
            }

            set
            {
                killedEnemies = value;
            }
        }

        internal List<Quest> Quests
        {
            get
            {
                return quests;
            }
        }

        public bool DialogMenu
        {
            get
            {
                return dialogMenu;
            }

            set
            {
                dialogMenu = value;
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

        public string Texture
        {
            get
            {
                return texture;
            }

            set
            {
                texture = value;
            }
        }

        public float SX
        {
            get
            {
                return sX;
            }

            set
            {
                sX = value;
            }
        }

        public float SY
        {
            get
            {
                return sY;
            }

            set
            {
                sY = value;
            }
        }

        public byte Direction
        {
            get
            {
                return direction;
            }

            set
            {
                direction = value;
            }
        }

        public bool Online
        {
            get
            {
                return online;
            }

            set
            {
                online = value;
            }
        }

        public float Speed
        {
            set
            {
                basespeed = speedbuff+value;
            }
            get
            {
                return basespeed+speedbuff;
            }
        }

        public float SendX
        {
            get
            {
                return sendX;
            }

            set
            {
                sendX = value;
            }
        }

        public float SendY
        {
            get
            {
                return sendY;
            }

            set
            {
                sendY = value;
            }
        }

        public float AtkDmg
        {
            get
            {
                return atkDmg;
            }

            set
            {
                atkDmg = value;
            }
        }
    }
}
