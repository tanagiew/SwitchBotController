using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SwitchBotController.App;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var displayScale = GetDpiForWindow(windowHandle) / 96d;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(
            (int)Math.Round(960 * displayScale),
            (int)Math.Round(720 * displayScale)));

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage));
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint windowHandle);
}
