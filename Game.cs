using System;
using System.Collections.Generic;

namespace CloudTowers
{
    public enum Scene { Menu, Records, Building, Battle, Shop, NameEntry }

    // Вся логика игры. Рисованием и вводом занимается GameForm.
    public class Game
    {
        public const string Version = "v0.4";

        // Стартовые значения состояния (константы)
        public const int StartHealth = 100;
        public const int StartGold = 20;
        public const int StartScore = 0;
        public const int StartPies = 0;

        // F1: урон героя — случайное целое от HitMin до HitMax включительно
        public const int HitMin = 8;
        public const int HitMax = 12;

        // Один генератор случайных чисел на всю игру
        public static readonly Random Rnd = new Random();

        private const int PiePrice = 10;
        private const int PieHeal = 30;
        private const int PeaceBonus = 50;

        public Scene Scene = Scene.Menu;
        public Hero Hero = new Hero();
        public Enemy Enemy;
        public int Building;
        public int Round = 1;
        public int EnemyIndex;
        public bool KilledAnyone;
        public bool Won;
        public bool GotPeaceBonus;
        public string Info = "";
        public string ShopInfo = "";
        public string PlayerName = "";
        public bool ExitRequested;
        public int HeroHurt;
        public int EnemyHurt;
        public List<RecordEntry> Records = new List<RecordEntry>();

        private readonly Queue<string> msgs = new Queue<string>();
        private Action onDone;

        private static readonly string[] BuildingNames = { "Нижние облака", "Грозовая башня" };

        public string BuildingName
        {
            get { return BuildingNames[Building]; }
        }

        public bool HasMessage
        {
            get { return msgs.Count > 0; }
        }

        public string CurrentMessage
        {
            get { return msgs.Count > 0 ? msgs.Peek() : ""; }
        }

        // Подсказка в рамке, когда игрок выбирает действие
        public string BattlePrompt
        {
            get
            {
                if (Enemy == null) return "";
                if (Enemy.CanBeSpared) return "* " + Enemy.Name + " готов к пощаде. Что будешь делать?";
                return "* Что будешь делать?";
            }
        }

        public void Tick()
        {
            if (HeroHurt > 0) HeroHurt--;
            if (EnemyHurt > 0) EnemyHurt--;
        }

        // ---------- Ввод ----------

        public void Command(int key)
        {
            switch (Scene)
            {
                case Scene.Menu:
                    if (key == 1) NewGame();
                    else if (key == 2) ShowRecords();
                    else if (key == 0) ExitRequested = true;
                    break;

                case Scene.Records:
                    if (key == 0) Scene = Scene.Menu;
                    break;

                case Scene.Building:
                    if (key == 1) Go();
                    else if (key == 2) { ShopInfo = "* Привет! Пирог стоит " + PiePrice + " золота."; Scene = Scene.Shop; }
                    else if (key == 4) UsePie(false);
                    else if (key == 0) Scene = Scene.Menu;
                    break;

                case Scene.Shop:
                    if (key == 1) BuyPie();
                    else if (key == 0) Scene = Scene.Building;
                    break;

                case Scene.Battle:
                    if (HasMessage || Enemy == null) return;
                    if (key == 1) Attack();
                    else if (key == 2) Talk();
                    else if (key == 3) Spare();
                    else if (key == 4) UsePie(true);
                    break;
            }
        }

        // Переход к следующему сообщению в бою
        public void Advance()
        {
            if (msgs.Count == 0) return;
            msgs.Dequeue();
            if (msgs.Count == 0 && onDone != null)
            {
                Action d = onDone;
                onDone = null;
                d();
            }
        }

        public void TypeChar(char c)
        {
            if (Scene != Scene.NameEntry || PlayerName.Length >= 12) return;
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-')
                PlayerName += c;
        }

        public void Backspace()
        {
            if (Scene == Scene.NameEntry && PlayerName.Length > 0)
                PlayerName = PlayerName.Substring(0, PlayerName.Length - 1);
        }

        public void ConfirmName()
        {
            if (Scene != Scene.NameEntry) return;
            string name = PlayerName.Trim();
            if (name.Length == 0) name = "Герой";
            RecordsManager.Add(name, Hero.Score);
            ShowRecords();
        }

        // ---------- Игровой процесс ----------

        private void Say(Action done, params string[] lines)
        {
            msgs.Clear();
            foreach (string l in lines) msgs.Enqueue(l);
            onDone = done;
        }

        private void ShowRecords()
        {
            Records = RecordsManager.Load();
            Scene = Scene.Records;
        }

        // Сбрасывает всё состояние партии к стартовым значениям
        private void ResetState()
        {
            Hero = new Hero();
            Enemy = null;
            Building = 0;
            EnemyIndex = 0;
            Round = 1;
            KilledAnyone = false;
            Won = false;
            GotPeaceBonus = false;
            msgs.Clear();
            onDone = null;
        }

