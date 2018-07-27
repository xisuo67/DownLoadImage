using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DownLoadImage.Events;
using System.Diagnostics;

namespace DownLoadImage
{
    public class Handler
    {
        /// <summary>
        /// 单线程最大下载文件数
        /// </summary>
        private int Number100 = 100;

        /// <summary>
        /// 最大线程数
        /// </summary>
        private readonly int MaxThreadCount = 10;
        public event EventHandler<OnStartEventArgs> OnStart;//启动事件

        public event EventHandler<OnCompletedEventArgs> OnCompleted;//完成事件

        public event EventHandler<OnErrorEventArgs> OnError;//出错事件



        /// <summary>
        /// 下载图片
        /// </summary>
        /// <param name="param">文件信息对象</param>
        /// <returns></returns>
        private async void Download(TrafficGroupParam param)
        {
            await Task.Run(() =>
            {
                string filePath = param.Path;
                string savePath = $"{AppDomain.CurrentDomain.BaseDirectory}/{param.PlateNo}";
                savePath = string.IsNullOrEmpty(param.SavePath) == true ? savePath : $"{param.SavePath}/{param.PlateNo}";
                //取文件名后缀
                string fileName = Path.GetFileName(filePath);
                var fileSuffix = fileName.Split('.');
                var arr = filePath.Split(new string[] { " || " }, StringSplitOptions.RemoveEmptyEntries);
                string localPath = string.Empty;
                if (arr.Length == 2 && !string.IsNullOrEmpty(arr[1]))
                {
                    filePath = arr[0];
                    localPath = Path.Combine(savePath, arr[1]);
                }
                else
                {
                    string extName = fileSuffix.Contains("jpg") && fileSuffix.Contains("png") == true ? fileSuffix.FirstOrDefault(e => e.Contains("jpg") && e.Contains("png")) : "jpg";
                    localPath = Path.Combine(savePath, $"{param.PassingFileName}.{extName}");
                }

                if (Regex.IsMatch(filePath, @"^\\{2}", RegexOptions.IgnoreCase))
                {
                    System.IO.File.Copy(filePath, localPath, true);
                    return;
                }
                if (Regex.IsMatch(filePath, @"^[a-zA-Z]:\\", RegexOptions.IgnoreCase))
                {
                    System.IO.File.Copy(filePath, localPath, true);
                    return;
                }
                if (Regex.IsMatch(filePath, "^ftp://", RegexOptions.IgnoreCase))
                {
                    try
                    {
                        new FtpClient().Get(filePath, savePath, Path.GetFileName(filePath));
                    }
                    catch (Exception ex)
                    {
                        if (this.OnError!=null)
                        {
                            this.OnError(this, new OnErrorEventArgs(filePath, ex));
                        }
                    }
                    return;
                }
                if (!Regex.IsMatch(filePath, "^http://", RegexOptions.IgnoreCase) && !Regex.IsMatch(filePath, "^https://", RegexOptions.IgnoreCase))
                {
                    return;
                }
                ToggleAllowUnsafeHeaderParsing(true);
                try
                {
                    if (this.OnStart != null)
                    {
                        OnStart(this, new OnStartEventArgs(filePath));
                    }
                    var watch = new Stopwatch();
                    watch.Start();
                    using (WebResponse response = WebRequest.Create(filePath).GetResponse())
                    {
                        try
                        {
                            using (Stream stream = response.GetResponseStream())
                            {
                                byte[] buffer = new byte[0x400];
                                int count = 0;
                                using (FileStream stream2 = new FileStream(localPath, FileMode.Create, FileAccess.Write))
                                {
                                    while ((count = stream.Read(buffer, 0, 0x400)) > 0)
                                    {
                                        stream2.Write(buffer, 0, count);
                                        stream2.Flush();
                                    }
                                }
                            }
                        }
                        catch (WebException ex)
                        {
                            var res = (WebResponse)ex.Response;
                            if (res != null)
                            {
                                StreamReader sr = new StreamReader(res.GetResponseStream());
                                var strHtml = sr.ReadToEnd();
                                WriteLog($"请求地址：{filePath}" + strHtml, "errorInfo");
                            }
                            else
                            {
                                WriteLog($"请求地址：{filePath}" + ex.ToString(), "errorInfo");
                            }
                        }
                    }
                    watch.Stop();
                    var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;//获取当前任务线程ID
                    var milliseconds = watch.ElapsedMilliseconds;//获取请求执行时间
                    if (this.OnCompleted != null) this.OnCompleted(this, new OnCompletedEventArgs(filePath, threadId, milliseconds));
                }
                catch (WebException ex)
                {
                    if (this.OnError != null) this.OnError(this, new OnErrorEventArgs(filePath, ex));
                }

            });
        }
        /// <summary>
        /// 日志方法
        /// </summary>
        /// <param name="message">保存日志内容</param>
        /// <param name="logName">日志名称</param>
        private void WriteLog(string message, string logName)
        {
            string path = $"{AppDomain.CurrentDomain.BaseDirectory}/{logName}.txt";
            StreamWriter sw = new StreamWriter(path, true);
            sw.WriteLine($"{message}");
            sw.Close();//写入读取的数据
        }
        /// <summary>
        /// 下载方法（读取传入对象）
        /// </summary>
        /// <param name="paramList"></param>
        /// <param name="success"></param>
        /// <param name="fail"></param>
        public void Execute(List<TrafficGroupParam> paramList)
        {
            var fileInfo = paramList.GroupBy(e => e.PlateNo).Select(e => new GroupFileInfo
            {
                FileName = e.Key,
                TrafficGroupParamList = e.ToList()
            }).ToList();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (paramList.Count < 1)
            {
                throw new Exception("没有要导出的文件！");
            }
            foreach (var item in fileInfo)
            {
                string tempPath = string.IsNullOrEmpty(item.TrafficGroupParamList[0].SavePath) == true ? baseDirectory : item.TrafficGroupParamList[0].SavePath;
                tempPath = Path.Combine(tempPath, item.FileName);
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }
                //如果小于或等于容器大小.则无需开启线程编译
                DownLoadParams downloadParam = null;
                var pathsCount = item.TrafficGroupParamList.Count;//图片数量，用于判断是否启用多线程
                if (pathsCount <= Number100)
                {
                    downloadParam = new DownLoadParams()
                    {
                        TrafficGroupParam = item.TrafficGroupParamList.ToArray()
                    };
                    DownLoadDork(downloadParam);
                }
                else
                {
                    // 计算编译线程数量
                    int threadCount = pathsCount % Number100 == 0 ? pathsCount / Number100 : pathsCount / Number100 + 1;
                    if (threadCount > MaxThreadCount)
                    {
                        threadCount = MaxThreadCount;
                    }
                    int threadPqgeSize = (pathsCount / threadCount) + 1;
                    int typeSum = 0;
                    // 为每个线程准备调用参数
                    List<DownLoadParams> parameters = new List<DownLoadParams>(threadCount);
                    TrafficGroupParam[] trafficGroupParam = null;
                    int index, endSize = 0; ;
                    for (int i = 0; i < threadCount; i++)
                    {
                        downloadParam = new DownLoadParams();
                        endSize = threadPqgeSize * (i + 1);
                        if (endSize > pathsCount)
                        {
                            trafficGroupParam = new TrafficGroupParam[threadPqgeSize + pathsCount - endSize];
                            endSize = pathsCount;
                        }
                        else
                        {
                            trafficGroupParam = new TrafficGroupParam[threadPqgeSize];
                        }
                        index = 0;
                        for (int j = typeSum; j < endSize; j++)
                        {
                            trafficGroupParam[index++] = item.TrafficGroupParamList[j];
                        }
                        downloadParam.TrafficGroupParam = trafficGroupParam;
                        parameters.Add(downloadParam);
                    }
                    // 创建编译线程
                    List<Thread> threads = new List<Thread>(threadCount);
                    for (int i = 1; i < threadCount; i++)
                    {
                        Thread thread = new Thread(DownLoadDork);
                        thread.IsBackground = true;
                        thread.Name = "DownloadThread #" + i.ToString();
                        threads.Add(thread);
                        thread.Start(parameters[i]);
                    }

                    // 重用当前线程：为当前线程指派下载任务。
                    DownLoadDork(parameters[0]);

                    // 等待所有的下载线程执行线束。
                    foreach (Thread thread in threads)
                    {
                        thread.Join();
                    }
                }
            }


