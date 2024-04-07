using Newtonsoft.Json.Linq;
using R3ResultTool;
using System;
using System.Threading.Tasks;

namespace R3ResultTool
{
    internal class Timer
    {

        string logpath = "";
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        Func func = new Func();
        string cache;

        public void TimerStart(string log)
        {
            logpath = log;


            timer.Elapsed += (sender, e) =>
            {
                string line = func.GetLine(logpath);
                if (line == "") return;
                if (cache == null || !line.Equals(cache))
                {
                    cache = line;
                }
                else
                {
                    return;
                }
                JObject data = func.ConvertLog(line);
                try
                {
                    _ = func.SendData(data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            };

            timer.Start();

            Console.ReadLine();
        }

        public void TimerStop()
        {
            timer.Stop();
            Console.ReadKey();
        }
    }
}