        private void NewGame()
        {
            ResetState();
            Info = "* Ты стоишь на краю здания «Нижние облака». Впереди слышны шорохи.";
            Scene = Scene.Building;
        }

        private void Go()
        {
            if (EnemyIndex < Enemy_Count())
            {
                StartBattle();
            }
            else
            {
                Building++;
                EnemyIndex = 0;
                Info = "* Ты поднялся в «Грозовую башню». Воздух гудит от молний.";
            }
        }

        private int Enemy_Count()
        {
            return Enemy.CountInBuilding(Building);
        }

        private void StartBattle()
        {
            Enemy = Enemy.Create(Building, EnemyIndex);
            Scene = Scene.Battle;
            Say(null, Enemy.Intro);
        }

        private void Attack()
        {
            int dmg = Rnd.Next(HitMin, HitMax + 1);
            Enemy.Hp -= dmg;
            EnemyHurt = 14;
            if (Enemy.Hp <= 0)
            {
                Enemy.Hp = 0;
                KilledAnyone = true;
                Hero.Gold += Enemy.RewardGold;
                Hero.Score += Enemy.RewardScore;
                Say(EndBattle,
                    "* Ты ударил: -" + dmg + " HP.",
                    "* " + Enemy.Name + " побеждён! +" + Enemy.RewardGold + " золота, +" + Enemy.RewardScore + " очков.");
            }
            else
            {
                Say(EnemyTurn, "* Ты ударил: -" + dmg + " HP.");
            }
        }

        private void Talk()
        {
            Enemy.Talks++;
            string line = Enemy.TalkLines[Math.Min(Enemy.Talks, Enemy.TalkLines.Length) - 1];
            if (Enemy.Talks == 2)
                Say(EnemyTurn, line, "* Теперь " + Enemy.Name + " можно пощадить.");
            else
                Say(EnemyTurn, line);
        }

        private void Spare()
        {
            if (Enemy.CanBeSpared)
            {
                Hero.Score += Enemy.RewardScore;
                Say(EndBattle,
                    "* Ты пощадил: " + Enemy.Name + ".",
                    "* +" + Enemy.RewardScore + " очков (золота за пощаду нет).");
            }
            else
            {
                Say(EnemyTurn, "* " + Enemy.Name + " ещё не готов к пощаде. Попробуй поговорить.");
            }
        }

        private void UsePie(bool inBattle)
        {
            if (Hero.Pies <= 0)
            {
                if (inBattle) Say(null, "* У тебя нет пирогов.");
                else Info = "* У тебя нет пирогов. Их можно купить в магазине.";
                return;
            }
            if (Hero.Hp >= Hero.MaxHp)
            {
                if (inBattle) Say(null, "* Здоровье и так полное.");
                else Info = "* Здоровье и так полное.";
                return;
            }
            Hero.Pies--;
            int before = Hero.Hp;
            Hero.Hp = Math.Min(Hero.MaxHp, Hero.Hp + PieHeal);
            string text = "* Ты съел пирог. +" + (Hero.Hp - before) + " HP.";
            if (inBattle) Say(EnemyTurn, text);
            else Info = text;
        }

        private void BuyPie()
        {
            if (Hero.Gold < PiePrice)
            {
                ShopInfo = "* Не хватает золота.";
                return;
            }
            Hero.Gold -= PiePrice;
            Hero.Pies++;
            ShopInfo = "* Ты купил пирог. Спасибо!";
        }

        private void EnemyTurn()
        {
            int dmg = Math.Max(1, Enemy.Atk - Hero.Def);
            Hero.Hp -= dmg;
            HeroHurt = 14;
            if (Hero.Hp <= 0)
            {
                Hero.Hp = 0;
                Say(delegate { Finish(false); },
                    "* " + Enemy.Name + " атакует: -" + dmg + " HP.",
                    "* Ты падаешь сквозь облака...");
            }
            else
            {
                Round++;
                Say(null, "* " + Enemy.Name + " атакует: -" + dmg + " HP.");
            }
        }

        private void EndBattle()
        {
            Enemy = null;
            EnemyIndex++;
            Scene = Scene.Building;
            if (Building == 1)
            {
                Finish(true);
            }
            else if (EnemyIndex < Enemy_Count())
            {
                Info = "* Ты идёшь дальше по облачным коридорам.";
            }
            else
            {
                Info = "* В этом здании больше никого. Лестница ведёт выше.";
            }
        }

        private void Finish(bool won)
        {
            Won = won;
            GotPeaceBonus = won && !KilledAnyone;
            if (GotPeaceBonus) Hero.Score += PeaceBonus;
            PlayerName = "";
            Scene = Scene.NameEntry;
        }
    }
}
