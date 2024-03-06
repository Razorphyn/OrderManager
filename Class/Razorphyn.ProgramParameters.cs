using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace OrderManager.Class
{
    internal static class ProgramParameters
    {
        static readonly internal string exeFolderPath = Path.GetDirectoryName(Application.ExecutablePath) + @"\";
        static readonly internal string db_folder_path = @"db\";
        static readonly internal string update_file_path = @"update\";
        static readonly internal string db_file_name = @"ManagerOrdini.db";

        static readonly internal string db_file_path = Path.Combine(exeFolderPath, db_folder_path, db_file_name);

        static readonly internal string settingFile = Path.Combine(exeFolderPath, "ManagerOrdiniSettings.txt");
        static readonly internal string db_check_file = Path.Combine(exeFolderPath, db_folder_path, db_file_name);
        static readonly internal string schemadb = "";

        static readonly internal CultureInfo provider = CultureInfo.InvariantCulture;
        static readonly internal NumberStyles style = NumberStyles.AllowDecimalPoint;
        static readonly internal CultureInfo culture = CultureInfo.CreateSpecificCulture("it-IT");
        static readonly internal NumberFormatInfo nfi = CultureInfo.GetCultureInfo("it-IT").NumberFormat;
        static readonly internal Calendar calendarCulture = CultureInfo.CurrentCulture.Calendar;

        static readonly internal string dateFormat = "dd/MM/yyyy";
        static readonly internal string dateFormatTime = "dd/MM/yyyy hh:mm:ss";

        static readonly internal SQLiteConnection connection = new(@"Data Source = " + db_file_path + @";cache=shared; synchronous  = NORMAL ;  foreign_keys  = 1;  journal_mode=WAL; temp_store = memory;  mmap_size = 30000000000; ");

        static readonly internal string connectionStringAdmin = @"Data Source = " + db_file_path + @";cache=shared; synchronous  = NORMAL ;  journal_mode=WAL; temp_store = memory;  mmap_size = 30000000000; ";

        static readonly internal string patternPrefixEvent = @"##ManaOrdini";
        static readonly internal string patternEvent = @"^.+" + patternPrefixEvent + "([0-9]+)##$";
    }
}