            //success = tempSuccess;
            //fail = tempFail;
        }

        private void DownLoadDork(object obj)
        {
            DownLoadParams dparm = obj as DownLoadParams;
            foreach (var item in dparm.TrafficGroupParam)
            {
                try
                {
                    Download(item);
                }
                catch (Exception ex)
                {
                    string path = $"{AppDomain.CurrentDomain.BaseDirectory}/errorInfo.txt";
                    StreamWriter sw = new StreamWriter(path, true);
                    sw.WriteLine(ex.ToString());
                    sw.Close();//写入读取的数据
                }
            }
        }

        // Enable/disable useUnsafeHeaderParsing.  
        // See http://o2platform.wordpress.com/2010/10/20/dealing-with-the-server-committed-a-protocol-violation-sectionresponsestatusline/ 
        private bool ToggleAllowUnsafeHeaderParsing(bool enable)
        {
            //Get the assembly that contains the internal class  
            Assembly assembly = Assembly.GetAssembly(typeof(SettingsSection));
            if (assembly != null)
            {
                //Use the assembly in order to get the internal type for the internal class  
                Type settingsSectionType = assembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                if (settingsSectionType != null)
                {
                    //Use the internal static property to get an instance of the internal settings class.  
                    //If the static instance isn't created already invoking the property will create it for us.  
                    object anInstance = settingsSectionType.InvokeMember("Section",
                    BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] { });
                    if (anInstance != null)
                    {
                        //Locate the private bool field that tells the framework if unsafe header parsing is allowed  
                        FieldInfo aUseUnsafeHeaderParsing = settingsSectionType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (aUseUnsafeHeaderParsing != null)
                        {
                            aUseUnsafeHeaderParsing.SetValue(anInstance, enable);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 下载参数
    /// </summary>
    public class DownloadParam
    {
        /// <summary>
        /// 下载地址
        /// </summary>
        public string[] Paths { get; set; }

        /// <summary>
        /// 本地保存路径
        /// </summary>
        public string SavePath { get; set; }
    }
    /// <summary>
    /// 下载参数
    /// </summary>
    public class DownLoadParams
    {
        /// <summary>
        /// 下载对象
        /// </summary>
        public TrafficGroupParam[] TrafficGroupParam { get; set; }
    }
}
