using DownLoadImage.Events;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownLoadImage
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            InitializeComponent();
        }
        private delegate void CSHandler(string arg0);
        private void btn_ChooseFile_Click(object sender, EventArgs e)
        {
            string filePath = string.Empty;
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择excel所在文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                else
                {
                    filePath = dialog.SelectedPath;
                    txt_FilePath.Text = filePath;
                }
            }
            DirectoryInfo theFolder = new DirectoryInfo(filePath);
            var file = theFolder.GetFiles("*.xls");
            cmb_ChooseExcel.DataSource = file;
        }
        /// <summary>
        /// 创建下载文件信息
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private DataSet CreateDownloadInfo(string fileName)
        {
            DataSet ds = null;
            int StartRowIndex = 0;

            //string fileName = Path.Combine(filePath, file[0].Name);
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader stream = new StreamReader(fs, System.Text.Encoding.Default);
            var ins = stream.BaseStream;
            //1.读取数据
            List<string> sheetNames = new List<string>() { "车辆轨迹明细数据" };
            List<ISheet> listSheet = null;
            //1.获取所有表格数据
            try
            {
                ds = ReadExcel.GetDataSetFromExcel(ins, sheetNames, StartRowIndex, out listSheet);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,"选定的Excel文件不符合下载要求，请重新选择","提示");
               
            }
            return ds;
        }
        /// <summary>
        /// 选择图片存放位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ChooseImage_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择下载图片存放路径";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                else
                {
                    txt_ImagePath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btn_Start_Click(object sender, EventArgs e)
        {
            var file = cmb_ChooseExcel.Text;
            string path = txt_FilePath.Text;
            if (string.IsNullOrEmpty(file))
            {
                MessageBox.Show(this, "请选择excel文件", "提示");
                return;
            }
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show(this,"请选择文件所在路径","提示");
                return;
            }
            string filePath = Path.Combine(path, file);
            List<TrafficGroupParam> paramList = new List<TrafficGroupParam>();
            try
            {
                DataSet ds = CreateDownloadInfo(filePath);
                if (ds != null)
                {
                    richTextBox1.Text += "开始读取文件内容...\r\n";
                    var dsInfo = ds.Tables["车辆轨迹明细数据"];
                    Dictionary<string, List<string>> dictUrls = new Dictionary<string, List<string>>();
                    TrafficGroupParam param = null;
                    string picPath = string.Empty, strSpottingNamne = string.Empty, strDirectionName = string.Empty, groupKey = string.Empty,
    plateNo = string.Empty, platecolor = string.Empty;
                    DateTime? passTime = null;
                    foreach (DataRow dr in dsInfo.Rows)
                    {
                        if (dr["链接"] != null && dr["链接"] != DBNull.Value)
                        {
                            picPath = dr["链接"].ToString();
                            if (dr["经过路口"] != null)
                            {
                                strSpottingNamne = dr["经过路口"].ToString();
                            }
                            if (dr["经过时间"] != null)
                            {
                                passTime = Convert.ToDateTime(dr["经过时间"]);
                            }
                            if (dr["号牌号码"] != null)
                            {
                                plateNo = dr["号牌号码"].ToString();
                            }
                            if (dr["号牌颜色"] != null)
                            {
                                platecolor = dr["号牌颜色"].ToString();
                            }
                            param = new TrafficGroupParam
                            {
                                Path = picPath,
                                SpottingName = strSpottingNamne,
                                PlateNo = plateNo + " " + platecolor,
                                DirectionName = strDirectionName,
                                PassingTime = passTime,
                                SavePath = txt_ImagePath.Text,
                                PassingFileName = $"{passTime.Value.ToString("yyyy-MM-dd HH-mm-ss")} {plateNo}"
                            };
                            paramList.Add(param);
                        }
                    }

                    
                    Handler handle = new Handler();
                    handle.OnStart += (s, ev) =>
                    {
                        this.Invoke(new MethodInvoker(()=> {
                            richTextBox1.Text += "开始下载图片，图片地址：" + ev.Url+ "\r\n";
                        }), s);
                    };
                    handle.OnError += (s,ev)=> {
                        this.Invoke(new MethodInvoker(()=> {
                            richTextBox1.Text += $"{ev.Exception.ToString()}【下载出错】，图片：{ev.Uri}未能下载成功，\r\n";
                        }),s);
                        
                    };
                    handle.OnCompleted += (s, ev) =>
                      {
                          this.Invoke(new MethodInvoker(() =>
                          {
                              richTextBox1.Text += $"{ev.Uri}【下载完成】\r\n";
                          }),s);
                          
                      };
                    handle.Execute(paramList);
                    paramList.Clear();
                    MessageBox.Show("下载完成");
                }
                else
                {
                    return;
                }
            }
            catch
            {
               
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
