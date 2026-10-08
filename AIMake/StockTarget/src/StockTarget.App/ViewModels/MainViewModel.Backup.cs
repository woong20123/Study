using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using StockTarget.App.Services;
using StockTarget.Core;

namespace StockTarget.App.ViewModels;

/// <summary>JSON 백업 · 복원과 Firebase 동기화(Google 로그인).</summary>
public sealed partial class MainViewModel
{
    private readonly CloudSessionStore _sessionStore = new(CloudSessionStore.DefaultPath);
    private FirebaseSession? _session;
    private string _cloudStatus = "";

    public AsyncCommand ExportJsonCommand { get; private set; } = null!;
    public AsyncCommand ImportJsonCommand { get; private set; } = null!;
    public AsyncCommand CloudLoginCommand { get; private set; } = null!;
    public AsyncCommand CloudSaveCommand { get; private set; } = null!;
    public AsyncCommand CloudRestoreCommand { get; private set; } = null!;
    public RelayCommand CloudLogoutCommand { get; private set; } = null!;

    public string CloudStatus { get => _cloudStatus; set => Set(ref _cloudStatus, value); }

    /// <summary>자동 백업(복원 직전 데이터) 폴더.</summary>
    public static string BackupFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StockTarget", "backups");

    private void InitBackupCommands()
    {
        ExportJsonCommand = new AsyncCommand(ExportJsonAsync, () => !IsBusy);
        ImportJsonCommand = new AsyncCommand(ImportJsonAsync, () => !IsBusy);
        CloudLoginCommand = new AsyncCommand(CloudLoginAsync, () => !IsBusy);
        CloudSaveCommand = new AsyncCommand(CloudSaveAsync, () => !IsBusy);
        CloudRestoreCommand = new AsyncCommand(CloudRestoreAsync, () => !IsBusy);
        CloudLogoutCommand = new RelayCommand(CloudLogout, () => _session is not null);
        _session = _sessionStore.Load();
        UpdateCloudStatus();
    }

    // ------------------------------------------------------------- JSON 파일
    private Task ExportJsonAsync()
    {
        var dlg = new SaveFileDialog
        {
            Title = "JSON 백업 내보내기",
            Filter = "StockTarget 백업 (*.json)|*.json",
            FileName = $"stocktarget-{DateTime.Now:yyyyMMdd-HHmm}.json",
        };
        if (dlg.ShowDialog() != true)
            return Task.CompletedTask;
        var backup = _db.ExportBackup();
        File.WriteAllText(dlg.FileName, BackupSerializer.ToJson(backup));
        Status = $"JSON 내보내기: {backup.Summary} → {dlg.FileName}";
        return Task.CompletedTask;
    }

    private async Task ImportJsonAsync()
    {
        var dlg = new OpenFileDialog { Title = "JSON 백업에서 복원", Filter = "StockTarget 백업 (*.json)|*.json|모든 파일|*.*" };
        if (dlg.ShowDialog() != true)
            return;
        try
        {
            var backup = BackupSerializer.FromJson(await File.ReadAllTextAsync(dlg.FileName));
            await RestoreAsync(backup, $"파일 {Path.GetFileName(dlg.FileName)}");
        }
        catch (Exception e) when (e is BackupFormatException or IOException)
        {
            ShowError("복원 실패", e.Message);
        }
    }

