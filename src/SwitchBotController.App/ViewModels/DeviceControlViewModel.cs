using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwitchBotController.Core.Api;
using SwitchBotController.Core.Models;

namespace SwitchBotController.App.ViewModels;

public sealed partial class DeviceControlViewModel : ObservableObject
{
    private readonly Func<DeviceControlViewModel, SwitchBotCommand, Task> _sendCommand;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TurnOnCommand))]
    [NotifyCanExecuteChangedFor(nameof(TurnOffCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "待機中";

    public DeviceControlViewModel(
        Device device,
        Func<DeviceControlViewModel, SwitchBotCommand, Task> sendCommand)
    {
        Name = device.Name;
        DeviceId = device.DeviceId;
        _sendCommand = sendCommand;
    }

    public string Name { get; }

    public string DeviceId { get; }

    public int? LastKnownPosition { get; set; }

    private bool CanSend() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task TurnOnAsync() => _sendCommand(this, SwitchBotCommand.TurnOn);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task TurnOffAsync() => _sendCommand(this, SwitchBotCommand.TurnOff);
}
