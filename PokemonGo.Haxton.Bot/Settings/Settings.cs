using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Enums;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.Settings
{
    public class Settings : ISettings
    {
        public Settings()
        {
            AuthType =
                (AuthType)Enum.Parse(typeof(AuthType), ConfigurationManager.AppSettings["AccountType"]);
            DefaultLatitude = ConfigurationManager.AppSettings["DefaultLatitude"];
            DefaultLongitude = ConfigurationManager.AppSettings["DefaultLongitude"];
            DefaultAltitude = ConfigurationManager.AppSettings["DefaultAltitude"];
            PtcUsername = ConfigurationManager.AppSettings["PtcUsername"];
            PtcPassword = ConfigurationManager.AppSettings["password"];
        }

        public AuthType AuthType { get; }
        public string DefaultLatitude { get; }
        public string DefaultLongitude { get; }
        public string DefaultAltitude { get; }

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