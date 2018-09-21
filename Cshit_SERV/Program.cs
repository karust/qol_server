using System;

namespace Cshit_SERV
{   
    //Баги:
    /*
      Когда ниже 1 банки расходуешь не исчезает, а баги появляются
      Снимание цен за предметы не оч сделал, и нпс
      Запись НПЦ в базу бесполезна пока
      Нету квестов
      */

    class Program
    {
        static void Main(string[] args)
        {
            Game game = new Game(5000, 3);//4444
            Login login = new Login(5001, game);//4445
            game.Serv_Up();
            login.Login_Up();

            string exit_keys = "";
            while (exit_keys != "exit-lg" )
            {
                exit_keys = Console.ReadLine();
                switch (exit_keys)
                {
                    case "down-l": { login.Login_Down(); Console.Title = "Login: Down"; } break;
                    case "down-g": { game.Serv_Down(); Console.Title = "Game: Down"; } break;
                    case "down-lg": { login.Login_Down(); game.Serv_Down(); Console.Title = "Login and Game: Down"; } break;

                    case "up-l": { login.Login_Up(); Console.Title = "Login: UP"; } break;
                    case "up-g": { game.Serv_Up(); Console.Title = "Game: UP"; } break;
                    case "up-lg": { login.Login_Up(); game.Serv_Up(); Console.Title = "Login and Game: UP"; }break;

                    case "exit-lg": login.Login_Down(); game.Serv_Down(); break;

                    case "kick":
                        Console.Write("Enter name: ");
                        string disc = Console.ReadLine();
                        Console.WriteLine("Diconnect result: " + game.Disconnect(disc));
                        break;

                    default: Console.WriteLine("Unacceptable command"); break;
                }
            }
        }
    }
}
          
