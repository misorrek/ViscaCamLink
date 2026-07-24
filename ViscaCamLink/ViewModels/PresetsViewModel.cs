namespace ViscaCamLink.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Repositories.HotKeys;
using ViscaCamLink.Repositories.Presets;
using ViscaCamLink.Resources;
using ViscaCamLink.Services;
using ViscaCamLink.Visca.Types;

public class PresetsViewModel : ViewModelBase
{
    private static readonly TimeSpan MemoryInfoDisplayDuration = TimeSpan.FromSeconds(5);

    // Grid cell order (left to right, top to bottom) mapped to preset indexes 1-9.
    private static readonly int[] NumpadLayoutPositions = [7, 8, 9, 4, 5, 6, 1, 2, 3];
    private static readonly int[] SequentialLayoutPositions = [1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly IPresetService _presetService;
    private readonly ISettingsService _settings;
    private readonly IHotKeyService _hotKeyService;
    private readonly ConnectionViewModel _connection;

    private bool _isSettingMemory;
    private string _memoryInfo = string.Empty;
    private bool _isMemoryInfoVisible;
    private int _renamingSlotIndex = -1;
    private string _renamingText = string.Empty;
    private bool _useNumpadLayout;
    private bool _usePresetGroups;
    private string _renamingGroupText = string.Empty;
    private int _lastRecalledPresetSlot = -1;
    private PresetIndicatorState _presetIndicatorState = PresetIndicatorState.None;
    private CancellationTokenSource? _memoryInfoCancellation;

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
            _presetService.Presets.Select(preset => new PresetItemViewModel(preset.SlotIndex, preset.Name)));
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

    public ObservableCollection<PresetItemViewModel> Presets { get; }

    public ObservableCollection<PresetItemViewModel> GridPresets { get; private set; }

    public ObservableCollection<PresetGroupViewModel> PresetGroups { get; private set; }

