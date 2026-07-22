namespace ViscaCamLink.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Services;

public partial class CameraProfilesViewModel : INotifyPropertyChanged
{
    [GeneratedRegex(@"^(((?!25?[6-9])[12]\d|[1-9])?\d\.?\b){4}$")]
    private static partial Regex IpRegex();

    private readonly ISettingsService _settings;

    private CameraProfileItemViewModel? _selectedCamera;
    private bool _isEditing;
    private string _editName = string.Empty;
    private string _editIp = string.Empty;
    private string _editPort = string.Empty;
    private Guid _editingId = Guid.Empty;

    public CameraProfilesViewModel(ISettingsService settings, Action closeHandler)
    {
        _settings = settings;
        CloseHandler = closeHandler;

        Cameras = new ObservableCollection<CameraProfileItemViewModel>(
            _settings.CameraProfiles.Select(c => new CameraProfileItemViewModel(c)));

        AddCommand = new Command(ExecuteAdd, () => !IsEditing);
        EditCommand = new Command(ExecuteEdit, () => SelectedCamera is not null && !IsEditing);
        DeleteCommand = new Command(ExecuteDelete, () => SelectedCamera is not null && !IsEditing && Cameras.Count > 1);
        SaveEditCommand = new Command(ExecuteSaveEdit, () => IsEditNameValid && IsEditIpValid && IsEditPortValid);
        CancelEditCommand = new Command(ExecuteCancelEdit);
        CloseCommand = new Command(() => CloseHandler());

        SelectedCamera = Cameras.FirstOrDefault(c => c.Id == _settings.ActiveCameraProfileId)
                         ?? Cameras.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public bool IsEditPortValid => int.TryParse(EditPort, out var p) && p is >= 1 and <= 65535;

    public ICommand AddCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand SaveEditCommand { get; }

    public ICommand CancelEditCommand { get; }

    public ICommand CloseCommand { get; }

    private Action CloseHandler { get; }

    private void ExecuteAdd()
    {
        _editingId = Guid.Empty;
        EditName = "Camera";
        EditIp = "192.168.0.1";
        EditPort = "5678";
        IsEditing = true;
    }

    private void ExecuteEdit()
    {
        if (SelectedCamera is null) return;

        _editingId = SelectedCamera.Id;
        EditName = SelectedCamera.Name;
        EditIp = SelectedCamera.Ip;
        EditPort = SelectedCamera.Port.ToString();
        IsEditing = true;
    }

    private void ExecuteDelete()
    {
        if (SelectedCamera is null || Cameras.Count <= 1) return;

        var idToDelete = SelectedCamera.Id;
        _settings.RemoveCameraProfile(idToDelete);

        var item = Cameras.First(c => c.Id == idToDelete);
        Cameras.Remove(item);

        SelectedCamera = Cameras.FirstOrDefault(c => c.Id == _settings.ActiveCameraProfileId)
                         ?? Cameras.FirstOrDefault();

        InvalidateEditCommands();
    }

    private void ExecuteSaveEdit()
    {
        var port = int.Parse(EditPort);

        if (_editingId == Guid.Empty)
        {
            // Add new
            var profile = new CameraProfile { Name = EditName, Ip = EditIp, Port = port };
            _settings.AddCameraProfile(profile);
            var item = new CameraProfileItemViewModel(profile);
            Cameras.Add(item);
            SelectedCamera = item;
        }
        else
        {
            // Update existing
            var profile = new CameraProfile { Id = _editingId, Name = EditName, Ip = EditIp, Port = port };
            _settings.UpdateCameraProfile(profile);

            var item = Cameras.First(c => c.Id == _editingId);
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

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class CameraProfileItemViewModel : INotifyPropertyChanged
{
    private string _name;
    private string _ip;
    private int _port;

    public CameraProfileItemViewModel(CameraProfile profile)
    {
        Id = profile.Id;
        _name = profile.Name;
        _ip = profile.Ip;
        _port = profile.Port;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; }

    public string Name
    {
        get => _name;
        private set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
    }

    public string Ip
    {
        get => _ip;
        private set { _ip = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Ip))); }
    }

    public int Port
    {
        get => _port;
        private set { _port = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Port))); }
    }

    public void Update(CameraProfile profile)
    {
        Name = profile.Name;
        Ip = profile.Ip;
        Port = profile.Port;
    }
}