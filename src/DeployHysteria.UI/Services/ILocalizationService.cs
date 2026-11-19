using System.ComponentModel;

namespace DeployHysteria.UI.Services;

public enum AppLanguage
{
    Russian,
    English
}

public interface ILocalizationService : INotifyPropertyChanged
{
    AppLanguage CurrentLanguage { get; set; }
    string this[string key] { get; }
    string GetString(string key);
}
