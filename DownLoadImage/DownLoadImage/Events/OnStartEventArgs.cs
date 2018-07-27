using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DownLoadImage.Events
{
    /// <summary>
    /// 图片下载启动事件
    /// </summary>
    public class OnStartEventArgs
    {
        //public Uri Uri { get; set; }// 爬虫URL地址
        public string Url { get; set; } //图片url
        //public OnStartEventArgs(Uri uri)
        //{
        //    this.Uri = uri;
        //}
        public OnStartEventArgs(string url)
        {
            this.Url = url;
        }
    }
}