    public bool HasMultipleGroups => PresetGroups.Count > 1;

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
            GridPresets = BuildGridPresets();

            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(GridPresets));
        }
    }

    public bool UsePresetGroups
    {
        get => _usePresetGroups;
        set
        {
            if (_usePresetGroups == value)
            {
                return;
            }

            _usePresetGroups = value;

            NotifyPropertyChanged();
        }
    }

    public string RenamingGroupText
    {
        get => _renamingGroupText;
        set
        {
            _renamingGroupText = value;

            NotifyPropertyChanged();
        }
    }

    public int LastRecalledPresetSlot
    {
        get => _lastRecalledPresetSlot;
        private set
        {
            _lastRecalledPresetSlot = value;

            NotifyPropertyChanged();
        }
    }

    public PresetIndicatorState PresetIndicatorState
    {
        get => _presetIndicatorState;
        private set
        {
            _presetIndicatorState = value;

            NotifyPropertyChanged();
        }
    }

    public void NotifyMovementStarted()
    {
        if (PresetIndicatorState == PresetIndicatorState.Active)
        {
            PresetIndicatorState = PresetIndicatorState.Moved;
        }
    }

    public void ClearPresetIndicator()
    {
        LastRecalledPresetSlot = -1;
        PresetIndicatorState = PresetIndicatorState.None;
    }

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
            _memoryInfoCancellation?.Cancel();
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

    private void OnConnectionPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is nameof(ConnectionViewModel.ConnectionStatus) or nameof(ConnectionViewModel.PowerStatus))
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
            _presetService.Groups.Select(group =>
                new PresetGroupViewModel(group.Id, group.Name, group.Id == _presetService.ActiveGroupId)));
    }

    private ObservableCollection<PresetItemViewModel> BuildGridPresets()
    {
        // Preset 0 has its own dedicated button; the grid shows presets 1-9.
        if (Presets.Count < PresetLayout.PresetsPerGroup)
        {
            return new ObservableCollection<PresetItemViewModel>(Presets.Skip(1));
        }

        var positions = _useNumpadLayout ? NumpadLayoutPositions : SequentialLayoutPositions;

        return new ObservableCollection<PresetItemViewModel>(
            positions.Select(position => Presets[position]));
    }

    private void ExecuteGroupSwitch(object? parameter)
    {
        if (parameter is not Guid groupId)
        {
            return;
        }

        _presetService.SwitchGroup(groupId);

        foreach (var group in PresetGroups)
        {
            group.IsActive = group.Id == groupId;
        }
    }

    private void ExecuteGroupAdd()
    {
        var groupNumber = _presetService.Groups.Count + 1;

        _presetService.AddGroup(string.Format(Strings.PresetGroup_NewName, groupNumber));

        var newGroup = _presetService.Groups[^1];

        _presetService.SwitchGroup(newGroup.Id);

        foreach (var group in PresetGroups)
        {
            group.IsActive = group.Id == newGroup.Id;
        }
    }

    private void ExecuteGroupRemove(object? parameter)
    {
        var groupId = parameter as Guid? ?? _presetService.ActiveGroupId;

        _presetService.RemoveGroup(groupId);
    }

    private void ExecuteGroupRename(object? parameter)
    {
        if (parameter is not Guid groupId)
        {
            return;
        }

        var group = PresetGroups.FirstOrDefault(candidate => candidate.Id == groupId);

        if (group is null)
        {
            return;
        }

        RenamingGroupText = group.Name;
        group.IsRenaming = true;
    }

    private void ExecuteGroupRenameConfirm()
    {
        var group = PresetGroups.FirstOrDefault(candidate => candidate.IsRenaming);

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
        var group = PresetGroups.FirstOrDefault(candidate => candidate.IsRenaming);
        group?.IsRenaming = false;
        RenamingGroupText = string.Empty;
    }

    private async void ExecuteMemorySet()
    {
        IsSettingMemory = !IsSettingMemory;

        if (IsSettingMemory)
        {
            MemoryInfo = Strings.Presets_ChooseSlot;
            _memoryInfoCancellation?.Cancel();
        }
        else
        {
            MemoryInfo = Strings.Common_Cancel;
            await ResetMemorySetInfoAsync();
        }
    }

    private async void ExecuteMemorySetOrRecall(object? parameter)
    {
        if (parameter is int slotNumber)
        {
            await MemorySetOrRecallAsync((byte)slotNumber);
        }
        else if (parameter is string slotText && byte.TryParse(slotText, out var slot))
        {
            await MemorySetOrRecallAsync(slot);
        }
    }

    private void ExecuteMemoryRename(object? parameter)
    {
        int slotIndex;

        if (parameter is int slotNumber)
        {
            slotIndex = slotNumber;
        }
        else if (parameter is string slotText && int.TryParse(slotText, out var parsedSlot))
        {
            slotIndex = parsedSlot;
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

        yield return new HotKeyActionRegistration(HotKeyAction.PresetGroupPrevious, () => SwitchToAdjacentGroup(-1));
        yield return new HotKeyActionRegistration(HotKeyAction.PresetGroupNext, () => SwitchToAdjacentGroup(+1));
    }

    private void SwitchToAdjacentGroup(int direction)
    {
        if (PresetGroups.Count <= 1)
        {
            return;
        }

        var currentIndex = PresetGroups.ToList().FindIndex(group => group.IsActive);

        if (currentIndex < 0)
        {
            return;
        }

        var targetIndex = (currentIndex + direction + PresetGroups.Count) % PresetGroups.Count;

        ExecuteGroupSwitch(PresetGroups[targetIndex].Id);
    }

    private async void ExecutePresetHotKey(HotKeyAction action)
    {
        if (!HotKeyDefinitions.TryGetPresetPosition(action, out var presetPosition) ||
            presetPosition >= _presetService.Presets.Count)
        {
            return;
        }

        await MemorySetOrRecallAsync((byte)_presetService.Presets[presetPosition].SlotIndex);
    }

    private async Task MemorySetOrRecallAsync(byte slot)
    {
        if (IsSettingMemory)
        {
            await TryCameraOperation(_presetService.SetMemoryAsync(slot));

            IsSettingMemory = false;
            MemoryInfo = Strings.Common_Saved;

            await ResetMemorySetInfoAsync();
        }
        else
        {
            // Update the indicator immediately on the UI thread before awaiting the camera.
            LastRecalledPresetSlot = slot;
            PresetIndicatorState = PresetIndicatorState.Active;

            await TryCameraOperation(_presetService.RecallMemoryAsync(slot));
        }
    }

    private async Task ResetMemorySetInfoAsync()
    {
        _memoryInfoCancellation = new CancellationTokenSource();

        try
        {
            await Task.Delay(MemoryInfoDisplayDuration, _memoryInfoCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        MemoryInfo = string.Empty;
    }
}
