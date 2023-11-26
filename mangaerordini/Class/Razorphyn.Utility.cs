using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Razorphyn.SupportClasses;
using static Razorphyn.ProgramParameters;
using System.Data;

namespace Razorphyn
{
    internal static class Utility
    {
        //COMBOBOX
        public static int FindIndexFromValue(ComboBox nome_ctr, long value)
        {
            int i = 0;
            bool indexfound = false;
            foreach (ComboBoxList item in nome_ctr.Items)
            {
                if ((long)item.Value == value)
                {
                    indexfound = true;
                    break;
                }
                i++;
            }
            if (indexfound == true)
                return i;
            else
                return 0;
        }

        public static void RemoveRow(TableLayoutPanel panel, int rowIndex)
        {

            if (rowIndex >= panel.RowCount)
            {
                return;
            }

            DrawingControl.SuspendDrawing(panel);
            // delete all controls of row that we want to delete
            for (int i = 0; i < panel.ColumnCount; i++)
            {
                var control = panel.GetControlFromPosition(i, rowIndex);
                panel.Controls.Remove(control);
            }

            // move up row controls that comes after row we want to remove
            for (int i = rowIndex + 1; i < panel.RowCount; i++)
            {
                for (int j = 0; j < panel.ColumnCount; j++)
                {
                    var control = panel.GetControlFromPosition(j, i);
                    if (control != null)
                    {
                        panel.SetRow(control, i - 1);
                    }
                }
            }

            var removeStyle = panel.RowCount - 1;

            if (panel.RowStyles.Count > removeStyle)
                panel.RowStyles.RemoveAt(removeStyle);

            panel.RowCount--;

            DrawingControl.ResumeDrawing(panel);
        }

        internal static void DataSourceToComboBox(ComboBox nome_ctr, List<ComboBoxList> dataSource)
        {
            nome_ctr.DataSource = null;
            nome_ctr.BindingContext = new BindingContext();

            nome_ctr.DisplayMember = "Name";
            nome_ctr.ValueMember = "Value";

            nome_ctr.DataSource = dataSource;
            nome_ctr.Refresh();
            nome_ctr.DropDownStyle = ComboBoxStyle.DropDownList;

            nome_ctr.DisplayMember = "Name";
            nome_ctr.ValueMember = "Value";

            nome_ctr.Invalidate();
        }

        internal static IEnumerable<Control> GetAllNestedControls(Control root)
        {
            var stack = new Stack<Control>();
            stack.Push(root);

            do
            {
                var control = stack.Pop();

                foreach (Control child in control.Controls)
                {
                    yield return child;
                    stack.Push(child);
                }
            }
            while (stack.Count > 0);
        }

        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            DateTime firstThursday = jan1.AddDays(daysOffset);
            int firstWeek = calendarCulture.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }
            var result = firstThursday.AddDays(weekNum * 7);

            return result.AddDays(-3);
        }

        internal static void ExitProgram()
        {
            if (System.Windows.Forms.Application.MessageLoop)
            {
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                System.Environment.Exit(1);
            }
        }

        internal static void OpenPDF(string filePath)
        {
            System.Diagnostics.Process.Start(filePath);
        }

        internal static void DataSourceToDataView(DataGridView data_grid, DataTable dataSource, Dictionary<string, string> columnNames) {
            DrawingControl.SuspendDrawing(data_grid);

            data_grid.DataSource = null;
            data_grid.Rows.Clear();
            if (data_grid.InvokeRequired)
                data_grid.Invoke(new MethodInvoker(() => data_grid.DataSource = dataSource));
            else
                data_grid.DataSource = dataSource;

            int colCount = data_grid.ColumnCount;
            for (int i = 0; i < colCount; i++)
            {
                if (columnNames.ContainsKey(data_grid.Columns[i].HeaderText))
                    data_grid.Columns[i].HeaderText = columnNames[data_grid.Columns[i].HeaderText];

                data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                int colw = data_grid.Columns[i].Width;
                data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                data_grid.Columns[i].Width = colw;
            }

            DrawingControl.ResumeDrawing(data_grid);
        }

        internal static void FixPanel(TableLayoutPanel panel)
        {
            typeof(TableLayoutPanel).InvokeMember(
               "DoubleBuffered",
               BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
               null,
               panel,
               new object[] { true }
            );
            panel.AutoScroll = false;
            panel.AutoScroll = true;

            panel.AutoSize = false;
            panel.AutoScroll = true;

        }
    }

    internal static class FixBuffer
    {
        internal static void TableLayoutPanel(TableLayoutPanel obj)
        {
            typeof(TableLayoutPanel).InvokeMember(
                   "DoubleBuffered",
                   BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                   null,
                   obj,
                   new object[] { true }
                );
            obj.AutoScroll = true;
        }


    }

    internal static class ImportPDFSupport
    {
        internal static void DeleteRows(TableLayoutPanel obj, List<int> Rows)
        {
            int c = Rows.Count;

            for (int i = 0; i < c; i++)
            {
                Utility.RemoveRow(obj, Rows[i] - i);
            }
            ResizeRow(obj);
        }

        internal static void ResizeRow(TableLayoutPanel obj, SizeType option = SizeType.Absolute, int height = 50)
        {
            DrawingControl.SuspendDrawing(obj);

            TableLayoutRowStyleCollection rowStyles = obj.RowStyles;
            foreach (RowStyle style in rowStyles)
            {
                style.SizeType = option;
                style.Height = height;
            }
            DrawingControl.ResumeDrawing(obj);
        }
    
        internal static void DeleteControls(TableLayoutPanel ItemTable)
        {
            while (ItemTable.Controls.Count > 0)
            {
                var control = ItemTable.Controls[0];
                ItemTable.Controls.RemoveAt(0);
                control.Dispose();
            }

            ItemTable.Controls.Clear();

            return;
        }
    }

    internal static class DrawingControl
    {
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);

        private const int WM_SETREDRAW = 11;

        public static void SuspendDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, false, 0);
        }

        public static void ResumeDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, true, 0);
            parent.Refresh();
        }
    }


    internal static class TableLayoutPanel_Tools
    {
        internal static void ResizeRowFixed(TableLayoutPanel Table, int size)
        {
            TableLayoutRowStyleCollection styles = Table.RowStyles;

            foreach (RowStyle style in styles)
            {
                style.SizeType = SizeType.Absolute;
                style.Height = size;
            }
        }
    }
}