    /// <summary>현재 데이터를 자동 백업한 뒤 백업 내용으로 통째로 바꾼다.</summary>
    private async Task RestoreAsync(BackupData backup, string source)
    {
        // 값 검사(잘못된 백업이면 여기서 예외, DB는 그대로)
        backup.ToTargets();
        backup.ToDefaultAmounts();
        var current = _db.ExportBackup();
        var answer = MessageBox.Show(
            $"{source}의 백업으로 현재 데이터를 바꿉니다.\n\n" +
            $"백업({backup.ExportedAt.ToLocalTime():yyyy-MM-dd HH:mm}): {backup.Summary}\n" +
            $"현재: {current.Summary}\n\n" +
            $"현재 데이터는 먼저 {BackupFolder} 에 자동 백업합니다. 계속할까요?",
            "복원 확인", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
            return;

        Directory.CreateDirectory(BackupFolder);
        var autoPath = Path.Combine(BackupFolder, $"before-restore-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        await File.WriteAllTextAsync(autoPath, BackupSerializer.ToJson(current));

        _db.ReplaceWithBackup(backup);
        Selected = null;
        ClearDetail();
        await LoadAsync();
        Status = $"복원 완료({source}): {backup.Summary} · 이전 데이터 → {autoPath}";
    }

    // ------------------------------------------------------------- Firebase
    private async Task CloudLoginAsync()
    {
        if (LoadFirebaseConfig() is not { } config)
            return;
        IsBusy = true;
        try
        {
            Status = "브라우저에서 Google 계정으로 로그인하세요...";
            using var firebase = new FirebaseClient(config);
            var googleIdToken = await GoogleLoopbackSignIn.GetGoogleIdTokenAsync(config, firebase.Http);
            _session = await firebase.SignInWithGoogleAsync(googleIdToken);
            _sessionStore.Save(_session);
            UpdateCloudStatus();
            Status = $"Google 로그인: {_session.Email} (Firebase uid {_session.Uid})";
        }
        catch (Exception e) when (e is CloudException or HttpRequestException or TaskCanceledException
                                      or System.Net.HttpListenerException)
        {
            ShowError("Google 로그인 실패", e.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CloudSaveAsync()
    {
        if (await GetCloudAsync() is not { } cloud)
            return;
        var (firebase, session) = cloud;
        using (firebase)
        {
            IsBusy = true;
            try
            {
                var remote = await firebase.LoadBackupAsync(session);
                var local = _db.ExportBackup();
                var message = $"Firebase({session.Email})에 현재 데이터를 저장합니다.\n\n현재: {local.Summary}\n" +
                              (remote is null
                                  ? "Firebase: 저장된 데이터 없음"
                                  : $"Firebase({remote.ExportedAt.ToLocalTime():yyyy-MM-dd HH:mm}): {remote.Summary}  ← 덮어씀") +
                              "\n\n계속할까요?";
                if (MessageBox.Show(message, "Firebase 저장", MessageBoxButton.YesNo, MessageBoxImage.Question,
                        MessageBoxResult.No) != MessageBoxResult.Yes)
                    return;
                await firebase.SaveBackupAsync(session, local);
                Status = $"Firebase 저장 완료: {local.Summary}";
            }
            catch (Exception e) when (e is CloudException or BackupFormatException or HttpRequestException or TaskCanceledException)
            {
                ShowError("Firebase 저장 실패", e.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private async Task CloudRestoreAsync()
    {
        if (await GetCloudAsync() is not { } cloud)
            return;
        var (firebase, session) = cloud;
        using (firebase)
        {
            try
            {
                IsBusy = true;
                var remote = await firebase.LoadBackupAsync(session);
                IsBusy = false;
                if (remote is null)
                {
                    MessageBox.Show("Firebase에 저장된 데이터가 없습니다.", "Firebase 복원", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                await RestoreAsync(remote, $"Firebase({session.Email})");
            }
            catch (Exception e) when (e is CloudException or BackupFormatException or HttpRequestException or TaskCanceledException)
            {
                ShowError("Firebase 복원 실패", e.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private void CloudLogout()
    {
        _session = null;
        _sessionStore.Clear();
        UpdateCloudStatus();
        Status = "Google 로그아웃(이 PC의 로그인 정보 삭제)";
    }

    /// <summary>설정 · 로그인 확인 후 토큰을 갱신한 클라이언트. 준비가 안 되면 안내하고 null.</summary>
    private async Task<(FirebaseClient, FirebaseSession)?> GetCloudAsync()
    {
        if (LoadFirebaseConfig() is not { } config)
            return null;
        if (_session is null)
        {
            await CloudLoginAsync();
            if (_session is null)
                return null;
        }
        var firebase = new FirebaseClient(config);
        try
        {
            _session = await firebase.EnsureFreshAsync(_session);
            _sessionStore.Save(_session);
            return (firebase, _session);
        }
        catch (Exception e) when (e is CloudException or HttpRequestException or TaskCanceledException)
        {
            firebase.Dispose();
            ShowError("Firebase 인증 실패", e.Message + "\n\n다시 로그인하세요.");
            CloudLogout();
            return null;
        }
    }

    private FirebaseConfig? LoadFirebaseConfig()
    {
        var path = FirebaseConfig.DefaultPath;
        if (FirebaseConfig.Load(path) is { } config)
            return config;
        MessageBox.Show(
            "Firebase 설정이 비어 있습니다. 아래 파일에 값을 채운 뒤 다시 시도하세요.\n\n" + path +
            "\n\n• ApiKey: Firebase 프로젝트 설정 → 웹 API 키\n• DatabaseUrl: Realtime Database 주소\n" +
            "• GoogleClientId / GoogleClientSecret: Google Cloud 콘솔 → OAuth 클라이언트(데스크톱 앱)\n\n" +
            "자세한 절차는 docs/progress.html '11. 백업 · Firebase 동기화'를 보세요.",
            "Firebase 설정 필요", MessageBoxButton.OK, MessageBoxImage.Information);
        return null;
    }

    private void UpdateCloudStatus()
    {
        CloudStatus = _session is null ? "Google: 로그아웃" : $"Google: {_session.Email}";
        CommandManager.InvalidateRequerySuggested();
    }

    private void ShowError(string title, string message)
    {
        Status = $"{title}: {message}";
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
