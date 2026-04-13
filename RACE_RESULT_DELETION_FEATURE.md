# Race Result Deletion Feature

## Overview
Added the ability to delete individual race results (classifications) directly from the Race Classification gridview. This complements the existing "Delete Race" feature which deletes entire races.

## Changes Made

### 1. ClassificationRepository (`NameParser\Infrastructure\Data\ClassificationRepository.cs`)
Added a new method to delete individual classification entries:

```csharp
public void DeleteClassification(int classificationId)
{
    using (var context = new RaceManagementContext())
    {
        var classification = context.Classifications.Find(classificationId);
        if (classification != null)
        {
            context.Classifications.Remove(classification);
            context.SaveChanges();
        }
    }
}
```

### 2. MainViewModel (`NameParser.UI\ViewModels\MainViewModel.cs`)

#### Added Command Property
```csharp
public ICommand DeleteClassificationCommand { get; }
```

#### Added Command Initialization
```csharp
DeleteClassificationCommand = new RelayCommand<ClassificationEntity>(ExecuteDeleteClassification);
```

#### Added Command Implementation
```csharp
private void ExecuteDeleteClassification(ClassificationEntity classification)
{
    if (classification == null) return;

    var result = MessageBox.Show(
        $"Are you sure you want to delete this race result?\n\n" +
        $"Participant: {classification.MemberFirstName} {classification.MemberLastName}\n" +
        $"Position: {classification.Position}\n" +
        $"Time: {classification.RaceTime?.ToString(@"hh\:mm\:ss")}\n\n" +
        $"This action cannot be undone.",
        "Confirm Delete",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

    if (result == MessageBoxResult.Yes)
    {
        try
        {
            _classificationRepository.DeleteClassification(classification.Id);
            Classifications.Remove(classification);
            StatusMessage = $"Race result for {classification.MemberFirstName} {classification.MemberLastName} deleted successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting race result: {ex.Message}";
            MessageBox.Show($"Error deleting race result: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
```

### 3. MainWindow XAML (`NameParser.UI\MainWindow.xaml`)

Added a delete button column to the Race Classification DataGrid:

```xml
<!-- Delete Button Column -->
<DataGridTemplateColumn Header="Delete" Width="60">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Button Content="🗑️" 
                    Command="{Binding DataContext.DeleteClassificationCommand, RelativeSource={RelativeSource AncestorType=Window}}"
                    CommandParameter="{Binding}"
                    ToolTip="Delete this race result"
                    Background="#F44336"
                    Foreground="White"
                    Padding="5,2"
                    MinWidth="40"
                    Margin="2"/>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

## Features

### 🗑️ Delete Individual Race Results
- **Location**: Race Classification tab → Race Results DataGrid → Delete column
- **Appearance**: Red trash bin icon (🗑️) button in each row
- **Behavior**:
  1. Click the delete button for any race result
  2. A confirmation dialog appears showing:
     - Participant name
     - Position
     - Time
     - Warning that action cannot be undone
  3. Click "Yes" to confirm deletion
  4. The result is immediately removed from both:
     - The database
     - The displayed grid
  5. Success message appears in the status bar

### Safety Features
1. **Confirmation Dialog**: Prevents accidental deletions
2. **Detailed Information**: Shows exactly what will be deleted
3. **Error Handling**: Displays error messages if deletion fails
4. **Status Updates**: Provides feedback on successful deletions

## Use Cases

### When to Use
1. **Incorrect Data Entry**: Remove erroneous results added by mistake
2. **Duplicate Entries**: Delete duplicate race results for the same participant
3. **Test Data Cleanup**: Remove test entries without deleting entire race
4. **Data Corrections**: Remove incorrect results before re-importing corrected data

### When NOT to Use
- To delete an entire race → Use "🗑️ Delete Selected Race" button instead
- To clear all results → Better to delete and recreate the race

## User Guide

### Step-by-Step: Delete a Race Result

1. **Navigate to Race Classification Tab**
   - Open the application
   - Click on "Race Classification" tab

2. **Select Race Event**
   - Choose the race event from the dropdown
   - The races in that event will appear

3. **View Classifications**
   - Click "View All Classifications" button
   - Race results appear in the grid below

4. **Delete a Result**
   - Find the result you want to delete
   - Click the red 🗑️ button in the "Delete" column
   - Review the confirmation dialog
   - Click "Yes" to confirm

5. **Verify Deletion**
   - The result disappears from the grid
   - Status message confirms deletion
   - The change is permanent in the database

### Visual Indicators
- **Delete Button**: Red background with white trash icon
- **Hover**: Button highlights on mouse hover
- **Tooltip**: "Delete this race result" appears on hover

## Technical Notes

### Database Impact
- Deletes a single row from the `Classifications` table
- Does not affect the parent `Race` record
- Does not cascade to other tables
- No impact on challenge classifications (these are calculated on-demand)

### UI Updates
- Uses `ObservableCollection<ClassificationEntity>` for automatic UI updates
- Removes item from collection after successful database deletion
- No page refresh required

### Error Scenarios
The feature handles these errors gracefully:
1. **Database connection issues**: Shows error dialog
2. **Classification not found**: Silently succeeds (idempotent)
3. **Foreign key constraints**: Shows error message (shouldn't occur)

## Testing Checklist

✅ Confirmation dialog appears when clicking delete  
✅ Deletion is prevented when clicking "No" in dialog  
✅ Record is removed from database when confirmed  
✅ UI updates immediately after deletion  
✅ Status message shows success/failure  
✅ Error handling works for database issues  
✅ Multiple deletions work sequentially  
✅ No errors when grid is empty  

## Related Features

- **Delete Race**: Deletes entire race including all classifications
- **Reprocess Race**: Recreates all classifications from source file
- **Export Results**: Export data before deleting if needed

## Troubleshooting

### Button Not Visible
- Ensure you've clicked "View All Classifications" first
- Check that the DataGrid has data loaded

### Deletion Fails
- Verify database connection
- Check user has write permissions to database
- Look for error messages in status bar

### Deleted Result Still Appears
- This shouldn't happen due to ObservableCollection
- If it does, click "Refresh Races" button

## Future Enhancements

Possible improvements:
1. **Undo functionality**: Add ability to restore deleted results
2. **Bulk delete**: Select multiple results for deletion
3. **Audit trail**: Log who deleted what and when
4. **Soft delete**: Mark as deleted instead of permanently removing
5. **Export before delete**: Automatically backup before deletion
