﻿using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Runtime.Remoting.Contexts;

namespace R3ResultTool
{
    internal class Func
    {
        private static HttpClient client = new HttpClient();
        private static Logger log = Logger.GetInstance();
        readonly string apiurl = ConfigurationManager.AppSettings["apiurl"];

        public string GetLine(String filepath)
        {
            int BUFFER_SIZE = 32; // バッファーサイズ(あえて小さく設定)
            int lineCountToWrite = 30; // 探索行数
            var buffer = new byte[BUFFER_SIZE];
            var foundCount = 0;

            using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // 検索ブロック位置の繰り返し
                for (var i = 0; ; i++)
                {
                    if (fs.Length <= i * BUFFER_SIZE)
                    {
                        // ファイルの先頭まで達した場合
                        Console.WriteLine("NOT FOUND");
                        string log = "";

                        using (var sr = new StreamReader(fs, Encoding.UTF8))
                        {
                            log = sr.ReadToEnd();
                        }
                        return log;
                    }

                    // ブロック開始位置に移動
                    var offset = Math.Min((int)fs.Length, (i + 1) * BUFFER_SIZE);
                    fs.Seek(-offset, SeekOrigin.End);

                    // ブロックの読み込み
                    var readLength = offset - BUFFER_SIZE * i;
                    for (var j = 0; j < readLength; j += fs.Read(buffer, j, readLength - j)) ;

                    // ブロック内の改行コードの検索
                    for (var k = readLength - 1; k >= 0; k--)
                    {
                        if (buffer[k] == 0x0A)
                        {
                            var sr = new System.IO.StreamReader(fs, Encoding.UTF8);
                            fs.Seek(k + 3, SeekOrigin.Current);
                            string line = sr.ReadLine();
                            if (line != null && line.Contains("XDR3_DBG_RESULT"))
                            {
                                // XrossDiscのリザルトログだった場合
                                return line;
                            }
                            foundCount++;
                            if (foundCount == lineCountToWrite)
                            {
                                // 所定の行数が見つかった場合
                                return "";
                            }
                        }
                    }
                }
            }
        }

        public JObject ConvertLog(string log)
        {
            JObject json = JObject.Parse(log);
            return json;
        }

        public bool SendData(JObject data)
        {
            JObject value = new JObject();
            value["api_key"] = ConfigurationManager.AppSettings["api-key"];
            value["name_a1"] = data["XDR3_DBG_RESULT"]["player_a1"]["name"];
            value["name_a2"] = data["XDR3_DBG_RESULT"]["player_a2"]["name"];
            value["name_b1"] = data["XDR3_DBG_RESULT"]["player_b1"]["name"];
            value["name_b2"] = data["XDR3_DBG_RESULT"]["player_b2"]["name"];
            value["score_a"] = data["XDR3_DBG_RESULT"]["score"]["a"];
            value["score_b"] = data["XDR3_DBG_RESULT"]["score"]["b"];
            value["guid"] = data["XDR3_DBG_RESULT"]["guid"];
            value["hopping_allowed"] = data["XDR3_DBG_RESULT"]["settings"]["hopping_allowed"];
            value["game_double"] = data["XDR3_DBG_RESULT"]["settings"]["double"];
            value["game_ex_speed"] = data["XDR3_DBG_RESULT"]["settings"]["ex_speed"];
            value["game_boundaries"] = data["XDR3_DBG_RESULT"]["settings"]["boundaries"];
            value["score_max"] = data["XDR3_DBG_RESULT"]["score"]["max"];
            try
            {
                var content = new StringContent(value.ToString(), Encoding.UTF8, "application/json");
                var res = client.PostAsync(apiurl, content);
                Console.WriteLine("送信しました");
                JObject res_json = JObject.Parse(res.Result.Content.ReadAsStringAsync().Result);
                if (res_json["status"].Equals("success"))
                {
                    log.Info("================================================================================");
                    log.Info(res_json["status"].ToString());
                    log.Info(res_json["result"].ToString());
                    log.Info(value.ToString());    
                }
                else
                {
                    log.Info("================================================================================");
                    Console.WriteLine("保存に失敗しました");
                    log.Warn(res_json["status"].ToString());
                    log.Warn(res_json["result"].ToString());
                    log.Warn(value.ToString());
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("送信に失敗しました");
                return false;
            }
            return true;
        }
    }
}