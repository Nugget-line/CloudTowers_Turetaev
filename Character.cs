namespace CloudTowers
{
    // Базовый класс: общие свойства героя и врагов
    public class Character
    {
        public string Name;
        public int MaxHp;
        public int Hp;
        public int Atk;
        public int Def;
    }

    // Герой: хранит здоровье, золото, очки, пироги и торты.
    // Создаётся заново в Game.ResetState перед каждой партией.
    public class Hero : Character
    {
        public int Gold;
        public int Score;
        public int Pies;
        public int Cakes;

        public Hero()
        {
            Name = "Герой";
            MaxHp = Game.StartHealth;
            Hp = Game.StartHealth;
            Def = 2;
            Gold = Game.StartGold;
            Score = Game.StartScore;
            Pies = Game.StartPies;
            Cakes = Game.StartCakes;
        }
    }

    // Враг: награда за победу, подсказка для развилки и «настроение» (сколько раз с ним поговорили)
    public class Enemy : Character
    {
        public const int BossId = 5;

        public int Kind;          // для рисования: 0 кот, 1 голубь, 2 босс, 3 лиса, 4 филин, 5 слизень
        public int RewardGold;
        public int RewardScore;
        public int Talks;
        public string Hint;
        public string Intro;
        public string[] TalkLines;

        public bool CanBeSpared
        {
            get { return Talks >= 2; }
        }

        public bool IsBoss
        {
            get { return Kind == 2; }
        }

        // id 0–4 — обычные жители, id 5 (BossId) — босс «Король Бури»
        public static Enemy Create(int id)
        {
            switch (id)
            {
                case 0:
                    return new Enemy
                    {
                        Kind = 0, Name = "Облачный кот", MaxHp = 30, Hp = 30, Atk = 8, Def = 0,
                        RewardGold = 10, RewardScore = 20, Hint = "слышно мурлыканье",
                        Intro = "* Облачный кот выпрыгивает из тумана!",
                        TalkLines = new[]
                        {
                            "* Ты почесал кота за ухом. Он мурлычет.",
                            "* Кот щурится. Ему больше не хочется драться."
                        }
                    };
                case 1:
                    return new Enemy
                    {
                        Kind = 1, Name = "Громовой голубь", MaxHp = 40, Hp = 40, Atk = 10, Def = 1,
                        RewardGold = 15, RewardScore = 30, Hint = "сердитое курлыканье",
                        Intro = "* Громовой голубь сердито курлычет.",
                        TalkLines = new[]
                        {
                            "* Ты предложил крошки. Голубь задумался.",
                            "* Голубь успокоился и чистит перья."
                        }
                    };
                case 2:
                    return new Enemy
                    {
                        Kind = 3, Name = "Туманная лиса", MaxHp = 35, Hp = 35, Atk = 9, Def = 0,
                        RewardGold = 12, RewardScore = 25, Hint = "шорох в тумане",
                        Intro = "* Из тумана выскальзывает Туманная лиса!",
                        TalkLines = new[]
                        {
                            "* Ты похвалил её пушистый хвост. Лиса довольно фыркает.",
                            "* Лиса садится рядом. Драться ей уже неинтересно."
                        }
                    };
                case 3:
                    return new Enemy
                    {
                        Kind = 4, Name = "Ветряной филин", MaxHp = 45, Hp = 45, Atk = 11, Def = 1,
                        RewardGold = 18, RewardScore = 35, Hint = "низкое уханье",
                        Intro = "* Ветряной филин расправляет крылья и ухает.",
                        TalkLines = new[]
                        {
                            "* Ты спросил, который час. Филин важно задумался.",
                            "* Филин решил, что ты вежливый путник."
                        }
                    };
                case 4:
                    return new Enemy
                    {
                        Kind = 5, Name = "Облачный слизень", MaxHp = 25, Hp = 25, Atk = 7, Def = 0,
                        RewardGold = 10, RewardScore = 20, Hint = "тихое бульканье",
                        Intro = "* Облачный слизень булькает и ползёт к тебе.",
                        TalkLines = new[]
                        {
                            "* Ты улыбнулся слизню. Он забулькал веселее.",
                            "* Слизень растёкся лужицей — драться он не хочет."
                        }
                    };
                default:
                    return new Enemy
                    {
                        Kind = 2, Name = "Король Бури", MaxHp = 80, Hp = 80, Atk = 14, Def = 2,
                        RewardGold = 50, RewardScore = 100, Hint = "гремит гром",
                        Intro = "* Король Бури заслоняет небо. Гремит гром!",
                        TalkLines = new[]
                        {
                            "* Ты сказал, что просто хочешь подняться выше. Король хмурится.",
                            "* Король Бури вздыхает. Ему тоже надоели грозы."
                        }
                    };
            }
        }
    }
}
