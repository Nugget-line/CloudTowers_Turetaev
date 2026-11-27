using System;
using System.Collections.Generic;

namespace CloudTowers
{
    public enum Scene { Menu, Records, Building, Fork, Battle, Shop, NameEntry }

    // Вся логика игры. Рисованием и вводом занимается GameForm.
    public class Game
    {
        public const string Version = "v0.5";

        // Стартовые значения состояния (константы)
        public const int StartHealth = 100;
        public const int StartGold = 20;
        public const int StartScore = 0;
        public const int StartPies = 0;
        public const int StartCakes = 0;

        // F1: урон героя — случайное целое от HitMin до HitMax включительно
        public const int HitMin = 8;
        public const int HitMax = 12;

        // Магазин
        public const int PiePrice = 10;
        public const int PieHeal = 30;
        public const int CakePrice = 25;
        public const int CakeHeal = 60;

        private const int PeaceBonus = 50;
        private const int EncountersPerBuilding = 2;
        private const int MaxPaths = 3;

        // Один генератор случайных чисел на всю игру
        public static readonly Random Rnd = new Random();
        public static readonly string[] PathNames = { "Налево", "Прямо", "Направо" };
        private static readonly string[] BuildingNames = { "Нижние облака", "Грозовая башня" };

        public Scene Scene = Scene.Menu;
        public Hero Hero = new Hero();
        public Enemy Enemy;
        public int Building;
        public int EnemyIndex;
        public int Round = 1;
        public bool KilledAnyone;
        public bool Won;
        public bool GotPeaceBonus;
        public string Info = "";
        public string ShopInfo = "";
        public string ForkNote = "";
        public string PlayerName = "";
        public bool ExitRequested;
        public int HeroHurt;
        public int EnemyHurt;
        public List<RecordEntry> Records = new List<RecordEntry>();

        // Список ещё не встреченных жителей (R4) и варианты текущей развилки
        private readonly List<Enemy> monsters = new List<Enemy>();
        public readonly List<Enemy> ForkOptions = new List<Enemy>();

        private readonly Queue<string> msgs = new Queue<string>();
        private Action onDone;

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

        // Подсказка в рамке, когда игрок выбирает действие в бою
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
                    else if (key == 2) OpenShop();
                    else if (key == 4) UseItem(false, false);
                    else if (key == 5) UseItem(false, true);
                    else if (key == 0) Scene = Scene.Menu;
                    break;

                case Scene.Fork:
                    if (key == 0) { ForkOptions.Clear(); ForkNote = ""; Scene = Scene.Building; }
                    else if (key <= ForkOptions.Count) ChoosePath(key);
                    else ForkNote = "* Нужно число от 1 до " + ForkOptions.Count + " (0 — назад).";
                    break;

                case Scene.Shop:
                    if (key == 1) BuyItem(false);
                    else if (key == 2) BuyItem(true);
                    else if (key == 0) Scene = Scene.Building;
                    break;

                case Scene.Battle:
                    if (HasMessage || Enemy == null) return;
                    if (key == 1) Attack();
                    else if (key == 2) Talk();
                    else if (key == 3) Spare();
                    else if (key == 4) UseItem(true, false);
                    else if (key == 5) UseItem(true, true);
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

        // ---------- Списки (R4) ----------

        // Заполняет список жителей стартовыми данными (5 штук, без босса)
        private void FillList()
        {
            monsters.Clear();
            for (int id = 0; id < Enemy.BossId; id++)
                monsters.Add(Enemy.Create(id));
        }

        // Текст развилки: пронумерованный список путей с подсказками
        public string ShowList()
        {
            string text = ForkNote != "" ? ForkNote : "* Куда пойти?";
            for (int i = 0; i < ForkOptions.Count; i++)
                text += "\n" + (i + 1) + ") " + PathNames[i] + ": " + ForkOptions[i].Hint + ".";
            return text;
        }

