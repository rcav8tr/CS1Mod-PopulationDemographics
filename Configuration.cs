namespace PopulationDemographics
{
    /// <summary>
    /// define global (i.e. for this mod but not game specific) configuration properties
    /// </summary>
    /// <remarks>convention for the config file name seems to be the mod name + "Config.xml"</remarks>
    [ConfigurationFileName("PopulationDemographicsConfig.xml")]
    public class Configuration
    {
        // it is important to set default config values in case there is no config file

        // configuration parameters
        public bool PanelVisible = true;
        public int RowSelection = (int)PopulationDemographicsPanel.RowSelection.AgeGroup;
        public int ColumnSelection = (int)PopulationDemographicsPanel.ColumnSelection.Education;
        public bool CountStatus = true;

        /// <summary>
        /// save the panel visible to the global config file
        /// </summary>
        public static void SavePanelVisible(bool visible)
        {
            Configuration config = ConfigurationUtil<Configuration>.Load();
            config.PanelVisible = visible;
            ConfigurationUtil<Configuration>.Save();
        }

        /// <summary>
        /// save the row selection to the global config file
        /// </summary>
        public static void SaveRowSelection(int index)
        {
            Configuration config = ConfigurationUtil<Configuration>.Load();
            config.RowSelection = index;
            ConfigurationUtil<Configuration>.Save();
        }

        /// <summary>
        /// save the column selection to the global config file
        /// </summary>
        public static void SaveColumnSelection(int index)
        {
            Configuration config = ConfigurationUtil<Configuration>.Load();
            config.ColumnSelection = index;
            ConfigurationUtil<Configuration>.Save();
        }

        /// <summary>
        /// save the count status to the global config file
        /// </summary>
        public static void SaveCountStatus(bool count)
        {
            Configuration config = ConfigurationUtil<Configuration>.Load();
            config.CountStatus = count;
            ConfigurationUtil<Configuration>.Save();
        }
    }
}