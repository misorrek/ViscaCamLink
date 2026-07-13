namespace ViscaCamLink.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Repositories.HotKeys;
using ViscaCamLink.Resources;
using ViscaCamLink.Services;
using ViscaCamLink.Visca.Types;

public class PresetsViewModel : ViewModelBase
{
    private readonly IPresetService _presetService;
    private readonly ISettingsService _settings;
    private readonly IHotKeyService _hotKeyService;
    private readonly ConnectionViewModel _connection;

    private bool _isSettingMemory;
    private string _memoryInfo = string.Empty;
    private int _renamingSlotIndex = -1;
    private string _renamingText = string.Empty;
    private bool _useNumpadLayout;
    private bool _usePresetGroups;
    private string _renamingGroupText = string.Empty;
    private int _lastRecalledPresetSlot = -1;
    private PresetIndicatorState _presetIndicatorState = PresetIndicatorState.None;

    public PresetsViewModel(
        IPresetService presetService,
        ISettingsService settings,
        IHotKeyService hotKeyService,
        ConnectionViewModel connection)
    {
        _presetService = presetService;
        _settings = settings;
        _hotKeyService = hotKeyService;
        _connection = connection;

        _useNumpadLayout = _settings.UseNumpadLayout;
        _usePresetGroups = _settings.UsePresetGroups;

        _presetService.PresetsChanged += OnPresetsChanged;
        _presetService.GroupsChanged += OnGroupsChanged;
        _connection.PropertyChanged += OnConnectionPropertyChanged;

        Presets = new ObservableCollection<PresetItemViewModel>(
            _presetService.Presets.Select(p => new PresetItemViewModel(p.SlotIndex, p.Name)));
        GridPresets = BuildGridPresets();
        PresetGroups = BuildPresetGroups();

        MemoryRenameCommand = new Command(ExecuteMemoryRename);
        MemoryRenameConfirmCommand = new Command(ExecuteMemoryRenameConfirm);
        MemoryRenameCancelCommand = new Command(ExecuteMemoryRenameCancel);
        MemorySetCommand = new Command(ExecuteMemorySet);
        MemoryCommand = new Command(ExecuteMemorySetOrRecall);
        GroupSwitchCommand = new Command(ExecuteGroupSwitch);
        GroupAddCommand = new Command(ExecuteGroupAdd);
        GroupRemoveCommand = new Command(ExecuteGroupRemove);
        GroupRenameCommand = new Command(ExecuteGroupRename);
        GroupRenameConfirmCommand = new Command(ExecuteGroupRenameConfirm);
        GroupRenameCancelCommand = new Command(ExecuteGroupRenameCancel);

        _hotKeyService.RegisterActions(CreateHotKeyActions());
    }

    public ICommand MemoryRenameCommand { get; }

    public ICommand MemoryRenameConfirmCommand { get; }

    public ICommand MemoryRenameCancelCommand { get; }

    public ICommand MemorySetCommand { get; }

    public ICommand MemoryCommand { get; }

    public ICommand GroupSwitchCommand { get; }

    public ICommand GroupAddCommand { get; }

    public ICommand GroupRemoveCommand { get; }

    public ICommand GroupRenameCommand { get; }

    public ICommand GroupRenameConfirmCommand { get; }

    public ICommand GroupRenameCancelCommand { get; }

    public bool IsSettingMemory
    {
        get => _isSettingMemory;
        set
        {
            _isSettingMemory = value;
            NotifyPropertyChanged();
        }
    }

    public string MemoryInfo
    {
        get => _memoryInfo;
        set
        {
            _memoryInfo = value;
            IsMemoryInfoVisible = !string.IsNullOrEmpty(value);
            NotifyPropertyChanged();
        }
    }

    public bool IsMemoryInfoVisible
    {
        get => _isMemoryInfoVisible;
        private set
        {
            _isMemoryInfoVisible = value;
            NotifyPropertyChanged();
        }
    }

