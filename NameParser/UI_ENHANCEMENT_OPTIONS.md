# Enhanced UI Display Options

## Optional: Add Visual Indicators for Race Type

If you want to add visual indicators to show which type of race each result is from, you can enhance the UI further:

### Option 1: Add a Race Type Column

Add this column to the DataGrid in `MainWindow.xaml`:

```xml
<DataGridTemplateColumn Header="Type" Width="80">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Border CornerRadius="3" Padding="5,2" HorizontalAlignment="Center">
                <Border.Style>
                    <Style TargetType="Border">
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding TimePerKm, Converter={StaticResource TimeSpanToStringConverter}}" Value="-">
                                <Setter Property="Background" Value="#E3F2FD"/>
                                <Setter Property="BorderBrush" Value="#2196F3"/>
                            </DataTrigger>
                            <DataTrigger Binding="{Binding RaceTime, Converter={StaticResource TimeSpanToStringConverter}}" Value="-">
                                <Setter Property="Background" Value="#FFF3E0"/>
                                <Setter Property="BorderBrush" Value="#FF9800"/>
                            </DataTrigger>
                        </Style.Triggers>
                        <Setter Property="BorderThickness" Value="1"/>
                    </Style>
                </Border.Style>
                <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center">
                    <TextBlock.Style>
                        <Style TargetType="TextBlock">
                            <Setter Property="Text" Value="Race"/>
                            <Setter Property="Foreground" Value="#2196F3"/>
                            <Setter Property="FontWeight" Value="SemiBold"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding RaceTime, Converter={StaticResource TimeSpanToStringConverter}}" Value="-">
                                    <Setter Property="Text" Value="Time/km"/>
                                    <Setter Property="Foreground" Value="#FF9800"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </TextBlock.Style>
                </TextBlock>
            </Border>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

### Option 2: Color-Code Rows by Race Type

Add row styling to the DataGrid to color-code based on race type:

```xml
<DataGrid.RowStyle>
    <Style TargetType="DataGridRow">
        <Style.Triggers>
            <!-- Race Time races - Blue tint -->
            <DataTrigger Binding="{Binding TimePerKm, Converter={StaticResource TimeSpanToStringConverter}}" Value="-">
                <Setter Property="Background" Value="#F5F9FF"/>
            </DataTrigger>
            <!-- Time/km races - Orange tint -->
            <DataTrigger Binding="{Binding RaceTime, Converter={StaticResource TimeSpanToStringConverter}}" Value="-">
                <Setter Property="Background" Value="#FFF9F5"/>
            </DataTrigger>
        </Style.Triggers>
    </Style>
</DataGrid.RowStyle>
```

### Option 3: Tooltip Information

Add tooltips to show additional race information:

```xml
<DataGridTextColumn Header="Race Time" 
                    Binding="{Binding RaceTime, Converter={StaticResource TimeSpanToStringConverter}}" 
                    Width="110">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="ToolTip">
                <Setter.Value>
                    <ToolTip>
                        <StackPanel>
                            <TextBlock Text="Race Finish Time" FontWeight="Bold"/>
                            <TextBlock Text="For races with distance ≥ 15 minutes"/>
                        </StackPanel>
                    </ToolTip>
                </Setter.Value>
            </Setter>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>

<DataGridTextColumn Header="Time/km" 
                    Binding="{Binding TimePerKm, Converter={StaticResource TimeSpanToStringConverter}}" 
                    Width="100">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="ToolTip">
                <Setter.Value>
                    <ToolTip>
                        <StackPanel>
                            <TextBlock Text="Time Per Kilometer" FontWeight="Bold"/>
                            <TextBlock Text="For races with distance &lt; 15 minutes"/>
                        </StackPanel>
                    </ToolTip>
                </Setter.Value>
            </Setter>
        </Style>
    </DataGridTextColumn.ElementStyle>
</DataGridTextColumn>
```

### Option 4: Add Icons

Use emojis or symbols to indicate race type:

```xml
<DataGridTextColumn Header="🏃 Race Time" 
                    Binding="{Binding RaceTime, Converter={StaticResource TimeSpanToStringConverter}}" 
                    Width="130"/>
