using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScriptLauncher
{
    public enum ScriptType
    {
        PowerShell,
        Bat,
        Cmd
    }

    /// <summary>
    /// Один скрипт в списке: отображаемое имя, путь к файлу и тип (как его запускать).
    /// </summary>
    public class ScriptEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public ScriptType Type { get; set; }
    }

    /// <summary>
    /// Хранит список скриптов и отвечает за его сохранение/загрузку.
    /// Настройки хранятся в файле %AppData%\ScriptLauncher\settings.json,
    /// поэтому они не зависят от того, откуда запущен exe-файл, и не теряются
    /// при закрытии программы.
    /// </summary>
    public class AppSettings
    {
        public List<ScriptEntry> Scripts { get; set; } = new List<ScriptEntry>();

        // Поля из предыдущей версии программы (3 фиксированных пути).
        // Оставлены только для чтения старого settings.json и переноса данных
        // в список Scripts; при сохранении больше не записываются.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? PowerShellScriptPath { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? BatScriptPath { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? CmdScriptPath { get; set; }

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private static string SettingsFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScriptLauncher");

        private static string SettingsFilePath => Path.Combine(SettingsFolder, "settings.json");

        /// <summary>
        /// Загружает настройки из файла (с переносом старого формата, если нужно).
        /// Если файла нет или он повреждён — возвращает пустой список скриптов.
        /// </summary>
        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();

            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    AppSettings? loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (loaded != null)
                    {
                        settings = loaded;
                    }
                }
            }
            catch
            {
                // Файл настроек повреждён или недоступен — используем значения по умолчанию.
            }

            MigrateOldFormat(settings);
            return settings;
        }

        /// <summary>
        /// Переносит пути из старого формата (версия с тремя фиксированными слотами)
        /// в список Scripts — только если список ещё пуст.
        /// </summary>
        private static void MigrateOldFormat(AppSettings settings)
        {
            if (settings.Scripts.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(settings.PowerShellScriptPath))
                {
                    settings.Scripts.Add(new ScriptEntry
                    {
                        Name = "PowerShell скрипт",
                        Path = settings.PowerShellScriptPath!,
                        Type = ScriptType.PowerShell
                    });
                }

                if (!string.IsNullOrWhiteSpace(settings.BatScriptPath))
                {
                    settings.Scripts.Add(new ScriptEntry
                    {
                        Name = "BAT скрипт",
                        Path = settings.BatScriptPath!,
                        Type = ScriptType.Bat
                    });
                }

                if (!string.IsNullOrWhiteSpace(settings.CmdScriptPath))
                {
                    settings.Scripts.Add(new ScriptEntry
                    {
                        Name = "CMD скрипт",
                        Path = settings.CmdScriptPath!,
                        Type = ScriptType.Cmd
                    });
                }
            }

            // Старые поля больше не нужны — список Scripts теперь единственный источник данных.
            settings.PowerShellScriptPath = null;
            settings.BatScriptPath = null;
            settings.CmdScriptPath = null;
        }

        /// <summary>
        /// Сохраняет текущие настройки в JSON-файл.
        /// </summary>
        public void Save()
        {
            Directory.CreateDirectory(SettingsFolder);
            string json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
    }
}
