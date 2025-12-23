using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Pandowdy.UI;

/// <summary>
/// User control that displays Apple II soft switch status information.
/// Shows memory configuration, video modes, pushbuttons, and annunciators.
/// </summary>
public partial class SoftSwitchStatusPanel : UserControl
{
    public SoftSwitchStatusPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
