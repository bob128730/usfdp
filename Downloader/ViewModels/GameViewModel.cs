using ReactiveUI.Fody.Helpers;

namespace Patcher.ViewModels;

public class GameViewModel : ViewModelBase
{
    [Reactive]
    public string Name { get; set; }
    
    [Reactive]
    public string Version { get; set; }
    
    [Reactive]
    public string ReleaseDate { get; set; }

    public static GameViewModel[] GameVersions { get; set; } = {
        new() {Name = "Starfield", Version = "1.14.70", ReleaseDate = "30 Sept, 2024"},
        new() {Name = "Starfield", Version = "1.14.74", ReleaseDate = "19 Nov, 2024"},
        new() {Name = "Starfield", Version = "1.15.216", ReleaseDate = "22 May, 2025"},
        new() {Name = "Starfield", Version = "1.15.222", ReleaseDate = "5 Aug, 2025"},
        new() {Name = "Starfield", Version = "1.16.236", ReleaseDate = "7 Aug, 2026"},
    };
}