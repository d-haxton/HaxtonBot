using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Enums;
using System;
using System.Configuration;
using System.IO;

namespace PokemonGo.Haxton.Bot.Settings
{
    public class Settings : ISettings
    {
        public Settings()
        {
            AuthType =
                (AuthType)Enum.Parse(typeof(AuthType), ConfigurationManager.AppSettings["AccountType"]);
            DefaultLatitude = Convert.ToDouble(ConfigurationManager.AppSettings["DefaultLatitude"]);
            DefaultLongitude = Convert.ToDouble(ConfigurationManager.AppSettings["DefaultLongitude"]);
            DefaultAltitude = Convert.ToDouble(ConfigurationManager.AppSettings["DefaultAltitude"]);
            PtcUsername = ConfigurationManager.AppSettings["PtcUsername"];
            PtcPassword = ConfigurationManager.AppSettings["PtcPassword"];
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
        public string ApiUrl { get; set; }
    }
}