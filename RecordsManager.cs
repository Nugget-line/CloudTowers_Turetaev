using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CloudTowers
{
    public class RecordEntry
    {
        public string Name;
        public int Score;
        public string Date;
    }

    // Таблица рекордов: 5 лучших результатов в файле records.txt
    public static class RecordsManager
    {
        private const int MaxRecords = 5;

        private static string FilePath
        {
            get { return Path.Combine(AppContext.BaseDirectory, "records.txt"); }
        }

        public static List<RecordEntry> Load()
        {
            var list = new List<RecordEntry>();
            try
            {
                if (File.Exists(FilePath))
                {
                    foreach (string line in File.ReadAllLines(FilePath))
                    {
                        string[] p = line.Split('|');
                        int score;
                        if (p.Length == 3 && int.TryParse(p[1], out score))
                            list.Add(new RecordEntry { Name = p[0], Score = score, Date = p[2] });
                    }
                }
            }
            catch (Exception)
            {
                // Повреждённый файл не должен ронять игру
            }
            return list.OrderByDescending(r => r.Score).Take(MaxRecords).ToList();
        }

        public static void Add(string name, int score)
        {
            List<RecordEntry> list = Load();
            list.Add(new RecordEntry
            {
                Name = name,
                Score = score,
                Date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            });
            list = list.OrderByDescending(r => r.Score).Take(MaxRecords).ToList();
            try
            {
                File.WriteAllLines(FilePath, list.Select(r => r.Name + "|" + r.Score + "|" + r.Date));
            }
            catch (Exception)
            {
                // Нет прав на запись — игра продолжает работать без сохранения
            }
        }
    }
}
