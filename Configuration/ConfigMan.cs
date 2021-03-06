﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Grimoire.Configuration.Structures;
using Newtonsoft.Json;
using Grimoire.Configuration.Enums;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Grimoire.Configuration
{
    /// <summary>
    /// Provides configuration storage and real time manipulation
    /// </summary>
    public class ConfigMan
    {
        /// <summary>
        /// List of Configuration descriptions
        /// </summary>
        public List<Option> Options = new List<Option>();

        string confDir = Directory.GetCurrentDirectory();
        string confName = Defaults.ConfigName;
        string _confPath = string.Empty;

        string confPath
        {
            get
            {
                if (string.IsNullOrEmpty(_confPath))
                    _confPath = $"{confDir}\\{confName}";

                return _confPath;
            }
            set
            {
                _confPath = value;
                confDir = Path.GetDirectoryName(_confPath);
                confName = Path.GetFileName(_confPath);
            }
        }

        string confText = string.Empty;

        /// <summary>
        /// Fully qualified path to the directory holding the configuration .json
        /// </summary>
        public string FolderName => confDir;

        /// <summary>
        /// Fully qualified path to the configuration .json (includes FolderName)
        /// </summary>
        public string FileName => confPath;

        /// <summary>
        /// Count of the elements in the Settings property
        /// </summary>
        public int Count => Options.Count;

        /// <summary>
        /// Default constructor which will use default path variables
        /// </summary>
        public ConfigMan() => parse();

        /// <summary>
        /// Constructor for initializing with a directory and file name.
        /// </summary>
        /// <param name="Directory">Directory holding the configuration .json</param>
        /// <param name="FileName">Name of the configuration .json</param>
        public ConfigMan(string Directory, string FileName)
        {
            confDir = Directory;
            confName = FileName;

            parse();
        }

        /// <summary>
        /// Constructor for initializing with a fully qualified file path to the config .json
        /// </summary>
        /// <param name="FilePath"></param>
        public ConfigMan(string FilePath) => confPath = FilePath;

        public dynamic this[int index] => Options?[index];

        public dynamic this[string key]
        {
            get
            {
                int index = Options.FindIndex(o => o.Name == key);
                if (index >= 0)
                    return Options[index].Value;
                else
                    return null;
            }
            set
            {
                int index = Options.FindIndex(o => o.Name == key);

                if (index >= 0)
                    Options[index].Value = value;
            }
        }

        public dynamic this[string key, string parent]
        {
            get
            {
                int index = Options.FindIndex(o => o.Name == key && o.Parent == parent);
                if (index >= 0)
                    return Options[index].Value;
                else
                    return null;
            }
            set
            {
                int index = Options.FindIndex(o => o.Name == key && o.Parent == parent);

                if (index >= 0)
                    Options[index].Value = value;
            }
        }

        public dynamic GetOption(int index) => Options?[index];

        public dynamic GetOption(string key) => Options.Find(o => o.Name == key);

        public dynamic GetOption(string key, string parent) => Options.Find(o => o.Name == key && o.Parent == parent);

        public string GetDirectory(string key, string parent = null)
        {
            Option opt = (parent != null) ? GetOption(key, parent) : GetOption(key);

            string fileDir = null;
            string filePath = null;
            string valStr = opt.Value;

            if (valStr.StartsWith("/")) //Only the folder name is given
            {
                string workingDir = Directory.GetCurrentDirectory();
                fileDir = workingDir;
                valStr = valStr.Replace("/", "\\");
            }
            else
                fileDir = valStr;

            filePath = $"{fileDir}{valStr as string}";

            return filePath;
        }

        //TODO: GetPath

        void parse()
        {
            if (File.Exists(confPath))
                confText = File.ReadAllText(confPath);
            else
                confText = write();

            JObject grandParent = JObject.Parse(confText);

            if (grandParent.Count > 0) //Gets root of json as JObject
            {
                Options.Clear();

                foreach (JToken parent in grandParent.Children())
                    foreach (JToken child in parent.Children())
                        foreach (JToken grandChild in child.Children())
                        {
                            var val = grandChild.Value<dynamic>();
                            dynamic value = null;

                            string[] info = val.Path.ToString().Split('.');

                            if (val.Value.Type == JTokenType.Array)
                            {
                                List<string> array = new List<string>();

                                foreach (var item in val.Value.Children())
                                    array.Add(item.Value);

                                value = array;
                            }
                            else
                                value = val.Value.Value;

                            Options.Add(new Option()
                            {
                                Parent = info[0],
                                Name = info[1],
                                Type = value.GetType(),
                                Value = value,
                                Comments = new List<string>()
                            });
                        }
            }
            else
                throw new JsonException();
        }

        //TODO: Write Config.json
        private string write()
        {
            ConfigWriter config = new ConfigWriter
            {
                Grim = new Grim
                {
                    BuildDirectory = "/Output"
                },
                Tab = new Tab
                {
                    Styles = new string[] { "RDB", "DATA", "HASHER" },
                    DefaultStyle = "RDB"
                },
                DB = new DB
                {
                    Engine = 0,
                    IP = "127.0.0.1",
                    Port = 1433,
                    Trusted = true,
                    WorldName = "Arcadia",
                    WorldUser = "",
                    WorldPass = "",
                    Backup = false,
                    DropOnExport = false,
                    Timeout = 0
                },
                RDB = new RDB
                {
                    Struct_AutoLoad = true,
                    Directory = "/Structures",
                    LoadDirectory = "",
                    AppendASCII = true,
                    SaveHashed = true,
                    CSV_Directory = "/CSV",
                    SQL_Directory = "/SQL"
                },
                Data = new Data
                {
                    LoadDirectory = "C:\\Webzen\\Rappelz US",
                    Encoding = 1252,
                    Backup = true,
                    ClearOnCreate = false
                },
                Hash = new Hash
                {
                    AutoClear = true,
                    AutoConvert = true,
                    Type = 0
                },
                Log = new Log
                {
                    Directory = "/Logs",
                    DisplayLevel = 0,
                    WriteLevel = 0,
                    SaveOnExit = true,
                    RefreshInterval = 1
                },
                Flag = new Flag
                {
                    Directory = "/Flags",
                    Default = "use_flags_81",
                    ClearOnChange = false
                },
                Localization = new Localization
                {
                    Directory = "/Localization",
                    Locale = "en-US"
                }
            };


            string configOutput = JsonConvert.SerializeObject(config, Formatting.Indented);

            if (!File.Exists(confPath))
                File.WriteAllText(confPath, configOutput);

            return configOutput;
        }
    }
}
