# Challenge Management System - Implementation Guide

## Overview
This implementation adds full challenge management capabilities including:
- Challenge CRUD operations
- Race Event management with Excel import
- Association between Challenges and Race Events  
- Updated race result processing to link with race events

## Database Schema

### New Tables Created

#### 1. **Challenges**
```sql
CREATE TABLE [Challenges] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [Year] INT NOT NULL,
    [StartDate] DATETIME2 NULL,
    [EndDate] DATETIME2 NULL,
    [CreatedDate] DATETIME2 NOT NULL,
    [ModifiedDate] DATETIME2 NULL
);
```

#### 2. **RaceEvents**
```sql
CREATE TABLE [RaceEvents] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(200) NOT NULL,
    [EventDate] DATETIME2 NOT NULL,
    [Location] NVARCHAR(200) NULL,
    [WebsiteUrl] NVARCHAR(500) NULL,
    [Description] NVARCHAR(2000) NULL,
    [CreatedDate] DATETIME2 NOT NULL,
    [ModifiedDate] DATETIME2 NULL
);
```

#### 3. **ChallengeRaceEvents** (Many-to-Many Join)
```sql
CREATE TABLE [ChallengeRaceEvents] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [ChallengeId] INT NOT NULL,
    [RaceEventId] INT NOT NULL,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    CONSTRAINT [FK_ChallengeRaceEvents_Challenges] FOREIGN KEY ([ChallengeId]) 
        REFERENCES [Challenges]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ChallengeRaceEvents_RaceEvents] FOREIGN KEY ([RaceEventId]) 
        REFERENCES [RaceEvents]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_ChallengeRaceEvents] UNIQUE ([ChallengeId], [RaceEventId])
);
```

#### 4. **Races** Table - Updated
```sql
ALTER TABLE [Races]
ADD [RaceEventId] INT NULL;

ALTER TABLE [Races]
ADD CONSTRAINT [FK_Races_RaceEvents] FOREIGN KEY ([RaceEventId])
    REFERENCES [RaceEvents]([Id]) ON DELETE SET NULL;
```

## Backend Components

### 1. **Database Models**
- `ChallengeEntity.cs` - Challenge data model
- `RaceEventEntity.cs` - Race event data model
- `ChallengeRaceEventEntity.cs` - Join table model
- `RaceEntity.cs` - Updated with RaceEventId foreign key

### 2. **Repositories**
- `ChallengeRepository.cs` - CRUD for challenges + race event associations
- `RaceEventRepository.cs` - CRUD for race events
- `RaceRepository.cs` - Updated with RaceEvent association methods

### 3. **Parsers**
- `RaceEventExcelParser.cs` - Excel parser for importing race events
  - Supports two formats:
    - Basic: Date | Name | Location | Website | Description
    - With Distances: Date | Name | Distance | Location | Website | Description

### 4. **Database Context**
- `RaceManagementContext.cs` - Updated with new DbSets and relationships
- `DatabaseInitializer.cs` - Updated with migration for RaceEventId column

## UI Components

### 1. **View Models**
- `ChallengeManagementViewModel.cs` - Challenge management logic
- `RaceEventManagementViewModel.cs` - Race event management logic
- `MainViewModel.cs` - Updated with RaceEvent selection for races

### 2. **XAML Views** (To be created)