        // Выбирает до трёх разных жителей из списка для развилки
        private void OpenFork()
        {
            ForkOptions.Clear();
            ForkNote = "";
            if (monsters.Count == 0)
            {
                EnemyIndex++;
                Info = "* Все жители уже встречены. Путь свободен.";
                return;
            }
            var pool = new List<Enemy>(monsters);
            int count = Math.Min(MaxPaths, pool.Count);
            for (int i = 0; i < count; i++)
            {
                int k = Rnd.Next(pool.Count);
                ForkOptions.Add(pool[k]);
                pool.RemoveAt(k);
            }
            Scene = Scene.Fork;
        }

        // Игрок выбрал путь с номером number (1..ForkOptions.Count)
        private void ChoosePath(int number)
        {
            Enemy chosen = ForkOptions[number - 1];
            if (monsters.Contains(chosen)) monsters.Remove(chosen);
            ForkOptions.Clear();
            ForkNote = "";
            StartBattle(chosen);
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
            ForkOptions.Clear();
            ForkNote = "";
            msgs.Clear();
            onDone = null;
            FillList();
        }

        private void NewGame()
        {
            ResetState();
            Info = "* Ты стоишь на краю здания «Нижние облака». Впереди слышны шорохи.";
            Scene = Scene.Building;
        }

        private void OpenShop()
        {
            ShopInfo = "* Пирог — " + PiePrice + " золота (+" + PieHeal + " HP). Торт со вкусом пельменей — "
                       + CakePrice + " золота (+" + CakeHeal + " HP).";
            Scene = Scene.Shop;
        }

        private void Go()
        {
            if (EnemyIndex >= EncountersPerBuilding)
            {
                Building++;
                EnemyIndex = 0;
                Info = "* Ты поднялся в «Грозовую башню». Воздух гудит от молний.";
            }
            else if (Building == 1 && EnemyIndex == EncountersPerBuilding - 1)
            {
                StartBattle(Enemy.Create(Enemy.BossId));
            }
            else
            {
                OpenFork();
            }
        }

        private void StartBattle(Enemy opponent)
        {
            Enemy = opponent;
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

        // Показывает сообщение: в бою — как реплику (при endsTurn затем ходит враг), вне боя — в рамке здания
        private void Notify(bool inBattle, string text, bool endsTurn)
        {
            if (inBattle) Say(endsTurn ? (Action)EnemyTurn : null, text);
            else Info = text;
        }

        // Съедает пирог (cake == false) или торт (cake == true)
        private void UseItem(bool inBattle, bool cake)
        {
            int have = cake ? Hero.Cakes : Hero.Pies;
            if (have <= 0)
            {
                Notify(inBattle, "* У тебя нет " + (cake ? "тортов" : "пирогов") + ". Их можно купить в магазине.", false);
                return;
            }
            if (Hero.Hp >= Hero.MaxHp)
            {
                Notify(inBattle, "* Здоровье и так полное.", false);
                return;
            }
            int before = Hero.Hp;
            Hero.Hp = Math.Min(Hero.MaxHp, Hero.Hp + (cake ? CakeHeal : PieHeal));
            if (cake) Hero.Cakes--; else Hero.Pies--;
            string food = cake ? "торт со вкусом пельменей. Вкус странный, но сытный" : "пирог";
            Notify(inBattle, "* Ты съел " + food + ". +" + (Hero.Hp - before) + " HP.", true);
        }

        // Покупает пирог (cake == false) или торт (cake == true)
        private void BuyItem(bool cake)
        {
            int price = cake ? CakePrice : PiePrice;
            if (Hero.Gold < price)
            {
                ShopInfo = "* Не хватает золота.";
                return;
            }
            Hero.Gold -= price;
            if (cake) Hero.Cakes++; else Hero.Pies++;
            ShopInfo = "* Ты купил " + (cake ? "торт со вкусом пельменей" : "пирог") + ". Спасибо!";
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
            bool wasBoss = Enemy.IsBoss;
            Enemy = null;
            EnemyIndex++;
            Scene = Scene.Building;
            if (wasBoss) Finish(true);
            else if (Building == 1) Info = "* Впереди дверь в тронный зал. Слышен гром.";
            else if (EnemyIndex < EncountersPerBuilding) Info = "* Ты идёшь дальше по облачным коридорам.";
            else Info = "* В этом здании больше никого. Лестница ведёт выше.";
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
