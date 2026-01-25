using Vintagestory.API.Common;
using System;
using System.IO;

namespace IndustrialRevolution.util;

public class ModLogger
{
    private readonly string logPath;
    private readonly object lockObj = new object();
    private readonly ICoreAPI api;

    public ModLogger(ICoreAPI api, string modName)
    {
        string logDir = Path.Combine(api.DataBasePath, "Logs");
        Directory.CreateDirectory(logDir);
        logPath = Path.Combine(logDir, $"{modName}.log");
        this.api = api;
    }

    public void Info(string message) => Log("INFO", message);
    public void Warning(string message) => Log("WARN", message);
    public void Error(string message) => Log("ERROR", message);
    public void Debug(string message) => Log("DEBUG", message);

    private void Log(string level, string message)
    {
        lock (lockObj)
        {
            string side = api.Side == EnumAppSide.Client ? "CLIENT" : "SERVER";
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}][{side}] {message}";
            File.AppendAllText(logPath, entry + Environment.NewLine);
        }
    }

    public void LogUnwantedNull(string val)
    {
        this.Warning($"{val} is null but shouldn't be!");
    }
}
