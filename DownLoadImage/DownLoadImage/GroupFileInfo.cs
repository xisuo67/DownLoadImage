using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DownLoadImage
{
    public class GroupFileInfo
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 文件信息
        /// </summary>
        public List<TrafficGroupParam> TrafficGroupParamList { get; set; }
    }
}
