using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Enums;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace PokemonGo.Haxton.Bot.Settings
{
    public class Settings : ISettings
    {
        public Settings(LineArguments lineArguments, string[] args)
        {
            Configuration config = lineArguments.GetConfig(args);

            AuthType =
                (AuthType)Enum.Parse(typeof(AuthType), config.AppSettings.Settings["AccountType"].Value);
            DefaultLatitude = Convert.ToDouble(config.AppSettings.Settings["DefaultLatitude"].Value, CultureInfo.InvariantCulture);
            DefaultLongitude = Convert.ToDouble(config.AppSettings.Settings["DefaultLongitude"].Value, CultureInfo.InvariantCulture);
            DefaultAltitude = Convert.ToDouble(config.AppSettings.Settings["DefaultAltitude"].Value, CultureInfo.InvariantCulture);
            PtcUsername = config.AppSettings.Settings["PtcUsername"].Value;
            PtcPassword = config.AppSettings.Settings["PtcPassword"].Value;

            GoogleUsername = config.AppSettings.Settings["GoogleEmail"].Value;
            GooglePassword = config.AppSettings.Settings["GooglePassword"].Value;
        }

        public AuthType AuthType { get; }
        public double DefaultLatitude { get; }
        public double DefaultLongitude { get; }
        public double DefaultAltitude { get; }

        private string _googleRefreshToken;

        public string GoogleRefreshToken
        {
            get
            {
                if (File.Exists(Directory.GetCurrentDirectory() + "\\Configs\\GoogleAuth.ini"))
                    _googleRefreshToken = File.ReadAllText(Directory.GetCurrentDirectory() + "\\Configs\\GoogleAuth.ini");
                return _googleRefreshToken;
            }
            set
            {
                if (!File.Exists(Directory.GetCurrentDirectory() + "\\Configs"))
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Configs");
                File.WriteAllText(Directory.GetCurrentDirectory() + "\\Configs\\GoogleAuth.ini", value);
                _googleRefreshToken = value;
            }
        }

        public string PtcPassword { get; }
        public string PtcUsername { get; }
        public string GoogleUsername { get; }
        public string GooglePassword { get; }
        public string ApiUrl { get; set; }
    }
}