#### ChallengeManagement.xaml
```xaml
<UserControl x:Class="NameParser.UI.Views.ChallengeManagement"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Challenge Form -->
        <GroupBox Header="Challenge Details" Grid.Row="0" Margin="0,0,0,10">
            <StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>

                    <TextBlock Text="Name:" Grid.Row="0" Grid.Column="0" Margin="5"/>
                    <TextBox Text="{Binding ChallengeName}" Grid.Row="0" Grid.Column="1" Margin="5"/>
                    
                    <TextBlock Text="Year:" Grid.Row="0" Grid.Column="2" Margin="5"/>
                    <TextBox Text="{Binding ChallengeYear}" Grid.Row="0" Grid.Column="3" Margin="5"/>

                    <TextBlock Text="Start Date:" Grid.Row="1" Grid.Column="0" Margin="5"/>
                    <DatePicker SelectedDate="{Binding StartDate}" Grid.Row="1" Grid.Column="1" Margin="5"/>
                    
                    <TextBlock Text="End Date:" Grid.Row="1" Grid.Column="2" Margin="5"/>
                    <DatePicker SelectedDate="{Binding EndDate}" Grid.Row="1" Grid.Column="3" Margin="5"/>

                    <TextBlock Text="Description:" Grid.Row="2" Grid.Column="0" Margin="5"/>
                    <TextBox Text="{Binding ChallengeDescription}" Grid.Row="2" Grid.Column="1" 
                             Grid.ColumnSpan="3" Margin="5" Height="60" TextWrapping="Wrap"/>
                </Grid>

                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="5">
                    <Button Content="Create" Command="{Binding CreateChallengeCommand}" Width="80" Margin="5"/>
                    <Button Content="Update" Command="{Binding UpdateChallengeCommand}" Width="80" Margin="5"/>
                    <Button Content="Delete" Command="{Binding DeleteChallengeCommand}" Width="80" Margin="5"/>
                    <Button Content="Clear" Command="{Binding ClearFormCommand}" Width="80" Margin="5"/>
                </StackPanel>
            </StackPanel>
        </GroupBox>

        <!-- Challenge List and Race Events -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- Challenges List -->
            <GroupBox Header="Challenges" Grid.Column="0" Margin="0,0,5,0">
                <DataGrid ItemsSource="{Binding Challenges}"
                         SelectedItem="{Binding SelectedChallenge}"
                         AutoGenerateColumns="False" IsReadOnly="True">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*"/>
                        <DataGridTextColumn Header="Year" Binding="{Binding Year}" Width="60"/>
                        <DataGridTextColumn Header="Start" Binding="{Binding StartDate, StringFormat=\{0:dd/MM/yyyy\}}" Width="90"/>
                    </DataGrid.Columns>
                </DataGrid>
            </GroupBox>

            <!-- Associated Race Events -->
            <GroupBox Header="Associated Race Events" Grid.Column="1" Margin="5,0,0,0">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="*"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>

                    <DataGrid ItemsSource="{Binding AssociatedRaceEvents}"
                             SelectedItem="{Binding SelectedAssociatedEvent}"
                             AutoGenerateColumns="False" IsReadOnly="True">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Event Name" Binding="{Binding Name}" Width="*"/>
                            <DataGridTextColumn Header="Date" Binding="{Binding EventDate, StringFormat=\{0:dd/MM/yyyy\}}" Width="90"/>
                        </DataGrid.Columns>
                    </DataGrid>

                    <Button Content="Remove Event" Command="{Binding RemoveRaceEventCommand}"
                           Grid.Row="1" HorizontalAlignment="Right" Width="120" Margin="5"/>
                </Grid>
            </GroupBox>
        </Grid>

        <!-- Available Race Events -->
        <GroupBox Header="Available Race Events (Add to Challenge)" Grid.Row="2" Margin="0,10,0,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <DataGrid ItemsSource="{Binding AvailableRaceEvents}"
                         SelectedItem="{Binding SelectedAvailableEvent}"
                         AutoGenerateColumns="False" IsReadOnly="True">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="Event Name" Binding="{Binding Name}" Width="*"/>
                        <DataGridTextColumn Header="Date" Binding="{Binding EventDate, StringFormat=\{0:dd/MM/yyyy\}}" Width="100"/>
                        <DataGridTextColumn Header="Location" Binding="{Binding Location}" Width="150"/>
                    </DataGrid.Columns>
                </DataGrid>

                <Button Content="Add to Challenge" Command="{Binding AddRaceEventCommand}"
                       Grid.Row="1" HorizontalAlignment="Right" Width="130" Margin="5"/>
            </Grid>
        </GroupBox>

        <!-- Status Message -->
        <TextBlock Text="{Binding StatusMessage}" Grid.Row="2" 
                  VerticalAlignment="Bottom" Foreground="Blue" Margin="0,5,0,0"/>
    </Grid>
</UserControl>
```

