using Newtonsoft.Json;
using System.IO;

namespace OrderManager.Class
{

    internal class UserSettings
    {
        internal Root Settings = new();

        public UserSettings()
        {
            ReadSettingApp();
        }

        internal void UpdateSettingApp()
        {
            string json = JsonConvert.SerializeObject(Settings);
            File.WriteAllText(ProgramParameters.settingFile, json);
        }

        internal void ReadSettingApp()
        {

            string json = File.ReadAllText(ProgramParameters.settingFile);
            Settings = JsonConvert.DeserializeObject<Root>(json);
        }

        internal class Root
        {
            public Calendario Calendario { get; set; }
        }

        internal class Calendario
        {
            public bool AggiornaCalendario { get; set; } = true;
            public string Destinatari { get; set; }
            public string NomeCalendario { get; set; }
            public string NomeCalendarGroup { get; set; }
        }
    }
}