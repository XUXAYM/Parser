using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace MyOCProject
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    [DataContract]
    class PostText
    {
        [DataMember]
        internal string ID;
        [DataMember]
        internal string Text;
    }

    [DataContract]
    class PostURLs
    {
        [DataMember]
        internal string ID;
        [DataMember]
        internal string[] TextURLs;
    }

    [DataContract]
    class PostPictures
    {
        [DataMember]
        internal string ID;
        [DataMember]
        internal string[] PictureURLs;
    }
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        ChromeDriver chromeDriver;
        List<PostText> tmpText;
        List<PostURLs> tmpURL;
        List<PostPictures> tmpPicture;
        readonly EventSchenduler schenduler = new EventSchenduler();
        int index = -1;
        bool whichSem = false;
        bool flag = false;
        readonly Queue<Thread> textQueue = new Queue<Thread>(2);
        readonly Queue<Thread> urlQueue = new Queue<Thread>(2);
        public System.Timers.Timer timer1 = new System.Timers.Timer();
        readonly Queue<Thread> pictureQueue = new Queue<Thread>(2);
        Thread threadOne;
        Thread threadTwo;
        Thread threadThree;
        Thread threadDie;
        readonly Semaphore semText = new Semaphore(1, 1);
        readonly Semaphore semUrl = new Semaphore(1, 1);
        readonly Semaphore semPicture = new Semaphore(1, 1);
        private const string textFile = @"PostData\PostText.json";
        private const string urlFile = @"PostData\PostURL.json";
        private const string pictureFile = @"PostData\PostPicture.json";
        private const string programmFile = @"PostData\ProgramRunning.txt";
        private const string serviceFile = @"PostData\ServiceRunning.txt";
        private const string serviceLive = @"PostData\ServiceLog\ServiceIsAlive.txt";


        //Методы работы программы с\без планировщика
        //////////////////////////////////////////////////////////////////////////////////////////
        private void StartThreads()
        {
            List<IWebElement> webElements = new List<IWebElement>();
            Thread.Sleep(500);
            List<IWebElement> allWebElements = chromeDriver.FindElementsByClassName("feed_row").ToList();
            for (int i = 0; i < allWebElements.Count - 1; i++)
            {
                if (!PostId(allWebElements.ElementAt(i)).Contains("repost") && PostId(allWebElements.ElementAt(i)).Contains("post"))
                {
                    webElements.Add(allWebElements.ElementAt(i));
                }
            }

            threadOne = new Thread(() => PostTextDeserialization(textFile));
            threadTwo = new Thread(() => PostURLDeserialization(urlFile));
            threadThree = new Thread(() => PostPictureDeserialization(pictureFile));

            tmpText = SortListPost(tmpText, FillingListPostText(webElements));
            tmpURL = SortListPost(tmpURL, FillingListPostURL(webElements));
            tmpPicture = SortListPost(tmpPicture, FillingListPostPicture(webElements));

            threadOne = new Thread(() => PostSerialization(tmpText, textFile));
            threadTwo = new Thread(() => PostSerialization(tmpURL, urlFile));
            threadThree = new Thread(() => PostSerialization(tmpPicture, pictureFile));
            threadDie = new Thread(FileWorking);
            if (index == 3)
            {
                index = 0;
            }
            else
            {
                index++;
            }
            threadDie.Start();
            threadOne.Start();
            threadTwo.Start();
            threadThree.Start();
        }
        private void EventScheduler()
        {
            List<IWebElement> webElements = new List<IWebElement>();
            Thread.Sleep(500);
            List<IWebElement> allWebElements = chromeDriver.FindElementsByClassName("feed_row").ToList();
            for (int i = 0; i < allWebElements.Count - 1; i++)
            {
                if (!PostId(allWebElements.ElementAt(i)).Contains("repost") && PostId(allWebElements.ElementAt(i)).Contains("post"))
                {
                    webElements.Add(allWebElements.ElementAt(i));
                }
            }

            if (index == 3)
            {
                flag = true;
                index = 0;
            }
            else
            {
                index++;
            }

            threadOne = new Thread(() => PostTextDeserialization(textFile));
            threadTwo = new Thread(() => PostURLDeserialization(urlFile));
            threadThree = new Thread(() => PostPictureDeserialization(pictureFile));

            semText.WaitOne();
            threadOne.Start();
            semUrl.WaitOne();
            threadTwo.Start();
            semPicture.WaitOne();
            threadThree.Start();

            tmpText = SortListPost(tmpText, FillingListPostText(webElements));
            tmpURL = SortListPost(tmpURL, FillingListPostURL(webElements));
            tmpPicture = SortListPost(tmpPicture, FillingListPostPicture(webElements));

            threadOne = new Thread(() => PostSerialization(tmpText, textFile));
            threadTwo = new Thread(() => PostSerialization(tmpURL, urlFile));
            threadThree = new Thread(() => PostSerialization(tmpPicture, pictureFile));
            threadDie = new Thread(FileWorking);

            switch (index)
            {
                case 0:
                    {
                        textQueue.Enqueue(threadOne);
                        urlQueue.Enqueue(threadTwo);
                        pictureQueue.Enqueue(threadThree);
                        break;
                    }
                case 1:
                    {
                        textQueue.Enqueue(threadDie);
                        urlQueue.Enqueue(threadTwo);
                        pictureQueue.Enqueue(threadThree);
                        break;
                    }
                case 2:
                    {
                        textQueue.Enqueue(threadOne);
                        urlQueue.Enqueue(threadDie);
                        pictureQueue.Enqueue(threadThree);
                        break;
                    }
                case 3:
                    {
                        textQueue.Enqueue(threadOne);
                        urlQueue.Enqueue(threadTwo);
                        pictureQueue.Enqueue(threadDie);
                        break;
                    }
            }
            schenduler.GetIteration();
            schenduler.CreatePlan();
            semText.WaitOne();
            textQueue.Dequeue().Start();
            semUrl.WaitOne();
            urlQueue.Dequeue().Start();
            semPicture.WaitOne();
            pictureQueue.Dequeue().Start();
            while (true)
            {
                if (!threadOne.IsAlive && !threadTwo.IsAlive && !threadThree.IsAlive && !threadDie.IsAlive)
                {
                    semText.Dispose();
                    semUrl.Dispose();
                    semPicture.Dispose();
                    break;
                }
                else
                {
                    continue;
                }
            }
        }
        //Метод для 4-го потока
        //////////////////////////////////////////////////////////////////////////////////////////
        private void FileWorking()
        {
            string path = null;
            int tmp = index;
            switch (tmp)
            {
                case 1:
                    {
                        path = textFile;
                        break;
                    }
                case 2:
                    {
                        path = urlFile;
                        break;
                    }
                case 3:
                    {
                        path = pictureFile;
                        break;
                    }
            }
            if (!File.Exists(path))
            {
                switch (tmp)
                {
                    case 1:
                        {
                            semText.Release();
                            break;
                        }
                    case 2:
                        {
                            semUrl.Release();
                            break;
                        }
                    case 3:
                        {
                            semPicture.Release();
                            break;
                        }
                }
                return;
            }
            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                byte[] array = new byte[stream.Length];
                // считываем данные
                stream.Read(array, 0, array.Length);
                Thread.Sleep(1000);
            }
            switch (tmp)
            {
                case 1:
                    {
                        semText.Release();
                        break;
                    }
                case 2:
                    {
                        semUrl.Release();
                        break;
                    }
                case 3:
                    {
                        semPicture.Release();
                        break;
                    }
            }
        }
        //Методы парсинга картинок
        //////////////////////////////////////////////////////////////////////////////////////////
        private void PostSerialization(List<PostPictures> pictureList, string fileWay)
        {
            if (pictureList == null)
            {
                semPicture.Release();
                return;
            }
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(PostPictures[]));
            using (FileStream stream = new FileStream(fileWay, FileMode.OpenOrCreate))
            {
                jsonFormatter.WriteObject(stream, pictureList.ToArray());
            }
            semPicture.Release();
        }
        private void PostPictureDeserialization(string fileWay)
        {
            if (!File.Exists(fileWay))
            {
                tmpPicture = null;
                semPicture.Release();
                return;
            }
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(PostPictures[]));
            using (FileStream stream = new FileStream(fileWay, FileMode.Open))
            {
                PostPictures[] p = (PostPictures[])jsonFormatter.ReadObject(stream);
                tmpPicture = p.ToList<PostPictures>();
                semPicture.Release();
            }
        }
        private List<PostPictures> FillingListPostPicture(List<IWebElement> webElements)
        {
            List<PostPictures> pictureList = new List<PostPictures>();
            if (webElements == null)
            {
                return null;
            }

            foreach (IWebElement element in webElements)
            {
                List<IWebElement> items = element.FindElements(By.XPath(".//div//*[contains(@style,'url')]")).ToList();
                if (items.Any())
                {
                    List<string> url = new List<string>();
                    foreach (IWebElement item in items)
                    {
                        string temp = item.GetAttribute("style").Substring(item.GetAttribute("style").IndexOf("("));
                        temp = temp.Substring(2);
                        temp = temp.Substring(0, temp.Length - 3);
                        url.Add(temp);
                    }
                    if (url.Any())
                    {
                        pictureList.Add(new PostPictures() { ID = PostId(element), PictureURLs = url.ToArray() });
                    }
                }
            }
            if (pictureList.Any())
            {
                return pictureList;
            }
            else { return null; }
        }
        private List<PostPictures> SortListPost(List<PostPictures> oldList, List<PostPictures> newList)
        {
            List<PostPictures> sort_list = new List<PostPictures>();
            int check = 0;
            if (newList == null)
            {
                return oldList;
            }

            if (oldList == null)
            {
                return newList;
            }

            foreach (PostPictures new_item in newList)
            {
                foreach (PostPictures item in oldList)
                {
                    if (new_item.ID.Equals(item.ID))
                    {
                        check = 1;
                        break;
                    }
                }
                if (check == 1)
                {
                    check = 0;
                }
                else
                {
                    sort_list.Add(new_item);
                }
            }
            sort_list.AddRange(oldList);
            return sort_list;
        }
        //Методы парсинга ссылок
        //////////////////////////////////////////////////////////////////////////////////////////
        private void PostSerialization(List<PostURLs> urlList, string fileWay)
        {
            if (urlList == null)
            {
                semUrl.Release();
                return;
            }
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(PostURLs[]));
            using (FileStream stream = new FileStream(fileWay, FileMode.OpenOrCreate))
            {
                jsonFormatter.WriteObject(stream, urlList.ToArray());
            }
            semUrl.Release();
        }
        private void PostURLDeserialization(string fileWay)
        {
            if (!File.Exists(fileWay))
            {
                tmpURL = null;
                semUrl.Release();
                return;
            }
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(PostURLs[]));
            using (FileStream stream = new FileStream(fileWay, FileMode.Open))
            {
                PostURLs[] p = (PostURLs[])jsonFormatter.ReadObject(stream);
                tmpURL = p.ToList<PostURLs>();
                semUrl.Release();
            }
        }
        private List<PostURLs> FillingListPostURL(List<IWebElement> webElements)
        {
            List<PostURLs> urlList = new List<PostURLs>();
            if (!webElements.Any())
            {
                return null;
            }

            foreach (IWebElement element in webElements)
            {
                List<string> url = new List<string>();
                List<IWebElement> items = element.FindElements(By.XPath(".//*[@class = 'wall_post_text']//a")).ToList();
                if (items.Any())
                {
                    foreach (IWebElement item in items)
                    {
                        url.Add(item.Text);
                    }
                    if (url != null)
                    {
                        urlList.Add(new PostURLs() { ID = PostId(element), TextURLs = url.ToArray() });
                    }
                }
            }
            if (urlList.Any())
            {
                return urlList;
            }
            else
            {
                return null;
            }
        }
        private List<PostURLs> SortListPost(List<PostURLs> oldList, List<PostURLs> newList)
        {
            List<PostURLs> sort_list = new List<PostURLs>();
            int check = 0;
            if (newList == null)
            {
                return oldList;
            }

            if (oldList == null)
            {
                return newList;
            }

            foreach (PostURLs new_item in newList)
            {
                foreach (PostURLs item in oldList)
                {
                    if (new_item.ID.Equals(item.ID))
                    {
                        check = 1;
                        break;
                    }
                }
                if (check == 1)
                {
                    check = 0;
                }
                else
                {
                    sort_list.Add(new_item);
                }
            }
            sort_list.AddRange(oldList);
            return sort_list;
        }
        //Методы парсинга текста
        //////////////////////////////////////////////////////////////////////////////////////////
        private void PostSerialization(List<PostText> textList, string fileWay)
        {
            if (textList == null)
            {
                semText.Release();
                return;
            }
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(PostText[]));
            using (FileStream stream = new FileStream(fileWay, FileMode.OpenOrCreate))
            {
                jsonFormatter.WriteObject(stream, textList.ToArray());
            }
            semText.Release();
        }
        private void PostTextDeserialization(string fileWay)
        {
            if (!File.Exists(fileWay))
            {
                tmpText = null;
                semText.Release();
                return;
            }
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(PostText[]));
            using (StreamReader stream = new StreamReader(fileWay, System.Text.Encoding.Default))
            {
                PostText[] p = (PostText[])jsonFormatter.ReadObject(stream.BaseStream);
                tmpText = p.ToList<PostText>();
            }
            semText.Release();
        }
        private List<PostText> FillingListPostText(List<IWebElement> webElements)
        {
            if (webElements == null)
            {
                return null;
            }

            List<IWebElement> tmp = webElements;
            List<PostText> textList = new List<PostText>();
            foreach (IWebElement element in tmp)
            {
                List<IWebElement> items = element.FindElements(By.ClassName("wall_post_text")).ToList();
                string text = !items.Any() ? "" : items.First().Text;
                if (!text.Equals(""))
                {
                    textList.Add(new PostText() { ID = PostId(element), Text = text });
                }
            }
            if (textList.Any())
            {
                return textList;
            }
            else
            {
                return null;
            }
        }
        private List<PostText> SortListPost(List<PostText> oldList, List<PostText> newList)
        {
            if (oldList == null)
            {
                return newList;
            }

            if (newList == null)
            {
                return oldList;
            }

            List<PostText> sort_list = new List<PostText>();
            bool check = false;
            foreach (PostText new_item in newList)
            {
                foreach (PostText item in oldList)
                {
                    if (new_item.ID.Equals(item.ID))
                    {
                        check = true;
                        break;
                    }
                }
                if (check)
                {
                    check = false;
                }
                else
                {
                    sort_list.Add(new_item);
                }
            }
            sort_list.AddRange(oldList);
            return sort_list;
        }
        //////////////////////////////////////////////////////////////////////////////////////////
        private string PostId(IWebElement webElement)
        {
            return webElement.FindElement(By.TagName("div")).GetAttribute("id");
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(programmFile))
            {
                using (FileStream fs = new FileStream(programmFile, FileMode.Create))
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
            if (File.Exists(serviceFile)) { File.Delete(serviceFile); }




            timer1.Elapsed += Timer1_Tick;
            timer1.Interval = 5000;
            timer1.Enabled = true;
            timer1.AutoReset = true;
            ChromeOptions options = new ChromeOptions();
            options.AddArguments(@"--user-data-dir=C:\Users\Maxim\AppData\Local\Google\Chrome\User Data");
            chromeDriver = new ChromeDriver(options);
            chromeDriver.Navigate().GoToUrl("https://vk.com/feed");
            Thread.Sleep(500);

            if ((bool)PlanChecker.IsChecked)
            {
                schenduler.CreatePlan();
                while (true)
                {

                    if (flag == false)
                    {
                        if (File.Exists(programmFile))
                        {
                            chromeDriver.Navigate().Refresh();
                            Thread.Sleep(500);
                            EventScheduler();
                        }
                    }
                    else
                    {
                        File.Delete(programmFile);
                        using (FileStream fs = new FileStream(serviceFile, FileMode.Create))
                        {
                            fs.Close();
                            fs.Dispose();
                        }
                        using (StreamWriter sw = new StreamWriter(@"C:\Users\Maxim\source\repos\MyOCProject\PostData\ServiceLog\ServiceLog.txt", true, System.Text.Encoding.Default))
                        {
                            sw.WriteLine("[" + DateTime.Now.ToString() + "] Парсер не дождался ответа сервиса и пропустил его итерацию.");
                        }
                        flag = false;
                        timer1.Start();
                    }
                }
            }
            else
            {
                while (true)
                {
                    chromeDriver.Navigate().Refresh();
                    Thread.Sleep(500);
                    StartThreads();
                }
            }
        }
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            int tmp = 0;
            if (Int32.TryParse(Tablo2.Text, out tmp))
            {
                schenduler.SetMaxSize(tmp);
                if (tmp == 0)
                {
                    Tablo2.Text = "Длина выводимого плана - 15";
                }
                else
                {
                    Tablo2.Text = "Длина выводимого плана - " + tmp.ToString();
                }
            }
            else
            {
                Tablo2.Text = "Некорректное значение";
            }
        }

        //Тест семафора
        ////////////////////////////////////////////////////////////////////////////////////////////////
        delegate void WriteMethod(int i);
        readonly Semaphore semNet = new Semaphore(1, 1);
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WriteMethod method;
            string testPath = @"C:\Users\Maxim\source\repos\MyOCProject\PostData\SemaphoreTest\NETSemaphore.txt";
            if (File.Exists(testPath))
            {
                File.Delete(testPath);
            }

            method = WriteWithNETSemaphore;

            Thread one;
            Thread two;
            for (int i = 0; i < 50; i++)
            {
                one = new Thread(() => method(i));
                two = new Thread(() => method(i * (-1)));
                one.Start();
                two.Start();
                while (true)
                {
                    if (!one.IsAlive && !two.IsAlive)
                    {
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        public void WriteWithNETSemaphore(int i)
        {
            string testPath = @"PostData\SemaphoreTest\NETSemaphore.txt";
            semNet.WaitOne();
            using (StreamWriter sw = new StreamWriter(testPath, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("Potok " + i.ToString() + " zapisal");
            }
            semNet.Release();
        }
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;
            radioMy.IsChecked = false;
            radioSys.IsChecked = false;
            radioButton.IsChecked = true;
        }
        private void Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;
            switch (radioButton.Name)
            {
                case "radioMy":
                    whichSem = true;
                    break;
                case "radioSys":
                    whichSem = false;
                    break;
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            Process[] proc = Process.GetProcessesByName("MyOCService");
            if (proc.Length == 0)
            {
                timer1.Stop();
                if (File.Exists(serviceFile)) { File.Delete(serviceFile); }
                if (!File.Exists(programmFile))
                {
                    using (FileStream fs = new FileStream(programmFile, FileMode.Create))
                    {
                        fs.Close();
                        fs.Dispose();
                    }
                }
                flag = false;
            }
        }

    }
}