#### RaceEventManagement.xaml
```xaml
<UserControl x:Class="NameParser.UI.Views.RaceEventManagement"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Race Event Form -->
        <GroupBox Header="Race Event Details" Grid.Row="0" Margin="0,0,0,10">
            <StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>

                    <TextBlock Text="Event Name:" Grid.Row="0" Grid.Column="0" Margin="5"/>
                    <TextBox Text="{Binding EventName}" Grid.Row="0" Grid.Column="1" Margin="5"/>
                    
                    <TextBlock Text="Event Date:" Grid.Row="0" Grid.Column="2" Margin="5"/>
                    <DatePicker SelectedDate="{Binding EventDate}" Grid.Row="0" Grid.Column="3" Margin="5"/>

                    <TextBlock Text="Location:" Grid.Row="1" Grid.Column="0" Margin="5"/>
                    <TextBox Text="{Binding Location}" Grid.Row="1" Grid.Column="1" Margin="5"/>
                    
                    <TextBlock Text="Website:" Grid.Row="1" Grid.Column="2" Margin="5"/>
                    <TextBox Text="{Binding WebsiteUrl}" Grid.Row="1" Grid.Column="3" Margin="5"/>

                    <TextBlock Text="Description:" Grid.Row="2" Grid.Column="0" Margin="5"/>
                    <TextBox Text="{Binding Description}" Grid.Row="2" Grid.Column="1" 
                             Grid.ColumnSpan="3" Margin="5" Height="60" TextWrapping="Wrap"/>
                </Grid>

                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="5">
                    <Button Content="Create" Command="{Binding CreateEventCommand}" Width="80" Margin="5"/>
                    <Button Content="Update" Command="{Binding UpdateEventCommand}" Width="80" Margin="5"/>
                    <Button Content="Delete" Command="{Binding DeleteEventCommand}" Width="80" Margin="5"/>
                    <Button Content="Clear" Command="{Binding ClearFormCommand}" Width="80" Margin="5"/>
                </StackPanel>
            </StackPanel>
        </GroupBox>

        <!-- Excel Import -->
        <GroupBox Header="Import from Excel" Grid.Row="1" Margin="0,0,0,10">
            <StackPanel>
                <TextBlock Text="Import multiple race events from Excel file (Challenge Lucien 26.xlsx format)"
                          Margin="5" TextWrapping="Wrap"/>
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    
                    <TextBox Text="{Binding ImportFilePath}" IsReadOnly="True" Margin="5"/>
                    <Button Content="Browse..." Command="{Binding BrowseImportFileCommand}" 
                           Grid.Column="1" Width="80" Margin="5"/>
                    <Button Content="Import" Command="{Binding ImportFromExcelCommand}" 
                           Grid.Column="2" Width="80" Margin="5"/>
                </Grid>
            </StackPanel>
        </GroupBox>

        <!-- Race Events List -->
        <GroupBox Header="Race Events" Grid.Row="2">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="2*"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <GroupBox Header="Events" Grid.Column="0" Margin="0,0,5,0">
                    <DataGrid ItemsSource="{Binding RaceEvents}"
                             SelectedItem="{Binding SelectedRaceEvent}"
                             AutoGenerateColumns="False" IsReadOnly="True">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Event Name" Binding="{Binding Name}" Width="*"/>
                            <DataGridTextColumn Header="Date" Binding="{Binding EventDate, StringFormat=\{0:dd/MM/yyyy\}}" Width="100"/>
                            <DataGridTextColumn Header="Location" Binding="{Binding Location}" Width="120"/>
                        </DataGrid.Columns>
                    </DataGrid>
                </GroupBox>

                <GroupBox Header="Linked Challenges" Grid.Column="1" Margin="2.5,0,2.5,0">
                    <DataGrid ItemsSource="{Binding AssociatedChallenges}"
                             AutoGenerateColumns="False" IsReadOnly="True">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Challenge" Binding="{Binding Name}" Width="*"/>
                            <DataGridTextColumn Header="Year" Binding="{Binding Year}" Width="50"/>
                        </DataGrid.Columns>
                    </DataGrid>
                </GroupBox>

                <GroupBox Header="Race Distances" Grid.Column="2" Margin="5,0,0,0">
                    <DataGrid ItemsSource="{Binding AssociatedRaces}"
                             AutoGenerateColumns="False" IsReadOnly="True">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Distance (km)" Binding="{Binding DistanceKm}" Width="*"/>
                            <DataGridTextColumn Header="Status" Binding="{Binding Status}" Width="80"/>
                        </DataGrid.Columns>
                    </DataGrid>
                </GroupBox>
            </Grid>
        </GroupBox>

        <!-- Status Message -->
        <TextBlock Text="{Binding StatusMessage}" Grid.Row="3" 
                  VerticalAlignment="Bottom" Foreground="Blue" Margin="0,5,0,0"/>
    </Grid>
</UserControl>
```

### 3. **MainWindow.xaml Updates**
Add new tabs to the existing TabControl:

```xaml
<!-- Add these tabs to the existing TabControl -->
<TabItem Header="Challenge Management">
    <local:ChallengeManagement DataContext="{Binding ChallengeManagementViewModel}"/>
</TabItem>

<TabItem Header="Race Event Management">
    <local:RaceEventManagement DataContext="{Binding RaceEventManagementViewModel}"/>
</TabItem>
```

