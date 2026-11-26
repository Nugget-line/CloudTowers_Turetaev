using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CloudTowers
{
    // Окно игры: рисует сцены и принимает ввод (клавиатура и мышь)
    public class GameForm : Form
    {
        private class Btn
        {
            public string Label;
            public int Key;
            public Rectangle Rect;
        }

        private const int W = 800;
        private const int H = 600;

        private readonly Game game = new Game();
        private readonly Timer timer = new Timer();
        private readonly float[] cloudX = new float[8];
        private readonly float[] cloudY = new float[8];
        private readonly float[] cloudS = new float[8];
        private readonly float[] cloudV = new float[8];

        private readonly Font fTitle = new Font("Consolas", 40f, FontStyle.Bold);
        private readonly Font fBig = new Font("Consolas", 22f, FontStyle.Bold);
        private readonly Font fMed = new Font("Consolas", 16f, FontStyle.Bold);
        private readonly Font fSmall = new Font("Consolas", 12f, FontStyle.Bold);

        private readonly StringFormat center = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        private int frame;
        private int selected;
        private int shown;
        private string lastMsg = "";
        private Scene lastScreen = Scene.Menu;
        private bool lastHasMsg;

        public GameForm()
        {
            Text = "Башни в облаках " + Game.Version;
            ClientSize = new Size(W, H);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            KeyPreview = true;

            var r = new Random(7);
            for (int i = 0; i < cloudX.Length; i++)
            {
                cloudX[i] = r.Next(0, W);
                cloudY[i] = r.Next(20, 280);
                cloudS[i] = 0.6f + (float)r.NextDouble() * 0.9f;
                cloudV[i] = 0.2f + (float)r.NextDouble() * 0.6f;
            }

            timer.Interval = 33;
            timer.Tick += OnTick;
            timer.Start();
        }

        // ---------- Обновление ----------

        private void OnTick(object sender, EventArgs e)
        {
            frame++;
            game.Tick();

            if (game.ExitRequested)
            {
                timer.Stop();
                Close();
                return;
            }

            for (int i = 0; i < cloudX.Length; i++)
            {
                cloudX[i] += cloudV[i];
                if (cloudX[i] > W + 40) cloudX[i] = -160 * cloudS[i];
            }

            // Сброс выбора кнопки при смене экрана или фазы боя
            if (game.Scene != lastScreen || game.HasMessage != lastHasMsg)
            {
                selected = 0;
                lastScreen = game.Scene;
                lastHasMsg = game.HasMessage;
            }

            // Эффект печатающегося текста
            string msg = game.CurrentMessage;
            if (msg != lastMsg)
            {
                lastMsg = msg;
                shown = 0;
            }
            if (shown < msg.Length) shown = Math.Min(msg.Length, shown + 2);

            Invalidate();
        }

        // ---------- Ввод ----------

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            HandleKey(e.KeyCode);
        }

        // Стрелки форма перехватывает сама (для переключения фокуса), поэтому ловим их здесь
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down)
            {
                HandleKey(keyData);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void HandleKey(Keys code)
        {
            if (game.Scene == Scene.NameEntry)
            {
                if (code == Keys.Enter) game.ConfirmName();
                else if (code == Keys.Back) game.Backspace();
                return;
            }

            List<Btn> btns = GetButtons();

            if (code == Keys.Enter || code == Keys.Space || code == Keys.Z)
            {
                if (game.HasMessage) AdvanceMessage();
                else if (btns.Count > 0) game.Command(btns[Math.Min(selected, btns.Count - 1)].Key);
            }
            else if (code == Keys.Left || code == Keys.Up)
            {
                if (btns.Count > 0) selected = (selected + btns.Count - 1) % btns.Count;
            }
            else if (code == Keys.Right || code == Keys.Down)
            {
                if (btns.Count > 0) selected = (selected + 1) % btns.Count;
            }
            else
            {
                int n = ReadDigit(code, 0, 4);
                if (n >= 0 && !game.HasMessage) game.Command(n);
            }
        }

        // Возвращает цифру нажатой клавиши (верхний ряд или NumPad), если она от min до max, иначе -1
        private static int ReadDigit(Keys code, int min, int max)
        {
            int n = -1;
            if (code >= Keys.D0 && code <= Keys.D9) n = code - Keys.D0;
            else if (code >= Keys.NumPad0 && code <= Keys.NumPad9) n = code - Keys.NumPad0;
            return n >= min && n <= max ? n : -1;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (game.Scene == Scene.NameEntry && !char.IsControl(e.KeyChar))
                game.TypeChar(e.KeyChar);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (game.HasMessage) return;
            List<Btn> btns = GetButtons();
            for (int i = 0; i < btns.Count; i++)
                if (btns[i].Rect.Contains(e.Location)) selected = i;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (game.Scene == Scene.NameEntry) return;
            if (game.HasMessage)
            {
                AdvanceMessage();
                return;
            }
            foreach (Btn b in GetButtons())
                if (b.Rect.Contains(e.Location)) game.Command(b.Key);
        }

        private void AdvanceMessage()
        {
            if (shown < game.CurrentMessage.Length) shown = game.CurrentMessage.Length;
            else game.Advance();
        }

        // ---------- Кнопки ----------

        private List<Btn> GetButtons()
        {
            var list = new List<Btn>();
            switch (game.Scene)
            {
                case Scene.Menu:
                    list.Add(new Btn { Label = "1  Новая игра", Key = 1 });
                    list.Add(new Btn { Label = "2  Таблица рекордов", Key = 2 });
                    list.Add(new Btn { Label = "0  Выход", Key = 0 });
                    for (int i = 0; i < list.Count; i++)
                        list[i].Rect = new Rectangle(220, 290 + i * 64, 360, 50);
                    break;

                case Scene.Records:
                    list.Add(new Btn { Label = "0  В меню", Key = 0, Rect = new Rectangle(300, 510, 200, 48) });
                    break;

                case Scene.Building:
                    list.Add(new Btn { Label = "1 Дальше", Key = 1 });
                    list.Add(new Btn { Label = "2 Магазин", Key = 2 });
                    list.Add(new Btn { Label = "4 Пирог", Key = 4 });
                    list.Add(new Btn { Label = "0 В меню", Key = 0 });
                    LayoutRow(list);
                    break;

                case Scene.Battle:
                    if (!game.HasMessage)
                    {
                        list.Add(new Btn { Label = "1 Атака", Key = 1 });
                        list.Add(new Btn { Label = "2 Говорить", Key = 2 });
                        list.Add(new Btn { Label = "3 Пощада", Key = 3 });
                        list.Add(new Btn { Label = "4 Пирог", Key = 4 });
                        LayoutRow(list);
                    }
                    break;

                case Scene.Shop:
                    list.Add(new Btn { Label = "1 Купить пирог", Key = 1 });
                    list.Add(new Btn { Label = "0 Назад", Key = 0 });
                    LayoutRow(list);
                    break;
            }
            return list;
        }

        private static void LayoutRow(List<Btn> list)
        {
            int gap = 12;
            int w = (700 - gap * (list.Count - 1)) / list.Count;
            for (int i = 0; i < list.Count; i++)
                list[i].Rect = new Rectangle(50 + i * (w + gap), 480, w, 44);
        }

        // ---------- Рисование ----------

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            switch (game.Scene)
            {
                case Scene.Menu: DrawMenu(g); break;
                case Scene.Records: DrawRecords(g); break;
                case Scene.NameEntry: DrawNameEntry(g); break;
                default: DrawPlay(g); break;
            }
        }

        private bool Storm
        {
            get
            {
                return (game.Scene == Scene.Building || game.Scene == Scene.Battle || game.Scene == Scene.Shop)
                       && game.Building == 1;
            }
        }

        private void DrawSky(Graphics g, int bottom)
        {
            bool storm = Storm;
            Color top = storm ? Color.FromArgb(35, 25, 65) : Color.FromArgb(80, 160, 235);
            Color bot = storm ? Color.FromArgb(120, 105, 165) : Color.FromArgb(235, 246, 255);
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, bottom), top, bot, 90f))
                g.FillRectangle(br, 0, 0, W, bottom);

            // Дальние башни
            Color far = storm ? Color.FromArgb(60, 50, 95) : Color.FromArgb(190, 215, 245);
            DrawTower(g, 40, bottom - 190, 80, 190, far, false);
            DrawTower(g, 140, bottom - 250, 70, 250, far, false);
            DrawTower(g, 590, bottom - 220, 90, 220, far, false);
            DrawTower(g, 700, bottom - 170, 70, 170, far, false);

            // Облака
            Color cc = storm ? Color.FromArgb(150, 140, 185) : Color.White;
            for (int i = 0; i < cloudX.Length; i++)
                if (cloudY[i] < bottom - 60) DrawCloud(g, cloudX[i], cloudY[i], cloudS[i], cc);

            // Облачный пол
            for (int x = -40; x < W + 40; x += 90)
                DrawCloud(g, x, bottom - 45 + (x / 90 % 2) * 8, 1.1f, cc);

            // Вспышка молнии в грозовой башне
            if (storm && frame % 140 < 3)
                using (var fl = new SolidBrush(Color.FromArgb(110, Color.White)))
                    g.FillRectangle(fl, 0, 0, W, bottom);
        }

        private static void DrawCloud(Graphics g, float x, float y, float s, Color c)
        {
            using (var b = new SolidBrush(c))
            {
                g.FillEllipse(b, x, y + 14 * s, 130 * s, 40 * s);
                g.FillEllipse(b, x + 18 * s, y - 4 * s, 62 * s, 52 * s);
                g.FillEllipse(b, x + 55 * s, y + 2 * s, 58 * s, 46 * s);
            }
        }

        private static void DrawTower(Graphics g, int x, int y, int w, int h, Color body, bool windowsLit)
        {
            using (var b = new SolidBrush(body))
                g.FillRectangle(b, x, y, w, h);
            using (var p = new Pen(Color.FromArgb(60, 0, 0, 0), 2))
                g.DrawRectangle(p, x, y, w, h);
            using (var p = new Pen(Color.FromArgb(120, 120, 130), 3))
                g.DrawLine(p, x + w / 2, y, x + w / 2, y - 24);

            Color win = windowsLit ? Color.FromArgb(255, 225, 120) : Color.FromArgb(70, Color.White);
            using (var wb = new SolidBrush(win))
            {
                for (int wy = y + 14; wy < y + h - 30; wy += 30)
                    for (int wx = x + 12; wx < x + w - 18; wx += 28)
                        g.FillRectangle(wb, wx, wy, 14, 18);
            }
        }

        private void DrawPanelBottom(Graphics g)
        {
            g.FillRectangle(Brushes.Black, 0, 330, W, H - 330);
        }

        private void DrawBox(Graphics g, Rectangle r)
        {
            g.FillRectangle(Brushes.Black, r);
            using (var p = new Pen(Color.White, 4))
                g.DrawRectangle(p, r);
        }

        private void DrawButtons(Graphics g)
        {
            List<Btn> btns = GetButtons();
            for (int i = 0; i < btns.Count; i++)
            {
                bool sel = i == Math.Min(selected, btns.Count - 1);
                Color c = sel ? Color.Yellow : Color.FromArgb(255, 140, 0);
                using (var p = new Pen(c, 3))
                    g.DrawRectangle(p, btns[i].Rect);
                using (var b = new SolidBrush(c))
                {
                    var tr = new RectangleF(btns[i].Rect.X + (sel ? 14 : 0), btns[i].Rect.Y, btns[i].Rect.Width - (sel ? 14 : 0), btns[i].Rect.Height);
                    g.DrawString(btns[i].Label, fMed, b, tr, center);
                }
                if (sel) DrawHeart(g, btns[i].Rect.X + 10, btns[i].Rect.Y + btns[i].Rect.Height / 2 - 8, 16, Color.Red);
            }
        }

        private static void DrawHeart(Graphics g, float x, float y, float s, Color c)
        {
            using (var b = new SolidBrush(c))
            {
                g.FillEllipse(b, x, y, s / 2f + 1, s / 2f + 1);
                g.FillEllipse(b, x + s / 2f - 1, y, s / 2f + 1, s / 2f + 1);
                g.FillPolygon(b, new[]
                {
                    new PointF(x, y + s / 4f), new PointF(x + s, y + s / 4f), new PointF(x + s / 2f, y + s)
                });
            }
        }

        private void DrawStatus(Graphics g)
        {
            Hero h = game.Hero;
            g.DrawString("ГЕРОЙ", fSmall, Brushes.White, 50, 542);
            g.DrawString("HP", fSmall, Brushes.White, 125, 542);

            Color back = game.HeroHurt > 0 && game.HeroHurt % 4 < 2 ? Color.White : Color.Red;
            using (var bb = new SolidBrush(back))
                g.FillRectangle(bb, 155, 542, 100, 20);
            int fill = (int)(100.0 * h.Hp / h.MaxHp);
            g.FillRectangle(Brushes.Yellow, 155, 542, fill, 20);
            g.DrawString(h.Hp + "/" + h.MaxHp, fSmall, Brushes.White, 262, 542);

            g.DrawString("Золото: " + h.Gold + "  Очки: " + h.Score + "  Пироги: " + h.Pies + "  Раунд: " + game.Round,
                fSmall, Brushes.White, 345, 542);
        }

        private void DrawPlay(Graphics g)
        {
            DrawSky(g, 330);
            DrawPanelBottom(g);

            Rectangle box = new Rectangle(50, 345, 700, 120);
            DrawBox(g, box);
            var textRect = new RectangleF(box.X + 16, box.Y + 12, box.Width - 32, box.Height - 24);

            // Название места
            g.DrawString(game.BuildingName, fSmall, Brushes.White, 10, 6);

            if (game.Scene == Scene.Building || game.Scene == Scene.Shop)
            {
                Color body = Storm ? Color.FromArgb(80, 70, 115) : Color.FromArgb(245, 245, 252);
                DrawTower(g, 490, 60, 190, 270, body, true);
                using (var d = new SolidBrush(Color.FromArgb(110, 70, 40)))
                    g.FillRectangle(d, 560, 275, 50, 55);
                DrawHero(g, 220, 305, 1.6f);
            }

            if (game.Scene == Scene.Battle && game.Enemy != null)
            {
                DrawEnemy(g, game.Enemy, 400, 305);
                DrawEnemyBar(g, game.Enemy);
            }

            string text;
            if (game.Scene == Scene.Shop) text = game.ShopInfo;
            else if (game.Scene == Scene.Building) text = game.Info;
            else if (game.HasMessage) text = game.CurrentMessage.Substring(0, Math.Min(shown, game.CurrentMessage.Length));
            else text = game.BattlePrompt;
            g.DrawString(text, fMed, Brushes.White, textRect);

            DrawButtons(g);
            DrawStatus(g);
        }

        private void DrawEnemyBar(Graphics g, Enemy en)
        {
            Brush nameBrush = en.CanBeSpared ? Brushes.Yellow : Brushes.White;
            var nameRect = new RectangleF(250, 14, 300, 28);
            g.DrawString(en.Name, fMed, nameBrush, nameRect, center);
            g.FillRectangle(Brushes.Red, 320, 48, 160, 12);
            g.FillRectangle(Brushes.LimeGreen, 320, 48, (int)(160.0 * en.Hp / en.MaxHp), 12);
        }

        private void DrawMenu(Graphics g)
        {
            DrawSky(g, H);
            var shadow = new RectangleF(4, 84, W, 80);
            g.DrawString("БАШНИ В ОБЛАКАХ", fTitle, Brushes.DimGray, shadow, center);
            g.DrawString("БАШНИ В ОБЛАКАХ", fTitle, Brushes.White, new RectangleF(0, 80, W, 80), center);
            g.DrawString("CloudTowers " + Game.Version + " — текстовая RPG", fMed, Brushes.White, new RectangleF(0, 160, W, 30), center);
            DrawHero(g, 400, 260, 1.2f);

            var rect = new Rectangle(190, 270, 420, 215);
            using (var bb = new SolidBrush(Color.FromArgb(200, Color.Black)))
                g.FillRectangle(bb, rect);
            DrawButtons(g);

            g.DrawString("Стрелки + Enter, цифры или мышь", fSmall, Brushes.Black, new RectangleF(0, 540, W, 24), center);
            g.DrawString("Абильмансур Туретаев, ПО 25-Z", fSmall, Brushes.Black, new RectangleF(0, 565, W, 24), center);
        }

        private void DrawRecords(Graphics g)
        {
            DrawSky(g, H);
            DrawBox(g, new Rectangle(100, 60, 600, 430));
            g.DrawString("ТАБЛИЦА РЕКОРДОВ", fBig, Brushes.Yellow, new RectangleF(100, 75, 600, 50), center);

            if (game.Records.Count == 0)
            {
                g.DrawString("Пока пусто. Пройди игру!", fMed, Brushes.White, new RectangleF(100, 200, 600, 40), center);
            }
            else
            {
                for (int i = 0; i < game.Records.Count; i++)
                {
                    RecordEntry r = game.Records[i];
                    float y = 150 + i * 56;
                    g.DrawString((i + 1) + ".", fBig, Brushes.White, 130, y);
                    g.DrawString(r.Name, fBig, Brushes.White, 190, y);
                    g.DrawString(r.Score.ToString(), fBig, Brushes.Yellow, 450, y);
                    g.DrawString(r.Date, fSmall, Brushes.Gray, 560, y + 8);
                }
            }
            DrawButtons(g);
        }

        private void DrawNameEntry(Graphics g)
        {
            DrawSky(g, H);
            DrawBox(g, new Rectangle(130, 110, 540, 360));

            Brush head = game.Won ? Brushes.LimeGreen : Brushes.Red;
            g.DrawString(game.Won ? "ПОБЕДА!" : "ПОРАЖЕНИЕ", fBig, head, new RectangleF(130, 130, 540, 50), center);

            string line = game.Won
                ? (game.GotPeaceBonus ? "Мирное прохождение! +50 очков." : "Ты прошёл все башни.")
                : "Герой упал с облаков.";
            g.DrawString(line, fMed, Brushes.White, new RectangleF(130, 200, 540, 36), center);
            g.DrawString("Очки: " + game.Hero.Score, fBig, Brushes.Yellow, new RectangleF(130, 250, 540, 44), center);
            g.DrawString("Введи имя и нажми Enter:", fMed, Brushes.White, new RectangleF(130, 320, 540, 36), center);

            string name = game.PlayerName + (frame % 30 < 15 ? "_" : " ");
            g.DrawString(name, fBig, Brushes.White, new RectangleF(130, 370, 540, 50), center);
        }

        // ---------- Спрайты (рисуются фигурами) ----------

        private void DrawHero(Graphics g, float cx, float by, float s)
        {
            float bob = (float)Math.Sin(frame * 0.1) * 2f;
            by += bob;

            using (var cape = new SolidBrush(Color.FromArgb(200, 40, 50)))
                g.FillPolygon(cape, new[]
                {
                    new PointF(cx - 14 * s, by - 52 * s), new PointF(cx + 14 * s, by - 52 * s),
                    new PointF(cx + 22 * s, by - 8 * s), new PointF(cx - 22 * s, by - 8 * s)
                });
            using (var body = new SolidBrush(Color.FromArgb(60, 100, 200)))
                g.FillRectangle(body, cx - 12 * s, by - 52 * s, 24 * s, 32 * s);
            using (var legs = new SolidBrush(Color.FromArgb(50, 50, 80)))
            {
                g.FillRectangle(legs, cx - 10 * s, by - 22 * s, 8 * s, 18 * s);
                g.FillRectangle(legs, cx + 2 * s, by - 22 * s, 8 * s, 18 * s);
            }
            using (var skin = new SolidBrush(Color.FromArgb(250, 210, 170)))
                g.FillEllipse(skin, cx - 14 * s, by - 82 * s, 28 * s, 28 * s);
            using (var hair = new SolidBrush(Color.FromArgb(110, 70, 40)))
                g.FillPie(hair, cx - 15 * s, by - 84 * s, 30 * s, 30 * s, 180, 180);
            g.FillEllipse(Brushes.Black, cx - 7 * s, by - 70 * s, 4 * s, 5 * s);
            g.FillEllipse(Brushes.Black, cx + 3 * s, by - 70 * s, 4 * s, 5 * s);
        }

        private void DrawEnemy(Graphics g, Enemy en, float cx, float by)
        {
            if (game.EnemyHurt > 0) cx += game.EnemyHurt % 2 == 0 ? 9 : -9;
            float bob = (float)Math.Sin(frame * 0.12) * 4f;
            by += bob;

            if (en.Kind == 0) DrawCat(g, cx, by);
            else if (en.Kind == 1) DrawPigeon(g, cx, by);
            else DrawKing(g, cx, by);
        }

        private static void DrawCat(Graphics g, float cx, float by)
        {
            using (var white = new SolidBrush(Color.FromArgb(250, 250, 255)))
            using (var edge = new Pen(Color.FromArgb(150, 170, 200), 3))
            using (var pink = new SolidBrush(Color.FromArgb(255, 150, 170)))
            {
                g.FillEllipse(white, cx - 75, by - 110, 150, 95);
                g.DrawEllipse(edge, cx - 75, by - 110, 150, 95);
                var earL = new[] { new PointF(cx - 48, by - 150), new PointF(cx - 42, by - 198), new PointF(cx - 14, by - 165) };
                var earR = new[] { new PointF(cx + 48, by - 150), new PointF(cx + 42, by - 198), new PointF(cx + 14, by - 165) };
                g.FillPolygon(white, earL);
                g.FillPolygon(white, earR);
                g.FillEllipse(white, cx - 50, by - 175, 100, 75);
                g.DrawEllipse(edge, cx - 50, by - 175, 100, 75);
                g.FillEllipse(Brushes.Black, cx - 28, by - 150, 12, 18);
                g.FillEllipse(Brushes.Black, cx + 16, by - 150, 12, 18);
                g.FillEllipse(pink, cx - 6, by - 130, 12, 8);
                g.DrawLine(edge, cx - 55, by - 128, cx - 25, by - 126);
                g.DrawLine(edge, cx + 55, by - 128, cx + 25, by - 126);
                g.DrawArc(edge, cx - 85, by - 85, 40, 50, 100, 200);
            }
        }

        private static void DrawPigeon(Graphics g, float cx, float by)
        {
            using (var gray = new SolidBrush(Color.FromArgb(150, 155, 175)))
            using (var dark = new SolidBrush(Color.FromArgb(95, 100, 125)))
            using (var orange = new SolidBrush(Color.FromArgb(255, 160, 40)))
            using (var yellow = new SolidBrush(Color.Gold))
            using (var leg = new Pen(Color.FromArgb(255, 160, 40), 4))
            {
                g.DrawLine(leg, cx - 15, by - 40, cx - 15, by);
                g.DrawLine(leg, cx + 15, by - 40, cx + 15, by);
                g.FillEllipse(gray, cx - 70, by - 130, 140, 95);
                g.FillEllipse(dark, cx - 55, by - 115, 80, 50);
                g.FillEllipse(gray, cx + 25, by - 175, 55, 55);
                g.FillPolygon(orange, new[]
                {
                    new PointF(cx + 78, by - 152), new PointF(cx + 105, by - 143), new PointF(cx + 78, by - 134)
                });
                g.FillEllipse(Brushes.Black, cx + 52, by - 160, 9, 11);
                g.FillPolygon(yellow, new[]
                {
                    new PointF(cx + 22, by - 118), new PointF(cx + 40, by - 118), new PointF(cx + 30, by - 98),
                    new PointF(cx + 42, by - 98), new PointF(cx + 18, by - 66), new PointF(cx + 26, by - 96),
                    new PointF(cx + 14, by - 96)
                });
            }
        }

        private static void DrawKing(Graphics g, float cx, float by)
        {
            using (var cloud = new SolidBrush(Color.FromArgb(70, 55, 110)))
            using (var edge = new Pen(Color.FromArgb(30, 20, 60), 3))
            using (var gold = new SolidBrush(Color.Gold))
            using (var red = new SolidBrush(Color.Red))
            {
                g.FillEllipse(cloud, cx - 120, by - 150, 240, 120);
                g.FillEllipse(cloud, cx - 80, by - 215, 160, 120);
                g.DrawEllipse(edge, cx - 80, by - 215, 160, 120);

                g.FillPolygon(gold, new[]
                {
                    new PointF(cx - 45, by - 205), new PointF(cx - 45, by - 245), new PointF(cx - 22, by - 222),
                    new PointF(cx, by - 252), new PointF(cx + 22, by - 222), new PointF(cx + 45, by - 245),
                    new PointF(cx + 45, by - 205)
                });

                g.FillEllipse(Brushes.White, cx - 42, by - 175, 30, 22);
                g.FillEllipse(Brushes.White, cx + 12, by - 175, 30, 22);
                g.FillEllipse(red, cx - 32, by - 171, 14, 14);
                g.FillEllipse(red, cx + 22, by - 171, 14, 14);
                using (var brow = new Pen(Color.Black, 5))
                {
                    g.DrawLine(brow, cx - 46, by - 188, cx - 10, by - 176);
                    g.DrawLine(brow, cx + 46, by - 188, cx + 10, by - 176);
                }
                g.DrawArc(edge, cx - 25, by - 148, 50, 30, 200, 140);

                Brush bolt = gold;
                g.FillPolygon(bolt, new[]
                {
                    new PointF(cx - 135, by - 120), new PointF(cx - 105, by - 120), new PointF(cx - 118, by - 90),
                    new PointF(cx - 100, by - 90), new PointF(cx - 140, by - 40), new PointF(cx - 128, by - 82),
                    new PointF(cx - 145, by - 82)
                });
                g.FillPolygon(bolt, new[]
                {
                    new PointF(cx + 135, by - 120), new PointF(cx + 105, by - 120), new PointF(cx + 118, by - 90),
                    new PointF(cx + 100, by - 90), new PointF(cx + 140, by - 40), new PointF(cx + 128, by - 82),
                    new PointF(cx + 145, by - 82)
                });
            }
        }
    }
}
