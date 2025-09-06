using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Jellyfin.Plugin.StreamLimit.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MediaBrowser.Controller.Events;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Plugin.StreamLimit.Limiter;

public sealed class PlaybackStartLimiter : IEventConsumer<PlaybackStartEventArgs>
{
    private readonly ISessionManager _sessionManager;
    private readonly IHttpContextAccessor _authenticationManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDeviceManager _deviceManager;
    private readonly ILogger<PlaybackStartLimiter> _logger;
    private PluginConfiguration? _configuration;
    private Dictionary<string, int> _userData = new();

    private static int _taskCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStartLimiter"/> class.
    /// </summary>
    public PlaybackStartLimiter(
        [NotNull] ISessionManager sessionManager,
        [NotNull] IHttpContextAccessor authenticationManager,
        [NotNull] ILoggerFactory loggerFactory,
        [NotNull] IDeviceManager deviceManager)
    {
        _sessionManager = sessionManager;
        _authenticationManager = authenticationManager;
        _loggerFactory = loggerFactory;
        _deviceManager = deviceManager;
        _logger = loggerFactory.CreateLogger<PlaybackStartLimiter>();
        _configuration = Plugin.Instance?.Configuration as PluginConfiguration;

        if (Plugin.Instance is not null)
        {
            Plugin.Instance.ConfigurationChanged += (sender, args) =>
            {
                _configuration = args as PluginConfiguration;
                LoadUserData();
            };
        }

        LoadUserData();
    }

    private void LoadUserData()
    {
        if (_configuration == null)
        {
            _logger.LogWarning("Configuration is null when loading user data");
            return;
        }

        var configurationUserStreamLimits = _configuration.UserStreamLimits;
        if (string.IsNullOrEmpty(configurationUserStreamLimits))
        {
            return;
        }

        try
        {
            _userData = JsonConvert.DeserializeObject<Dictionary<string, int>>(configurationUserStreamLimits) ?? new Dictionary<string, int>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert configurationUserStreamLimits to object");
        }
    }

    private static int GetNextTaskNumber() => Interlocked.Increment(ref _taskCounter);

    public async Task OnEvent(PlaybackStartEventArgs e)
    {
        var taskNumber = GetNextTaskNumber();
        _logger.LogInformation("[{TaskNumber}] ---------------[StreamLimit_Start]---------------", taskNumber);

        try
        {
            if (e.Users.Count == 0 || e.Users[0].Id == Guid.Empty)
            {
                _logger.LogInformation("[{TaskNumber}] [Error] Invalid user ID", taskNumber);
                return;
            }

            if (e.Session?.Id == null)
            {
                _logger.LogInformation("[{TaskNumber}] [Error] Invalid session", taskNumber);
                return;
            }

            var userId = e.Users[0].Id.ToString();
            _logger.LogInformation("[{TaskNumber}] Playback Started : {UserId}", taskNumber, userId);

            var activeStreamsForUser = _sessionManager.Sessions.Count(s =>
                s.UserId == Guid.Parse(userId) &&
                s.NowPlayingItem != null &&
                s.IsActive);
            _logger.LogInformation("[{TaskNumber}] Streaming Active : {ActiveStreams}", taskNumber, activeStreamsForUser);

            var userDataKey = userId.Replace("-", string.Empty);
            var hasPerUser = _userData.TryGetValue(userDataKey, out var perUserLimit);
            var defaultLimit = _configuration?.DefaultMaxStreams ?? 0;
            var effectiveLimit = hasPerUser ? perUserLimit : defaultLimit;

            _logger.LogInformation(
                "[{TaskNumber}] Streaming Limit  : {Effective} (User:{PerUser}; Default:{Default}; Using:{Source})",
                taskNumber,
                effectiveLimit,
                perUserLimit,
                defaultLimit,
                hasPerUser ? "User" : "Default");

            if (effectiveLimit > 0 && activeStreamsForUser > effectiveLimit)
            {
                await LimitPlayback(e.Session, taskNumber);
            }
            else
            {
                _logger.LogInformation(
                    "[{TaskNumber}] {Status} : Play Bypass",
                    taskNumber,
                    effectiveLimit > 0 ? "Not Limited" : "No In Limit");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, e, taskNumber);
        }

        _logger.LogInformation("[{TaskNumber}] ----------------[StreamLimit_End]----------------", taskNumber);
    }

    private async Task LimitPlayback(SessionInfo session, int taskNumber)
    {
        _logger.LogInformation("[{TaskNumber}] Attempting to stop playback for session {SessionId}", taskNumber, session.Id);

        try
        {
            await StopPlayback(session, taskNumber);
            await Task.Delay(500);
            await ShowLimitMessage(session, taskNumber);
            await LogoutSession(session, taskNumber);

            _logger.LogInformation("[{TaskNumber}] Limited : Play Canceled", taskNumber);
        }
        catch (Exception stopEx)
        {
            _logger.LogError(stopEx, "[{TaskNumber}] Failed to stop playback", taskNumber);
            throw;
        }
    }

    private async Task StopPlayback(SessionInfo session, int taskNumber)
    {
        try
        {
            await _sessionManager.SendPlaystateCommand(
                session.Id,
                session.Id,
                new PlaystateRequest
                {
                    Command = PlaystateCommand.Stop,
                    ControllingUserId = session.UserId.ToString(),
                    SeekPositionTicks = 0,
                },
                CancellationToken.None);

            _logger.LogInformation("[{TaskNumber}] Successfully sent stop command", taskNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TaskNumber}] Failed to send stop command", taskNumber);
            throw;
        }
    }

    private async Task ShowLimitMessage(SessionInfo session, int taskNumber)
    {
        try
        {
            var MessageText = _configuration?.MessageTitle ?? "Stream Limit";
            var messageText = _configuration?.MessageText ?? "Active streams exceeded";

            await _sessionManager.SendMessageCommand(
                session.Id,
                session.Id,
                new MessageCommand
                {
                    Header = MessageText,
                    Text = messageText,
                },
                CancellationToken.None);

            _logger.LogInformation("[{TaskNumber}] Successfully sent message command", taskNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TaskNumber}] Failed to send message command", taskNumber);
            throw;
        }
    }

    private async Task LogoutSession(SessionInfo session, int taskNumber)
    {
        try
        {
            await _sessionManager.Logout(session.Id);
            _logger.LogInformation("[{TaskNumber}] Successfully logged out session", taskNumber);
        }
        catch (Exception rex)
        {
            _logger.LogWarning(rex, "[{TaskNumber}] Failed to logout session", taskNumber);
        }
    }

    private void LogError(Exception ex, PlaybackStartEventArgs e, int taskNumber)
    {
        if (ex.InnerException != null)
        {
            _logger.LogError(ex.InnerException, "[{TaskNumber}] Inner exception", taskNumber);
        }

        _logger.LogError(
            ex,
            "[{TaskNumber}] Error details - Users: {Users}, PlaySessionId: {PlaySessionId}, Session: {Session}",
            taskNumber,
            e.Users,
            e.PlaySessionId,
            e.Session);
    }
}
