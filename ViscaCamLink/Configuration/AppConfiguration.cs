namespace ViscaCamLink.Configuration
{
    using System;
    using System.Configuration;

    public class AppConfiguration : 
        ILayoutConfiguration, 
        IConnectionConfiguration, 
        IControlConfiguration, 
        IMemoryConfiguration
    {   
        // Layout

        public Boolean MemoryContainerVisible
        {
            get => GetSetting<Boolean>(nameof(MemoryContainerVisible), true);

            set => AddOrUpdateAppSettings(nameof(MemoryContainerVisible), value.ToString());
        }

        public Boolean MoveContainerVisible
        {
            get => GetSetting<Boolean>(nameof(MoveContainerVisible), true);

            set => AddOrUpdateAppSettings(nameof(MoveContainerVisible), value.ToString());
        }

        public Boolean ZoomContainerVisible
        {
            get => GetSetting<Boolean>(nameof(ZoomContainerVisible), true);

            set => AddOrUpdateAppSettings(nameof(ZoomContainerVisible), value.ToString());
        }

        // Connection

        public String Ip
        {
            get => GetSetting<String>(nameof(Ip), "192.168.1.0");

            set => AddOrUpdateAppSettings(nameof(Ip), value);
        }

        public Int32 Port
        {
            get => GetSetting<Int32>(nameof(Port), 5678);

            set => AddOrUpdateAppSettings(nameof(Port), value.ToString());
        }

        // Control

        public Int32 PanTiltSpeed
        {
            get => GetSetting<Int32>(nameof(PanTiltSpeed), 1);

            set => AddOrUpdateAppSettings(nameof(PanTiltSpeed), value.ToString());
        }

        public Int32 ZoomSpeed
        {
            get => GetSetting<Int32>(nameof(ZoomSpeed), 1);

            set => AddOrUpdateAppSettings(nameof(ZoomSpeed), value.ToString());
        }

        // Memory

        public String Slot0Name
        {
            get => GetSetting<String>(nameof(Slot0Name), String.Empty);

            set => AddOrUpdateAppSettings(nameof(Slot0Name), value);
        }

        public String Slot1Name
        {
            get => GetSetting<String>(nameof(Slot1Name), String.Empty);

            set => AddOrUpdateAppSettings(nameof(Slot1Name), value);
        }

        public String Slot2Name
        {
            get => GetSetting<String>(nameof(Slot2Name), String.Empty);

            set => AddOrUpdateAppSettings(nameof(Slot2Name), value);
        }

        public String Slot3Name
        {
            get => GetSetting<String>(nameof(Slot3Name), String.Empty);

            set => AddOrUpdateAppSettings(nameof(Slot3Name), value);
        }

        public String Slot4Name
        {
            get => GetSetting<String>(nameof(Slot4Name), String.Empty);

            set => AddOrUpdateAppSettings(nameof(Slot4Name), value);
        }

        public String Slot5Name
        {
            get => GetSetting<String>(nameof(Slot5Name), String.Empty);

            set => AddOrUpdateAppSettings(nameof(Slot5Name), value);
        }

        public String Slot6Name
        {
            get => GetSetting<String>(nameof(Slot6Name), String.Empty);

            set => AddOrUpdateAppSettings(nameof(Slot6Name), value);
        }

        public String Slot7Name
        {
            get => GetSetting<String>(nameof(Slot7Name), String.Empty);

            set => AddOrUpdateAppSettings(nameof(Slot7Name), value);
        }

        public String Slot8Name
        {
            get => GetSetting<String>(nameof(Slot8Name), String.Empty);

            set => AddOrUpdateAppSettings(nameof(Slot8Name), value);
        }

        public String Slot9Name
        {
            get => GetSetting<String>(nameof(Slot9Name), String.Empty);

            set => AddOrUpdateAppSettings(nameof(Slot9Name), value);
        }

        // Util

        private static T GetSetting<T>(String key, T defaultValue)
        {
            var configValue = ConfigurationManager.AppSettings[key];

            if (configValue == null)
            {
                return defaultValue;
            }
            var typedConfigValue = (T?)Convert.ChangeType(configValue, typeof(T));

            return typedConfigValue == null ? defaultValue : typedConfigValue;
        }

        private static void AddOrUpdateAppSettings(String key, String value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;

                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }
    }
}
