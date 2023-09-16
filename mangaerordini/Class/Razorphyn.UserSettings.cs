using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Razorphyn
{

    public class UserSettings
    {
        public Dictionary<string, Dictionary<string, string>> settings = new();

        public UserSettings()
        {
            ReadSettingApp();
        }

        public void UpdateSettingApp()
        {
            string json = JsonConvert.SerializeObject(settings);
            File.WriteAllText(ProgramParameters.settingFile, json);
        }

        private void ReadSettingApp()
        {
            settings.Add("calendario", new Dictionary<string, string>());
            settings["calendario"].Add("aggiornaCalendario", "true");
            settings["calendario"].Add("destinatari", "");
            settings["calendario"].Add("nomeCalendario", "");

            string json = File.ReadAllText(ProgramParameters.settingFile);
            Dictionary<string, Dictionary<string, string>> read_settings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);

            CopyDict(read_settings);
        }

        public void CopyDict(Dictionary<string, Dictionary<string, string>> dict)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> rootKv in dict)
            {
                foreach (KeyValuePair<string, string> childKv in rootKv.Value)
                {
                    settings[rootKv.Key][childKv.Key] = dict[rootKv.Key][childKv.Key];
                }
            }
        }

    }
}