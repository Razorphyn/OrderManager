using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Razorphyn
{
    public static class ProgramParameters
    {
        static readonly public string exeFolderPath = Path.GetDirectoryName(Application.ExecutablePath) + @"\";
        static readonly public string db_file_path = @"db\";
        static readonly public string db_file_name = @"ManagerOrdini.db";
        static readonly public string settingFile = exeFolderPath + @"\" + "ManagerOrdiniSettings.txt";
        static readonly public string schemadb = "";

        static readonly public CultureInfo provider = CultureInfo.InvariantCulture;
        static readonly public NumberStyles style = NumberStyles.AllowDecimalPoint;
        static readonly public CultureInfo culture = CultureInfo.CreateSpecificCulture("it-IT");
        static readonly public NumberFormatInfo nfi = CultureInfo.GetCultureInfo("it-IT").NumberFormat;
        static readonly public string dateFormat = "dd/MM/yyyy";
        static readonly public string dateFormatTime = "dd/MM/yyyy hh:mm:ss";

        static readonly public SQLiteConnection connection = new SQLiteConnection(@"Data Source = " + exeFolderPath + db_file_path + db_file_name + @";cache=shared; synchronous  = NORMAL ;  foreign_keys  = 1;  journal_mode=WAL; temp_store = memory;  mmap_size = 30000000000; ");
    }

}