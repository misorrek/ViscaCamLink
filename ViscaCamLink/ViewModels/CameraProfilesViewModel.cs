namespace ViscaCamLink.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Resources;
using ViscaCamLink.Services;

public partial class CameraProfilesViewModel : ViewModelBase
{
    private const int MinimumPort = 1;
    private const int MaximumPort = 65535;

    private readonly ISettingsService _settings;
    private readonly Action _closeHandler;

    private CameraProfileItemViewModel? _selectedCamera;
    private bool _isEditing;
    private string _editName = string.Empty;
    private string _editIp = string.Empty;
    private string _editPort = string.Empty;
    private Guid _editingId = Guid.Empty;

    public CameraProfilesViewModel(ISettingsService settings, Action closeHandler)
    {
        _settings = settings;
        _closeHandler = closeHandler;

        Cameras = new ObservableCollection<CameraProfileItemViewModel>(
            _settings.CameraProfiles.Select(profile => new CameraProfileItemViewModel(profile)));

        AddCommand = new Command(ExecuteAdd, () => !IsEditing);
        EditCommand = new Command(ExecuteEdit, () => SelectedCamera is not null && !IsEditing);
        DeleteCommand = new Command(ExecuteDelete, () => SelectedCamera is not null && !IsEditing && Cameras.Count > 1);
        SaveEditCommand = new Command(ExecuteSaveEdit, () => IsEditNameValid && IsEditIpValid && IsEditPortValid);
        CancelEditCommand = new Command(ExecuteCancelEdit);
        CloseCommand = new Command(() => _closeHandler());

        SelectedCamera = Cameras.FirstOrDefault(camera => camera.Id == _settings.ActiveCameraProfileId)
                         ?? Cameras.FirstOrDefault();
    }

    public ICommand AddCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand SaveEditCommand { get; }

    public ICommand CancelEditCommand { get; }

    public ICommand CloseCommand { get; }

    public ObservableCollection<CameraProfileItemViewModel> Cameras { get; }

    public CameraProfileItemViewModel? SelectedCamera
    {
        get => _selectedCamera;
        set
        {
            _selectedCamera = value;

            NotifyPropertyChanged();
            InvalidateEditCommands();
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            _isEditing = value;

            NotifyPropertyChanged();
            InvalidateEditCommands();
        }
    }

    public string EditName
    {
        get => _editName;
        set
        {
            _editName = value;

            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(IsEditNameValid));
            ((Command)SaveEditCommand).Invalidate();
        }
    }

    public string EditIp
    {
        get => _editIp;
        set
        {
            _editIp = value;

            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(IsEditIpValid));
            ((Command)SaveEditCommand).Invalidate();
        }
    }

    public string EditPort
    {
        get => _editPort;
        set
        {
            _editPort = value;

            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(IsEditPortValid));
            ((Command)SaveEditCommand).Invalidate();
        }
    }

    public bool IsEditNameValid => !string.IsNullOrWhiteSpace(EditName);

    public bool IsEditIpValid => !string.IsNullOrWhiteSpace(EditIp) && IpRegex().IsMatch(EditIp);

    public bool IsEditPortValid => int.TryParse(EditPort, out var port) && port is >= MinimumPort and <= MaximumPort;

    private void ExecuteAdd()
    {
        _editingId = Guid.Empty;
        EditName = string.Format(Strings.CameraProfile_DefaultName, Cameras.Count + 1);
        EditIp = CameraProfile.DefaultIp;
        EditPort = CameraProfile.DefaultPort.ToString();
        IsEditing = true;
    }

    private void ExecuteEdit()
    {
        if (SelectedCamera is null)
        {
            return;
        }

        _editingId = SelectedCamera.Id;
        EditName = SelectedCamera.Name;
        EditIp = SelectedCamera.Ip;
        EditPort = SelectedCamera.Port.ToString();
        IsEditing = true;
    }

    private void ExecuteDelete()
    {
        if (SelectedCamera is null || Cameras.Count <= 1)
        {
            return;
        }

        var idToDelete = SelectedCamera.Id;

        _settings.RemoveCameraProfile(idToDelete);

        var item = Cameras.First(camera => camera.Id == idToDelete);

        Cameras.Remove(item);

        SelectedCamera = Cameras.FirstOrDefault(camera => camera.Id == _settings.ActiveCameraProfileId)
                         ?? Cameras.FirstOrDefault();

        InvalidateEditCommands();
    }

    private void ExecuteSaveEdit()
    {
        var port = int.Parse(EditPort);

        if (_editingId == Guid.Empty)
        {
            var profile = new CameraProfile { Name = EditName, Ip = EditIp, Port = port };

            _settings.AddCameraProfile(profile);

            var item = new CameraProfileItemViewModel(profile);

            Cameras.Add(item);
            SelectedCamera = item;
        }
        else
        {
            var profile = new CameraProfile { Id = _editingId, Name = EditName, Ip = EditIp, Port = port };

            _settings.UpdateCameraProfile(profile);

            var item = Cameras.First(camera => camera.Id == _editingId);

            item.Update(profile);

            if (SelectedCamera?.Id == _editingId)
            {
                SelectedCamera = item;
            }
        }

        IsEditing = false;
    }

    private void ExecuteCancelEdit()
    {
        IsEditing = false;
    }

    private void InvalidateEditCommands()
    {
        ((Command)AddCommand).Invalidate();
        ((Command)EditCommand).Invalidate();
        ((Command)DeleteCommand).Invalidate();
    }

    [GeneratedRegex(@"^(((?!25?[6-9])[12]\d|[1-9])?\d\.?\b){4}$")]
    private static partial Regex IpRegex();
}
