using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DownLoadImage
{
    /// <summary>
    /// 通行记录分组参数
    /// </summary>
    public class TrafficGroupParam
    {
        /// <summary>
        /// 图片url路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 号牌号码
        /// </summary>
        public string PlateNo { get; set; }

        /// <summary>
        /// 路口名称
        /// </summary>
        public string SpottingName { get; set; }

        /// <summary>
        /// 方向名称
        /// </summary>
        public string DirectionName { get; set; }

        /// <summary>
        /// 过车时间
        /// </summary>
        public DateTime? PassingTime { get; set; }
        /// <summary>
        /// 图片名称
        /// </summary>
        public string PassingFileName { get; set; }

        /// <summary>
        /// 存储路径
        /// </summary>
        public string SavePath { get; set; }
    }
}
