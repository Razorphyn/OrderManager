using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Razorphyn
{
    internal static class ProgramParameters
    {
        static readonly internal string exeFolderPath = Path.GetDirectoryName(Application.ExecutablePath) + @"\";
        static readonly internal string db_file_path = @"db\";
        static readonly internal string update_file_path = @"update\";
        static readonly internal string db_file_name = @"ManagerOrdini.db";
        static readonly internal string settingFile = exeFolderPath + @"\" + "ManagerOrdiniSettings.txt";
        static readonly internal string schemadb = "";

        static readonly internal CultureInfo provider = CultureInfo.InvariantCulture;
        static readonly internal NumberStyles style = NumberStyles.AllowDecimalPoint;
        static readonly internal CultureInfo culture = CultureInfo.CreateSpecificCulture("it-IT");
        static readonly internal NumberFormatInfo nfi = CultureInfo.GetCultureInfo("it-IT").NumberFormat;
        static readonly internal Calendar calendarCulture = CultureInfo.CurrentCulture.Calendar;

        static readonly internal string dateFormat = "dd/MM/yyyy";
        static readonly internal string dateFormatTime = "dd/MM/yyyy hh:mm:ss";

        static readonly internal SQLiteConnection connection = new(@"Data Source = " + exeFolderPath + db_file_path + db_file_name + @";cache=shared; synchronous  = NORMAL ;  foreign_keys  = 1;  journal_mode=WAL; temp_store = memory;  mmap_size = 30000000000; ");

        static readonly internal string connectionStringAdmin = @"Data Source = " + exeFolderPath + db_file_path + db_file_name + @";cache=shared; synchronous  = NORMAL ;  journal_mode=WAL; temp_store = memory;  mmap_size = 30000000000; ";
    }
}