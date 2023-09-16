using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Razorphyn.SupportClasses;
using static Razorphyn.ProgramParameters;

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

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            DateTime firstThursday = jan1.AddDays(daysOffset);
            int firstWeek = calendarCulture.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            var result = firstThursday.AddDays(weekNum * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
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
}
