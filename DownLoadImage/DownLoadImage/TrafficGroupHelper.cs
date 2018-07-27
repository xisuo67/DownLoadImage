using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownLoadImage
{
    public class TrafficGroupHelper
    {
        /// <summary>
        /// 获取图片本地存储路径
        /// </summary>
        /// <param name="path">图片http路径</param>
        /// <param name="DirectionName">方向名称</param>
        /// <param name="SpottingName">路口名称</param>
        ///<param name="dictUrls">输出参数 存储当前数据分组后每个组的图片信息</param>
        /// <returns>下载完成后的本地路径</returns>
        public static string GetPicLocalPath(TrafficGroupParam param, Dictionary<string, List<string>> dictUrls)
        {
            string localPath = string.Empty;
            if (dictUrls == null)
            {
                dictUrls = new Dictionary<string, List<string>>();
            }
            if (!string.IsNullOrEmpty(param.Path))
            {
                string fileName = Path.GetFileName(param.Path);
                var ext = Path.GetExtension(param.Path).ToLower();
                //判断是否为图片文件路径
                bool picPath = true;
                if (!ext.Contains(".jpg") && !ext.Contains(".png") && !ext.Contains(".bmp") && !ext.Contains(".gif"))
                {
                    fileName = Guid.NewGuid().ToString("N") + ".jpg";
                    picPath = false;
                }
                string groupKey = string.Empty;
                List<string> realUrls = null;
                string PicGroupType = "5";//TrafficExportConfig.PicGroupType;
                switch (PicGroupType)
                {
                    case "0":
                        localPath = "导出图片\\" + fileName;
                        groupKey = "导出图片.dl";
                        break;
                    case "1":
                        //按路口分组
                        if (string.IsNullOrEmpty(param.SpottingName))
                        {
                            localPath = "导出图片\\" + fileName;
                            groupKey = "导出图片.dl";
                        }
                        else
                        {
                            localPath = param.SpottingName + "\\" + fileName;
                            groupKey = param.SpottingName + ".dl";
                        }
                        break;
                    case "2":
                        //按路口方向分组
                        if (string.IsNullOrEmpty(param.SpottingName) || string.IsNullOrEmpty(param.DirectionName))
                        {
                            localPath = "导出图片\\" + fileName;
                            groupKey = "导出图片.dl";
                        }
                        else
                        {
                            localPath = param.SpottingName + "\\" + param.DirectionName + "\\" + fileName;
                            groupKey = param.SpottingName + "@" + param.DirectionName + ".dl";
                        }
                        break;
                    case "3":
                        //按路口/日期分组
                        if (string.IsNullOrEmpty(param.SpottingName) || !param.PassingTime.HasValue)
                        {
                            localPath = "导出图片\\" + fileName;
                            groupKey = "导出图片.dl";
                        }
                        else
                        {
                            localPath = param.SpottingName + "\\" + param.PassingTime.Value.ToString("yyyy-MM-dd") + "\\" + fileName;
                            groupKey = param.SpottingName + "@" + param.PassingTime.Value.ToString("yyyy-MM-dd") + ".dl";
                        }
                        break;
                    case "4":
                        //按号牌/日期分组
                        if (string.IsNullOrEmpty(param.PlateNo) || !param.PassingTime.HasValue)
                        {
                            localPath = "导出图片\\" + fileName;
                            groupKey = "导出图片.dl";
                        }
                        else
                        {
                            localPath = param.PlateNo + "\\" + param.PassingTime.Value.ToString("yyyy-MM-dd") + "\\" + fileName;
                            groupKey = param.PlateNo + "@" + param.PassingTime.Value.ToString("yyyy-MM-dd") + ".dl";
                        }
                        break;
                    case "5":
                        //按号牌
                        if (string.IsNullOrEmpty(param.PlateNo))
                        {
                            localPath = "导出图片\\" + fileName;
                            groupKey = "导出图片.dl";
                        }
                        else
                        {
                            //var file= fileName.Split('.');
                            //string picName=$"{param.PassingTime}-{param.PlateNo}{fileName.Substring(file[0].Length)}"  ;
                            localPath = param.PlateNo + "\\" + fileName;
                            groupKey = param.PlateNo + ".dl";
                        }
                        break;
                    default:
                        localPath = "导出图片\\" + fileName;
                        groupKey = "导出图片.dl";
                        break;
                }
                if (!dictUrls.TryGetValue(groupKey, out realUrls))
                {
                    realUrls = new List<string>();
                    dictUrls[groupKey] = realUrls;
                }
                if (picPath)
                {
                    realUrls.Add(param.Path);
                }
                else
                {
                    realUrls.Add(param.Path + " || " + fileName);
                }
            }
            return localPath;
        }

        /// <summary>
        /// 生成下载文件清单
        /// </summary>
        /// <param name="s">压缩文件流</param>
        /// <param name="dictUrls">分组数据</param>
        public static void CreateGroupDir(Dictionary<string, List<string>> dictUrls)
        {
            if (dictUrls != null)
            {
                byte[] buffer = null;
                List<string> realUrls = null;
                foreach (string key in dictUrls.Keys)
                {
                    //entry = new ZipEntry(key);
                    //entry.DateTime = DateTime.Now;
                    //s.PutNextEntry(entry);
                    string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, key);
                    FileStream s = new FileStream(fileName, FileMode.Create);
                    if (dictUrls.TryGetValue(key, out realUrls))
                    {
                        //生成资源文件清单
                        foreach (string url in realUrls)
                        {
                            buffer = Encoding.UTF8.GetBytes(url + "\n");
                            s.Write(buffer, 0, buffer.Length);
                        }
                        s.Close();
                    }
                }
            }
        }
    }



}