    private bool _isMemoryInfoVisible;

    public ObservableCollection<PresetItemViewModel> Presets { get; }

    public ObservableCollection<PresetItemViewModel> GridPresets { get; private set; }

    public int RenamingSlotIndex
    {
        get => _renamingSlotIndex;
        set
        {
            _renamingSlotIndex = value;
            NotifyPropertyChanged();
        }
    }

    public string RenamingText
    {
        get => _renamingText;
        set
        {
            _renamingText = value;
            NotifyPropertyChanged();
        }
    }

    public bool UseNumpadLayout
    {
        get => _useNumpadLayout;
        set
        {
            if (_useNumpadLayout == value)
            {
                return;
            }

            _useNumpadLayout = value;
            _settings.UseNumpadLayout = value;
            _settings.Save();
            GridPresets = BuildGridPresets();
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(GridPresets));
        }
    }

    public ObservableCollection<PresetGroupViewModel> PresetGroups { get; private set; }

    public bool UsePresetGroups
    {
        get => _usePresetGroups;
        set
        {
            if (_usePresetGroups == value) return;
            _usePresetGroups = value;
            _settings.UsePresetGroups = value;
            NotifyPropertyChanged();
        }
    }

    public bool HasMultipleGroups => PresetGroups.Count > 1;

    public string RenamingGroupText
    {
        get => _renamingGroupText;
        set
        {
            _renamingGroupText = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The slot index (0-255) of the last recalled preset, or -1 when no preset is active.
    /// Drives the indicator dot visibility on the matching preset button.
    /// </summary>
    public int LastRecalledPresetSlot
    {
        get => _lastRecalledPresetSlot;
        private set
        {
            _lastRecalledPresetSlot = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Visual state of the last-recalled preset indicator dot.
    /// Green (Active) = camera is at the preset; Yellow (Moved) = camera has moved since.
    /// </summary>
    public PresetIndicatorState PresetIndicatorState
    {
        get => _presetIndicatorState;
        private set
        {
            _presetIndicatorState = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Called when the camera starts moving or zooming. Transitions the indicator from
    /// Active (green) to Moved (yellow) to signal the camera is no longer at the preset.
    /// </summary>
    public void NotifyMovementStarted()
    {
        if (PresetIndicatorState == PresetIndicatorState.Active)
        {
            PresetIndicatorState = PresetIndicatorState.Moved;
        }
    }

    /// <summary>
    /// Called when the home button is pressed. Clears the indicator entirely because
    /// the camera moves to a known home position unrelated to any preset.
    /// </summary>
    public void ClearPresetIndicator()
    {
        LastRecalledPresetSlot = -1;
        PresetIndicatorState = PresetIndicatorState.None;
    }

    private CancellationTokenSource? MemoryInfoCancellationTokenSource { get; set; }

    public void OnLanguageChanged()
    {
        if (IsSettingMemory)
        {
            MemoryInfo = Strings.Presets_ChooseSlot;
        }
    }

    public void CancelEditMode()
    {
        if (IsSettingMemory)
        {
            MemoryInfoCancellationTokenSource?.Cancel();
            IsSettingMemory = false;
            MemoryInfo = string.Empty;
        }

        if (RenamingSlotIndex >= 0)
        {
            RenamingSlotIndex = -1;
            RenamingText = string.Empty;
        }

        ExecuteGroupRenameCancel();
    }

    public void RefreshLayout()
    {
        if (_useNumpadLayout != _settings.UseNumpadLayout)
        {
            UseNumpadLayout = _settings.UseNumpadLayout;
        }

        if (_usePresetGroups != _settings.UsePresetGroups)
        {
            UsePresetGroups = _settings.UsePresetGroups;
        }
    }

    /// <summary>
    /// Clears the indicator when the camera disconnects or loses power,
    /// because the last-recalled preset position can no longer be trusted.
    /// </summary>
    private void OnConnectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ConnectionViewModel.ConnectionStatus) or nameof(ConnectionViewModel.PowerStatus))
        {
            var isConnectedAndOn = _connection.ConnectionStatus == ConnectionStatus.Ok
                                   && _connection.PowerStatus == PowerStatus.On;

            if (!isConnectedAndOn)
            {
                ClearPresetIndicator();
            }
        }
    }

    private void OnPresetsChanged()
    {
        Presets.Clear();
        foreach (var preset in _presetService.Presets)
        {
            Presets.Add(new PresetItemViewModel(preset.SlotIndex, preset.Name));
        }

        GridPresets = BuildGridPresets();
        NotifyPropertyChanged(nameof(GridPresets));
    }

    private void OnGroupsChanged()
    {
        PresetGroups = BuildPresetGroups();
        NotifyPropertyChanged(nameof(PresetGroups));
        NotifyPropertyChanged(nameof(HasMultipleGroups));
    }

    private ObservableCollection<PresetGroupViewModel> BuildPresetGroups()
    {
        return new ObservableCollection<PresetGroupViewModel>(
            _presetService.Groups.Select(g =>
                new PresetGroupViewModel(g.Id, g.Name, g.Id == _presetService.ActiveGroupId)));
    }

    private void ExecuteGroupSwitch(object? parameter)
    {
        if (parameter is string groupId)
        {
            _presetService.SwitchGroup(groupId);
            foreach (var group in PresetGroups)
            {
                group.IsActive = group.Id == groupId;
            }
        }
    }

    private void ExecuteGroupAdd()
    {
        var groupNumber = _presetService.Groups.Count + 1;
        _presetService.AddGroup($"Group {groupNumber}");

        var newGroup = _presetService.Groups[^1];
        _presetService.SwitchGroup(newGroup.Id);
        foreach (var group in PresetGroups)
        {
            group.IsActive = group.Id == newGroup.Id;
        }
    }

    private void ExecuteGroupRemove(object? parameter)
    {
        var groupId = parameter as string ?? _presetService.ActiveGroupId;
        _presetService.RemoveGroup(groupId);
    }

    private void ExecuteGroupRename(object? parameter)
    {
        if (parameter is string groupId)
        {
            var group = PresetGroups.FirstOrDefault(g => g.Id == groupId);
            if (group is null)
            {
                return;
            }

            RenamingGroupText = group.Name;
            group.IsRenaming = true;
        }
    }

    private void ExecuteGroupRenameConfirm()
    {
        var group = PresetGroups.FirstOrDefault(g => g.IsRenaming);
        if (group is null)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(RenamingGroupText)
            ? group.Name
            : RenamingGroupText.Trim();

        _presetService.RenameGroup(group.Id, name);
        group.Name = name;
        group.IsRenaming = false;
        RenamingGroupText = string.Empty;
    }

    private void ExecuteGroupRenameCancel()
    {
        var group = PresetGroups.FirstOrDefault(g => g.IsRenaming);
        if (group is not null)
        {
            group.IsRenaming = false;
        }

        RenamingGroupText = string.Empty;
    }

    private ObservableCollection<PresetItemViewModel> BuildGridPresets()
    {
        if (Presets.Count < 10)
        {
            return new ObservableCollection<PresetItemViewModel>(Presets.Skip(1));
        }

        var positions = _useNumpadLayout
            ? new[] { 7, 8, 9, 4, 5, 6, 1, 2, 3 }
            : new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        return new ObservableCollection<PresetItemViewModel>(
            positions.Select(i => Presets[i]));
    }

    private async void ExecuteMemorySet()
    {
        IsSettingMemory = !IsSettingMemory;

        if (IsSettingMemory)
        {
            MemoryInfo = Strings.Presets_ChooseSlot;
            MemoryInfoCancellationTokenSource?.Cancel();
        }
        else
        {
            MemoryInfo = Strings.Common_Cancel;
            await ResetMemorySetInfo();
        }
    }

    private async void ExecuteMemorySetOrRecall(object? parameter)
    {
        if (parameter is int intSlot)
        {
            await ExecuteMemorySetOrRecallCore((byte)intSlot);
        }
        else if (parameter is string stringedParameter && byte.TryParse(stringedParameter, out var slot))
        {
            await ExecuteMemorySetOrRecallCore(slot);
        }
    }

    private async void ExecuteMemorySetOrRecall(byte slot)
    {
        await ExecuteMemorySetOrRecallCore(slot);
    }

    private void ExecuteMemoryRename(object? parameter)
    {
        int slotIndex;
        if (parameter is int intSlot)
        {
            slotIndex = intSlot;
        }
        else if (parameter is string slot && int.TryParse(slot, out var parsed))
        {
            slotIndex = parsed;
        }
        else
        {
            return;
        }

        RenamingText = _presetService.GetPresetName(slotIndex);
        RenamingSlotIndex = slotIndex;
    }

    private void ExecuteMemoryRenameConfirm()
    {
        if (RenamingSlotIndex < 0)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(RenamingText)
            ? RenamingSlotIndex.ToString()
            : RenamingText.Trim();

        _presetService.RenamePreset(RenamingSlotIndex, name);
        RenamingSlotIndex = -1;
        RenamingText = string.Empty;
    }

    private void ExecuteMemoryRenameCancel()
    {
        RenamingSlotIndex = -1;
        RenamingText = string.Empty;
    }

    private IEnumerable<HotKeyActionRegistration> CreateHotKeyActions()
    {
        foreach (var action in HotKeyDefinitions.PresetActions)
        {
            yield return new HotKeyActionRegistration(action, () => ExecutePresetHotKey(action));
        }

        yield return new HotKeyActionRegistration(HotKeyAction.PresetGroupPrevious, () =>
        {
            if (PresetGroups.Count <= 1)
            {
                return;
            }

            var currentIndex = PresetGroups.ToList().FindIndex(g => g.IsActive);
            if (currentIndex < 0)
            {
                return;
            }

            var previousIndex = (currentIndex - 1 + PresetGroups.Count) % PresetGroups.Count;
            ExecuteGroupSwitch(PresetGroups[previousIndex].Id);
        });
        yield return new HotKeyActionRegistration(HotKeyAction.PresetGroupNext, () =>
        {
            if (PresetGroups.Count <= 1)
            {
                return;
            }

            var currentIndex = PresetGroups.ToList().FindIndex(g => g.IsActive);
            if (currentIndex < 0)
            {
                return;
            }

            var nextIndex = (currentIndex + 1) % PresetGroups.Count;
            ExecuteGroupSwitch(PresetGroups[nextIndex].Id);
        });
    }

    private void ExecutePresetHotKey(HotKeyAction action)
    {
        if (!HotKeyDefinitions.TryGetPresetPosition(action, out var presetPosition) ||
            presetPosition >= _presetService.Presets.Count)
        {
            return;
        }

        ExecuteMemorySetOrRecall((byte)_presetService.Presets[presetPosition].SlotIndex);
    }

    private async Task ExecuteMemorySetOrRecallCore(byte slot)
    {
        if (IsSettingMemory)
        {
            await TryCameraOperation(_presetService.SetMemoryAsync(slot));
            IsSettingMemory = false;
            MemoryInfo = Strings.Common_Saved;
            await ResetMemorySetInfo();
        }
        else
        {
            // Update the indicator immediately on the UI thread before awaiting the camera.
            // This also avoids cross-thread PropertyChanged issues (the await may resume on
            // a thread-pool thread depending on how RecallMemoryAsync is implemented).
            LastRecalledPresetSlot = slot;
            PresetIndicatorState = PresetIndicatorState.Active;
            await TryCameraOperation(_presetService.RecallMemoryAsync(slot));
        }
    }

    private async Task ResetMemorySetInfo()
    {
        MemoryInfoCancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Delay(5000, MemoryInfoCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        MemoryInfo = string.Empty;
    }
}
