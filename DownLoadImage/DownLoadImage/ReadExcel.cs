using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownLoadImage
{
    /// <summary>
    /// 读取Excel文件
    /// </summary>
    public class ReadExcel
    {
        /// <summary>
        /// 从excel中所有sheet中读取数据
        /// </summary>
        /// <param name="ins">输入流</param>
        /// <param name="headRowIndex">标题行索引 默认为第6行</param>
        /// <param name="listSheet">所有有数据的Sheet</param>
        /// <returns>DataTable</returns>
        public static DataSet GetDataSetFromExcel(Stream ins, List<string> sheetNames, int headRowIndex, out List<ISheet> listSheet)
        {
            IWorkbook workbook = InitWorkBook(ins);
            DataSet ds = new DataSet();
            List<ISheet> sheets = new List<ISheet>();
            if (workbook.NumberOfSheets > 0)
            {
                //读取标题行
                IRow row = null;
                ICell cell = null;
                ISheet fSheet = null;
                DataTable dt = null;
                foreach (string sheetName in sheetNames)
                {
                    fSheet = workbook.GetSheet(sheetName);
                    if (fSheet == null || fSheet.LastRowNum < headRowIndex)
                    {
                        continue;
                    }
                    dt = new DataTable();
                    dt.TableName = sheetName;
                    row = fSheet.GetRow(headRowIndex);
                    object objColumnName = null;
                    for (int i = 0, length = row.LastCellNum; i < length; i++)
                    {
                        cell = row.GetCell(i);
                        if (cell == null)
                        {
                            continue;
                        }
                        objColumnName = GetCellVale(cell);
                        if (objColumnName != null)
                        {
                            dt.Columns.Add(objColumnName.ToString().Trim());
                        }
                        else
                        {
                            dt.Columns.Add("");
                        }
                    }

                    //读取数据行
                    object[] entityValues = null;
                    int columnCount = dt.Columns.Count;

                    for (int i = headRowIndex + 1, length = fSheet.LastRowNum + 1; i < length; i++)
                    {
                        row = fSheet.GetRow(i);
                        if (row == null)
                        {
                            continue;
                        }
                        entityValues = new object[columnCount];
                        //用于判断是否为空行
                        bool isHasData = false;
                        int dataColumnLength = row.LastCellNum < columnCount ? row.LastCellNum : columnCount;
                        for (int j = 0; j < dataColumnLength; j++)
                        {
                            cell = row.GetCell(j);
                            if (cell == null)
                            {
                                continue;
                            }
                            entityValues[j] = GetCellVale(cell);
                            if (!isHasData && j < columnCount && entityValues[j] != null)
                            {
                                isHasData = true;
                            }
                        }
                        if (isHasData)
                        {
                            dt.Rows.Add(entityValues);
                        }
                    }
                    ds.Tables.Add(dt);
                    sheets.Add(fSheet);
                }
            }
            listSheet = sheets;
            return ds;
        }
        public static DataTable GetDataFromExcel(Stream ins, out ISheet datasheet)
        {
            int StartRowIndex = 0;
            return GetDataFromExcel(ins, out datasheet, StartRowIndex);
        }

        public static DataTable GetDataFromExcel(Stream ins, out ISheet fSheet, int headRowIndex = 5)
        {
            IWorkbook workbook = InitWorkBook(ins);
            fSheet = null;
            DataTable dt = new DataTable();
            if (workbook.NumberOfSheets > 0)
            {
                fSheet = workbook.GetSheetAt(0);
                if (fSheet.LastRowNum < headRowIndex)
                {
                    throw new ArgumentException("Excel模版错误,标题行索引大于总行数");
                }

                //读取标题行
                IRow row = null;
                ICell cell = null;

                row = fSheet.GetRow(headRowIndex);
                object objColumnName = null;
                for (int i = 0, length = row.LastCellNum; i < length; i++)
                {
                    cell = row.GetCell(i);
                    if (cell == null)
                    {
                        continue;
                    }
                    objColumnName = GetCellVale(cell);
                    if (objColumnName != null)
                    {
                        try
                        {
                            dt.Columns.Add(objColumnName.ToString().Trim());
                        }
                        catch (Exception e)
                        {
                            throw new Exception("上传文件格式与下载模板格式不符");
                        }

                    }
                    else
                    {
                        dt.Columns.Add("");
                    }
                }

                //读取数据行
                object[] entityValues = null;
                int columnCount = dt.Columns.Count;

                for (int i = headRowIndex + 1, length = fSheet.LastRowNum; i < length; i++)
                {
                    row = fSheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }
                    entityValues = new object[columnCount];
                    //用于判断是否为空行
                    bool isHasData = false;
                    int dataColumnLength = row.LastCellNum < columnCount ? row.LastCellNum : columnCount;
                    for (int j = 0; j < dataColumnLength; j++)
                    {
                        cell = row.GetCell(j);
                        if (cell == null)
                        {
                            continue;
                        }
                        entityValues[j] = GetCellVale(cell);
                        if (!isHasData && j < columnCount && entityValues[j] != null)
                        {
                            isHasData = true;
                        }
                    }
                    if (isHasData)
                    {
                        dt.Rows.Add(entityValues);
                    }
                }
            }
            return dt;
        }
        /// <summary>
        /// 获取单元格值
        /// </summary>
        /// <param name="cell">单元格</param>
        /// <returns>单元格值</returns>
        private static object GetCellVale(ICell cell)
        {
            object obj = null;
            switch (cell.CellType)
            {
                case CellType.NUMERIC:
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        obj = cell.DateCellValue;
                    }
                    else
                    {
                        obj = cell.NumericCellValue;
                    }
                    break;
                case CellType.STRING:
                    if (string.IsNullOrEmpty(cell.StringCellValue))
                        obj = string.Empty;
                    else
                        obj = cell.StringCellValue.Trim();
                    break;
                case CellType.BOOLEAN:
                    obj = cell.BooleanCellValue;
                    break;
                case CellType.FORMULA:
                    obj = cell.CellFormula;
                    break;

            }
            return obj;
        }
        public static IWorkbook InitWorkBook(Stream ins)
        {
            return new HSSFWorkbook(ins);
        }
    }
}
