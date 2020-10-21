using System;
using System.Collections.Generic;
using System.IO;

namespace MyOCProject
{
    class EventSchenduler
    {
        private readonly Potok[] pMas = new Potok[3];
        private static readonly string pathLog = @"C:\Users\Maxim\source\repos\Parser\PostData\ProgramLog\PlanningLog.txt";
        private int count;
        private const int planSize = 15;
        private int maxSize;
        private int statusT4 = -1;
        private readonly string programmT = "PT";
        private readonly string serviceT = "ST";
        private readonly string additionalT = "T4";

        public EventSchenduler()
        {
            if (File.Exists(pathLog))
            {
                File.Delete(pathLog);
            }

            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("PT - работа основного потока парсера, ST - работа основного потока службы, T4 - работа мешающего потока \n\nЛог работы программы: ");
            }
            maxSize = planSize;
            count = 0;
            for (int i = 0; i < 3; i++)
            {
                pMas[i] = new Potok(i + 1);
            }
        }
        public void GetIteration()
        {
            string[] tmp = new string[3];
            for (int i = 0; i < 3; i++)
            {
                tmp[i] = pMas[i].Queue.Dequeue();
            }
            count--;
        }
        public class Potok
        {
            public int index;
            public Queue<string> Queue { get; set; } = new Queue<string>();

            public Potok(int index)
            {
                this.index = index;
            }
        }

        public void CreatePlan()
        {
            while (count < maxSize)
            {
                if (statusT4 == 4)
                {
                    statusT4 = 0;
                }
                else
                {
                    statusT4++;
                }

                for (int j = 0; j < 3; j++)
                {
                    if (statusT4 == 4) { pMas[j].Queue.Enqueue(serviceT); continue; }
                    if (statusT4 == pMas[j].index) { pMas[j].Queue.Enqueue(additionalT); }
                    else { pMas[j].Queue.Enqueue(programmT); }
                }
                count++;
            }
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("\n");
                for (int i = 0; i < 3; i++)
                {
                    string str = string.Join(", ", pMas[i].Queue.ToArray());
                    str.Trim();
                    sw.WriteLine("[" + DateTime.Now.ToString() + "] Поток " + (i + 1).ToString() + ": " + str);
                }
            }
        }
        public void SetMaxSize(int i)
        {
            if (i != 0)
            {
                maxSize = i;
            }
            else
            {
                maxSize = planSize;
            }
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("\nУстановлена длина плана " + maxSize.ToString());
            }
        }
        public int GetMaxSize()
        {
            return maxSize;
        }
    }
}
