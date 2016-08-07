using System;
using System.Configuration;
using Mono.Options;
using NLog;

namespace PokemonGo.Haxton.Bot.Settings
{
    public interface ILineArguments
    {
        Configuration GetConfig(string[] args);
    }
    public class LineArguments : ILineArguments
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private Configuration Config { get; set; }

        public LineArguments()
        {
            Config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        public static bool ShowHelp = false;
        public Configuration GetConfig(string[] args)
        {
            ParseArguments(args);
            return Config;
        }

        private void ParseArguments(string[] args)
        {
            var options = GetOptions();

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                logger.Error(e);
                ShowHelp = true;
            }

            if(ShowHelp)
            {
                options.WriteOptionDescriptions(Console.Out);
            }
        }

        private OptionSet GetOptions()
        {
            var options = new OptionSet()
            {
                { "config=", "Directory of config file(see default config for formatting)", config => {
                    ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
                    configMap.ExeConfigFilename = config;
                    Config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
                    if (!Config.HasFile)
                    {
                        throw new OptionException("config argument could not find the specified file at " + Config.FilePath, "config=");
                    }
                } },
                { "auth=", "Type of login 'Google' or 'Ptc'", auth => {
                    if (auth != "Google" && auth != "Ptc")
                    {
                        throw new OptionException("auth arugment must be either 'Google' or 'Ptc'", "auth=");
                    }
                    ChangeConfigKey("AccountType", auth);
                } },
                { "username=", "Username of Google or Ptc account", username => {
                    if (Config.AppSettings.Settings["AccountType"].Value == "Google")
                    {
                        ChangeConfigKey("GoogleEmail", username);
                    }
                    else if (Config.AppSettings.Settings["AccountType"].Value == "Ptc")
                    {
                        ChangeConfigKey("PtcUsername", username);
                    }
                    else
                    {
                        throw new OptionException("Invalid auth specified. Use -auth= or edit the config to set auth type", "username=");
                    }
                } },
                { "password=", "Password of Google or Ptc account", password => {
                    if (Config.AppSettings.Settings["AccountType"].Value == "Google")
                    {
                        ChangeConfigKey("GooglePassword", password);
                    }
                    else if (Config.AppSettings.Settings["AccountType"].Value == "Ptc")
                    {
                        ChangeConfigKey("PtcPassword", password);
                    }
                    else
                    {
                        throw new OptionException("Invalid auth specified. Use -auth= or edit the config to set auth type", "password=");
                    }
                } },
                { "latitude=", "Default latitude of Pokemon trainer", latitude => ChangeConfigKey("DefaultLatitude", latitude)},
                { "longitude=", "Default longitude of Pokemon trainer", longitude => ChangeConfigKey("DefaultLongitude", longitude)},
                { "altitude=", "Default altitude of Pokemon trainer", altitude => ChangeConfigKey("DefaultAltitude", altitude) },
                { "h|help", "Display command line argument help",  h => ShowHelp = h != null}
            };

            return (options);
        }

        private void ChangeConfigKey(string key, string value)
        {
            //this does not actually save anything to the file
            var element = Config.AppSettings.Settings[key];
            if (element != null)
            {
                
                Config.AppSettings.Settings.Remove(key);
                element.Value = value;
                Config.AppSettings.Settings.Add(element);
            }
        }
    }
}