In MainWindow.xaml.cs or MainViewModel initialization:
```csharp
public ChallengeManagementViewModel ChallengeManagementViewModel { get; set; }
public RaceEventManagementViewModel RaceEventManagementViewModel { get; set; }

// In constructor
ChallengeManagementViewModel = new ChallengeManagementViewModel();
RaceEventManagementViewModel = new RaceEventManagementViewModel();
```

## Usage Workflows

### 1. **Create a Challenge**
1. Navigate to "Challenge Management" tab
2. Fill in Name, Year, optionally Start/End dates and Description
3. Click "Create"
4. Select race events from "Available Race Events" list
5. Click "Add to Challenge" to associate them

### 2. **Import Race Events**
1. Navigate to "Race Event Management" tab
2. Click "Browse..." and select Excel file (Challenge Lucien 26.xlsx format)
3. Click "Import"
4. Events will be parsed and created in database

### 3. **Process Race Results with Event Association**
1. Navigate to "Race Processing" tab (existing)
2. Upload race file (Excel/PDF)
3. Fill in race details
4. **NEW**: Select "Race Event" from dropdown (optional)
5. Process race - it will be linked to the selected event

### 4. **View Event Details**
1. Navigate to "Race Event Management"
2. Select an event from the list
3. View associated challenges and race distances in side panels

## API Methods

### ChallengeRepository
```csharp
List<ChallengeEntity> GetAll()
List<ChallengeEntity> GetByYear(int year)
ChallengeEntity GetById(int id)
int Create(ChallengeEntity challenge)
void Update(ChallengeEntity challenge)
void Delete(int id)
void AssociateRaceEvent(int challengeId, int raceEventId, int displayOrder)
void DisassociateRaceEvent(int challengeId, int raceEventId)
List<RaceEventEntity> GetRaceEventsByChallenge(int challengeId)
```

### RaceEventRepository
```csharp
List<RaceEventEntity> GetAll()
RaceEventEntity GetById(int id)
int Create(RaceEventEntity raceEvent)
void Update(RaceEventEntity raceEvent)
void Delete(int id)
List<RaceEntity> GetRacesByEvent(int raceEventId)
List<ChallengeEntity> GetChallengesByEvent(int raceEventId)
```

### RaceRepository (Updated)
```csharp
void SaveRace(RaceDistance raceDistance, int? year, string filePath, bool isHorsChallenge, int? raceEventId)
void AssociateRaceWithEvent(int raceId, int raceEventId)
void DisassociateRaceFromEvent(int raceId)
```

## Excel Import Format

### Basic Format (Challenge Lucien 26.xlsx style)
| Date       | Race Name        | Location | Website           | Description |
|------------|------------------|----------|-------------------|-------------|
| 26/04/2026 | Paris Marathon   | Paris    | www.marathon.fr   | Annual race |
| 15/05/2026 | Trail des Fagnes | Liège    | www.trailfagnes.be| Mountain    |

### Extended Format (with distances)
| Date       | Race Name      | Distance (km) | Location | Website           | Description |
|------------|----------------|---------------|----------|-------------------|-------------|
| 26/04/2026 | Paris Marathon | 42            | Paris    | www.marathon.fr   | Full        |
| 26/04/2026 | Paris Marathon | 21            | Paris    | www.marathon.fr   | Half        |
| 26/04/2026 | Paris Marathon | 10            | Paris    | www.marathon.fr   | 10K         |

## Migration Notes

1. **Database will auto-create** tables on first run via `EnsureCreated()`
2. **RaceEventId** column will be added to existing Races table via DatabaseInitializer
3. **Existing races** will have `RaceEventId = NULL` (backward compatible)
4. **No data loss** - all existing data remains intact

## Testing Checklist

- [ ] Create a new challenge
- [ ] Update challenge details
- [ ] Delete a challenge
- [ ] Import race events from Excel
- [ ] Create race event manually
- [ ] Associate race events with challenge
- [ ] Process race result with event selection
- [ ] View event associations
- [ ] Delete event (cascade check)
- [ ] Verify database relationships

## Future Enhancements

1. **Bulk operations** - Import challenges with events from Excel
2. **Event templates** - Reuse event details year over year
3. **Distance management** - UI for managing race distances per event
4. **Challenge statistics** - Dashboard showing event participation
5. **Export capabilities** - Export challenge calendar to iCal/PDF
