using Baidu.Aip.Speech;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FaceCheckIn
{
    public class SpeakHello
    {
        [DllImport("winmm.dll", SetLastError = true)]
        static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        // 设置APPID/AK/SK
        private const string APP_ID = "22836668";
        private const string API_KEY = "jMrmRPSWcz4jLfQKMwmQz3Rg";
        private const string SECRET_KEY = "GmgqvXo1IndRHXTSDh15R0pvAE8GohBO";

        private static Tts speechClient = new Tts(API_KEY, SECRET_KEY) { Timeout = 6000 };

        /// <summary>
        /// 语音合成
        /// </summary>
        /// <param name="content"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static bool speech(string content, Dictionary<string, object> option)
        {
            try
            {
                if (option == null)
                {
                    // 可选参数
                    option = new Dictionary<string, object>()
                    {
                        {"spd", 5}, // 语速
                        {"vol", 7}, // 音量
                        {"per", 4}  // 发音人，4：情感度丫丫童声
                    };
                }

                var result = speechClient.Synthesis(content, option);

                if (result.ErrorCode == 0)
                {
                    File.WriteAllBytes("temp.mp3", result.Data);
                    Play("temp.mp3");
                }
                return result.Success;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        private static void Play(string path)
        {
            // 播放音频文件
            mciSendString("open temp.mp3 alias temp_alias", null, 0, IntPtr.Zero);
            mciSendString("play temp_alias", null, 0, IntPtr.Zero);

            // 等待播放结束
            StringBuilder strReturn = new StringBuilder(64);
            do
            {
                mciSendString("status temp_alias mode", strReturn, 64, IntPtr.Zero);
            } while (!strReturn.ToString().Contains("stopped"));

            // 关闭音频文件
            mciSendString("close temp_alias", null, 0, IntPtr.Zero);
        }


    }
}

