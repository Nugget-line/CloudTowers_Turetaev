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

    // Герой: золото, очки, пироги
    public class Hero : Character
    {
        public int Gold;
        public int Score;
        public int Pies;

        public Hero()
        {
            Name = "Герой";
            MaxHp = Game.StartHealth;
            Hp = Game.StartHealth;
            Def = 2;
            Gold = Game.StartGold;
            Score = Game.StartScore;
            Pies = Game.StartPies;
        }
    }

    // Враг: награда за победу и «настроение» (сколько раз с ним поговорили)
    public class Enemy : Character
    {
        public int Kind;          // 0 - кот, 1 - голубь, 2 - босс (для рисования)
        public int RewardGold;
        public int RewardScore;
        public int Talks;
        public string Intro;
        public string[] TalkLines;

        public bool CanBeSpared
        {
            get { return Talks >= 2; }
        }

        // Здание 0: Облачный кот, Громовой голубь. Здание 1: Король Бури.
        public static int CountInBuilding(int building)
        {
            return building == 0 ? 2 : 1;
        }

        public static Enemy Create(int building, int index)
        {
            if (building == 0 && index == 0)
            {
                return new Enemy
                {
                    Kind = 0, Name = "Облачный кот", MaxHp = 30, Hp = 30, Atk = 8, Def = 0,
                    RewardGold = 10, RewardScore = 20,
                    Intro = "* Облачный кот выпрыгивает из тумана!",
                    TalkLines = new[]
                    {
                        "* Ты почесал кота за ухом. Он мурлычет.",
                        "* Кот щурится. Ему больше не хочется драться."
                    }
                };
            }
            if (building == 0)
            {
                return new Enemy
                {
                    Kind = 1, Name = "Громовой голубь", MaxHp = 40, Hp = 40, Atk = 10, Def = 1,
                    RewardGold = 15, RewardScore = 30,
                    Intro = "* Громовой голубь сердито курлычет.",
                    TalkLines = new[]
                    {
                        "* Ты предложил крошки. Голубь задумался.",
                        "* Голубь успокоился и чистит перья."
                    }
                };
            }
            return new Enemy
            {
                Kind = 2, Name = "Король Бури", MaxHp = 80, Hp = 80, Atk = 14, Def = 2,
                RewardGold = 50, RewardScore = 100,
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
