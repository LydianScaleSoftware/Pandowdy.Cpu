# Adding Custom TrueType Fonts to Pandowdy.UI (Avalonia)

## Overview
This guide explains how to add and use custom TrueType fonts (like Apple2Forever.ttf and Apple2Forever80.ttf) in your Avalonia application.

## Steps to Add Fonts

### 1. Place Font Files in Assets Directory
- Create `assets/fonts/` directory if it doesn't exist
- Copy your .ttf files into this directory:
  - `assets/fonts/Apple2Forever.ttf`
  - `assets/fonts/Apple2Forever80.ttf`

### 2. Update Pandowdy.UI.csproj
Uncomment the font references section in `Pandowdy.UI/Pandowdy.UI.csproj`:

```xml
<!-- Include custom fonts as resources -->
<ItemGroup>
  <None Include="..\assets\fonts\Apple2Forever.ttf">
    <Link>Assets\Fonts\Apple2Forever.ttf</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Include="..\assets\fonts\Apple2Forever80.ttf">
    <Link>Assets\Fonts\Apple2Forever80.ttf</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

This configuration:
- **Link**: Places fonts in `Assets/Fonts` folder in the output (visible in project explorer)
- **CopyToOutputDirectory**: Ensures fonts are copied to the build output directory

### 3. Rebuild Solution
- Build the project to verify fonts are copied to output
- Fonts will be available at runtime

## Using Fonts in XAML

Once registered, use the fonts in your XAML controls:

### In MainWindow.axaml or other XAML files:

```xaml
<!-- Using Apple2Forever font -->
<TextBlock 
    Text="Hello Apple II!" 
    FontFamily="avares://pandowdy/Assets/Fonts#Apple2Forever"
    FontSize="16" />

<!-- Using Apple2Forever80 font -->
<TextBlock 
    Text="Apple II 80 Column" 
    FontFamily="avares://pandowdy/Assets/Fonts#Apple2Forever80"
    FontSize="14" />
```

### In C# Code-Behind:

```csharp
using Avalonia.Media;

// Create a font family reference
var fontFamily = new FontFamily("avares://pandowdy/Assets/Fonts#Apple2Forever");

// Use in a TextBlock
var textBlock = new TextBlock
{
    Text = "Hello Apple II!",
    FontFamily = fontFamily,
    FontSize = 16
};
```

## Font Family URI Format

```
avares://[AssemblyName]/[Path]#[FontName]
```

- **avares://**: Avalonia resource URI scheme
- **pandowdy**: Your project assembly name (matches namespace)
- **Assets/Fonts**: Path to fonts folder in project structure
- **#Apple2Forever**: Font name as it appears in the TTF file (use # separator)

## Finding Font Names

To find the exact font name to use in the URI:

1. **Open TTF file in Windows**: Right-click ? Properties ? Details
2. **Look for "Font name"** field (not filename)
3. Or use a tool like FontForge to inspect the font

For your fonts:
- `Apple2Forever.ttf` ? likely displays as "Apple2Forever"
- `Apple2Forever80.ttf` ? likely displays as "Apple2Forever80"

## Troubleshooting

### Fonts not appearing?
1. Verify font files exist in `assets/fonts/` folder
2. Check that `.csproj` references are uncommented and correct
3. Rebuild the solution (Clean + Rebuild)
4. Verify font URI in XAML matches exactly
5. Restart Visual Studio if needed

### Font file path issues?
- Ensure paths use forward slashes in URIs: `Assets/Fonts` (not `Assets\Fonts`)
- Use backslashes in `.csproj` paths: `..\assets\fonts\` ?

### Build errors?
- Verify the font files actually exist at the specified paths
- Check that filenames match exactly (case-sensitive in URIs)
- Make sure `CopyToOutputDirectory` is set to `PreserveNewest`

## Example: Using Fonts for Apple II Content

```xaml
<Grid>
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
        <!-- Display Applesoft BASIC code with Apple2Forever font -->
        <TextBlock 
            Text="10 PRINT &quot;HELLO WORLD&quot;"
            FontFamily="avares://pandowdy/Assets/Fonts#Apple2Forever"
            FontSize="14"
            Foreground="Green"
            Background="Black"
            Padding="10" />
            
        <!-- Display 80-column text with Apple2Forever80 font -->
        <TextBlock 
            Text="This is 80-column Apple II text"
            FontFamily="avares://pandowdy/Assets/Fonts#Apple2Forever80"
            FontSize="12"
            Foreground="White"
            Background="Blue"
            Padding="10"
            Margin="0,10,0,0" />
    </StackPanel>
</Grid>
```

## Resources

- [Avalonia Font Families Documentation](https://docs.avaloniaui.net/guides/styles-and-resources/how-to-use-fonts)
- [avares:// Protocol](https://docs.avaloniaui.net/guides/basics/assets)
- [Font Classification (Wikipedia)](https://en.wikipedia.org/wiki/Font_family_(CSS))
