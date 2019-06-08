using Discord.WebSocket;
using DiVA.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace DiVA
{
    internal class DiVAConfiguration
    {
        public string Prefix { get; set; }
        public Tokens Tokens { get; set; }

        public DiVAConfiguration(string prefix = "..", Tokens token = null)
        {
            Prefix = prefix;
            Tokens = token;
        }
    }
    internal class Tokens
    {
        public string Discord { get; set; }
        public string Youtube { get; set; }
        public Tokens(string discord = "", string youtube = "")
        {
            Discord = discord;
            Youtube = youtube;
        }
    }

    public class GuildConfig
    {
        /// <summary>
        /// Generate a guild's settings
        /// </summary>
        /// <param name="guild"></param>
        public static void GenerateGuildSettings(SocketGuild guild)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[$"{guild.Id}.{GuildConfKeys.UserJoinedAllowed}"] == null)
                    settings.Add($"{guild.Id}.{GuildConfKeys.UserJoinedAllowed}", "true");
                if (settings[$"{guild.Id}.{GuildConfKeys.UserJoinedDefaultChannel}"] == null)
                    settings.Add($"{guild.Id}.{GuildConfKeys.UserJoinedDefaultChannel}", guild.DefaultChannel.Id.ToString());
                if (settings[$"{guild.Id}.{GuildConfKeys.UserLeftAllowed}"] == null)
                    settings.Add($"{guild.Id}.{GuildConfKeys.UserLeftAllowed}", "true");
                if (settings[$"{guild.Id}.{GuildConfKeys.UserLeftDefaultChannel}"] == null)
                    settings.Add($"{guild.Id}.{GuildConfKeys.UserLeftDefaultChannel}", guild.DefaultChannel.Id.ToString());
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch
            { Logger.Log(Logger.Info, "Error writing app settings", "ConfigGen"); }
        }

        /// <summary>
        /// Edit a guild's config
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void ChangeGuildSettings(SocketGuild guild, string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[$"{guild.Id}.{key}"] == null)
                    Logger.Log(Logger.Warning, $"Could not edit key {key} from guild {guild.Id} with value {value} : \nKey does not exists", "ConfigEdit");
                else
                    settings[$"{guild.Id}.{key}"].Value = value;
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch
            { Logger.Log(Logger.Info, "Error editing app settings", "ConfigGen"); }
        }

        /// <summary>
        /// Edit a guild's setting
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void ChangeGuildSettings(SocketGuild guild, GuildConfKeys key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[$"{guild.Id}.{key}"] == null)
                    Logger.Log(Logger.Warning, $"Could not edit key {key} from guild {guild.Id} with value {value} : \nKey does not exists", "ConfigEdit");
                else
                    settings[$"{guild.Id}.{key}"].Value = value;
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch
            { Logger.Log(Logger.Info, "Error editing app settings", "ConfigGen"); }
        }

        /// <summary>
        /// Get a specific value from a guild's config
        /// </summary>
        /// <param name="guild">SocketGuild target</param>
        /// <param name="key">GuildConfKeys key used</param>
        /// <returns></returns>
        public static string GetGuildSetting(SocketGuild guild, GuildConfKeys key)
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings[$"{guild.Id}.{key}"];
        }

        /// <summary>
        /// Get a specific value from a guild's config
        /// </summary>
        /// <param name="guild">SocketGuild target</param>
        /// <param name="key">string key used</param>
        /// <returns></returns>
        public static string GetGuildSetting(SocketGuild guild, string key)
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings[$"{guild.Id}.{key}"];
        }

        public static List<(string key, string value)> GetGuildSettings(SocketGuild guild)
        {
            var list = new List<(string key, string value)>();
            foreach (var method in typeof(GuildConfKeys).GetMethods())
            {
                if (method.IsStatic)
                { list.Add((method.Name.Replace("get_", ""), GetGuildSetting(guild, method.Name.Replace("get_", "")))); }
            }
            return list;
        }

            public static Type GetKeyType(string key)
        { return GuildConfKeys.Keys.FirstOrDefault(k => k.Item1.Value == key).Item2; }
    }

    /// <summary>
    /// Some kind of enum with strings
    /// </summary>
    public class GuildConfKeys
    {
        public static List<(GuildConfKeys, Type)> Keys = new List<(GuildConfKeys, Type)>
        {
            (GuildConfKeys.UserJoinedAllowed, typeof(bool)),
            (GuildConfKeys.UserJoinedDefaultChannel, typeof(ulong)),
            (GuildConfKeys.UserLeftAllowed, typeof(bool)),
            (GuildConfKeys.UserLeftDefaultChannel, typeof(ulong)),
        };
        private GuildConfKeys(string value) { Value = value; }

        public string Value { get; set; }

        public static GuildConfKeys UserJoinedAllowed { get { return new GuildConfKeys("UserJoinedAllowed"); } }
        public static GuildConfKeys UserJoinedDefaultChannel { get { return new GuildConfKeys("UserJoinedDefaultChannel"); } }
        public static GuildConfKeys UserLeftAllowed { get { return new GuildConfKeys("UserLeftAllowed"); } }
        public static GuildConfKeys UserLeftDefaultChannel { get { return new GuildConfKeys("UserLeftDefaultChannel"); } }

        public override string ToString()
        { return Value; }
    }
}