<DataGridTextColumn Header="⏱️ Time/km" 
                    Binding="{Binding TimePerKm, Converter={StaticResource TimeSpanToStringConverter}}" 
                    Width="120"/>
```

## Complete Enhanced DataGrid Example

Here's a complete example with all enhancements:

```xml
<DataGrid ItemsSource="{Binding Classifications}" 
          AutoGenerateColumns="False" 
          IsReadOnly="True"
          Margin="5"
          AlternationCount="2"
          GridLinesVisibility="Horizontal"
          HeadersVisibility="Column">
    
    <!-- Row Style with alternating colors and race type tinting -->
    <DataGrid.RowStyle>
        <Style TargetType="DataGridRow">
            <Setter Property="Background" Value="White"/>
            <Style.Triggers>
                <Trigger Property="IsAlternating" Value="True">
                    <Setter Property="Background" Value="#FAFAFA"/>
                </Trigger>
                <DataTrigger Binding="{Binding TimePerKm, Converter={StaticResource TimeSpanToStringConverter}}" Value="-">
                    <Setter Property="BorderBrush" Value="#2196F3"/>
                    <Setter Property="BorderThickness" Value="0,0,3,0"/>
                </DataTrigger>
                <DataTrigger Binding="{Binding RaceTime, Converter={StaticResource TimeSpanToStringConverter}}" Value="-">
                    <Setter Property="BorderBrush" Value="#FF9800"/>
                    <Setter Property="BorderThickness" Value="0,0,3,0"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </DataGrid.RowStyle>
    
    <DataGrid.Columns>
        <DataGridTextColumn Header="Rank" Binding="{Binding Id}" Width="60"/>
        <DataGridTextColumn Header="First Name" Binding="{Binding MemberFirstName}" Width="*"/>
        <DataGridTextColumn Header="Last Name" Binding="{Binding MemberLastName}" Width="*"/>
        <DataGridTextColumn Header="Points" Binding="{Binding Points}" Width="100"/>
        
        <!-- Race Time with icon and tooltip -->
        <DataGridTextColumn Header="🏃 Race Time" 
                            Binding="{Binding RaceTime, Converter={StaticResource TimeSpanToStringConverter}}" 
                            Width="130">
            <DataGridTextColumn.ElementStyle>
                <Style TargetType="TextBlock">
                    <Setter Property="HorizontalAlignment" Value="Center"/>
                    <Setter Property="FontFamily" Value="Consolas"/>
                    <Setter Property="ToolTip" Value="Race finish time (for race time events)"/>
                </Style>
            </DataGridTextColumn.ElementStyle>
        </DataGridTextColumn>
        
        <!-- Time/km with icon and tooltip -->
        <DataGridTextColumn Header="⏱️ Time/km" 
                            Binding="{Binding TimePerKm, Converter={StaticResource TimeSpanToStringConverter}}" 
                            Width="120">
            <DataGridTextColumn.ElementStyle>
                <Style TargetType="TextBlock">
                    <Setter Property="HorizontalAlignment" Value="Center"/>
                    <Setter Property="FontFamily" Value="Consolas"/>
                    <Setter Property="ToolTip" Value="Time per kilometer (for time/km events)"/>
                </Style>
            </DataGridTextColumn.ElementStyle>
        </DataGridTextColumn>
        
        <DataGridTextColumn Header="Bonus KM" Binding="{Binding BonusKm}" Width="100"/>
    </DataGrid.Columns>
</DataGrid>
```

## Benefits of Enhanced Display

✅ **Visual Clarity**: Immediately see race type without reading column values
✅ **Professional Look**: Polished UI with icons and colors
✅ **Better UX**: Tooltips provide context
✅ **Easy Scanning**: Color coding helps scan large datasets
✅ **Consistency**: Uniform styling across the application

## Implementation Steps

1. Choose which enhancements you want
2. Update `MainWindow.xaml` with the desired code
3. Test with both race types to verify appearance
4. Adjust colors/styles to match your branding

## Color Scheme

Current suggested colors:
- **Race Time events**: Blue (#2196F3) - represents endurance
- **Time/km events**: Orange (#FF9800) - represents speed
- You can customize these to match your preferences
