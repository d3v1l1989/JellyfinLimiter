using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Plugin.StreamLimit.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MediaBrowser.Controller.Events;
using System.Threading.Tasks;



namespace Jellyfin.Plugin.StreamLimit.Limiter;

public class PlaybackStartLimiter : IEventConsumer<PlaybackStartEventArgs>
{
    private readonly ISessionManager sessionManager;
    private readonly IHttpContextAccessor authenticationManager;
    private readonly ILoggerFactory loggerFactory;
    private readonly IDeviceManager deviceManager;
    private readonly ILogger<PlaybackStartLimiter> logger;
    private PluginConfiguration? configuration;
    private Dictionary<string, int>? userData = new Dictionary<string, int>();

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStartLimiter"/> class.
    /// </summary>
    /// <param name="sessionManager">sessionManager.</param>
    /// <param name="authenticationManager">authenticationManager.</param>
    /// <param name="loggerFactory">loggerFactory.</param>
    /// <param name="deviceManager">deviceManager.</param>
    public PlaybackStartLimiter(
        ISessionManager sessionManager,
        IHttpContextAccessor authenticationManager,
        ILoggerFactory loggerFactory,
        IDeviceManager deviceManager)
    {
        this.sessionManager = sessionManager;
        this.authenticationManager = authenticationManager;
        this.loggerFactory = loggerFactory;
        this.deviceManager = deviceManager;
        this.logger = loggerFactory.CreateLogger<PlaybackStartLimiter>();
        this.configuration = Plugin.Instance?.Configuration as PluginConfiguration;
        Plugin.Instance!.ConfigurationChanged += (sender, pluginConfiguration) =>
        {
            this.configuration = pluginConfiguration as PluginConfiguration;
            this.LoadUserData();
        };
        this.logger.LogInformation("");
    }
    private void LoadUserData()
    {
        this.configuration = Plugin.Instance.Configuration;//this.configuration;

        var configurationUserNumericValues = this.configuration?.UserNumericValues;
        if (configurationUserNumericValues != null)
        {
            try
            {
                this.userData = JsonConvert.DeserializeObject<Dictionary<string, int>>(configurationUserNumericValues);
            }
            catch (Exception ex)
            {
                this.logger.LogError("Failed to convert configurationUserNumericValues to object");
            }
        }
    }
    public static class IncrementalNumberGenerator
    {
        private static int _counter = 0;

        public static int GetNextNumber()
        {
            return Interlocked.Increment(ref _counter);
        }
    }

    public async Task OnEvent(PlaybackStartEventArgs e)
    {
        int TaskNumber = IncrementalNumberGenerator.GetNextNumber();
        this.logger.LogInformation($"[{TaskNumber}] ---------------[StreamLimit_Start]---------------");


        try
        {
            if (e.Users[0].Id == null)
            {
                this.logger.LogInformation($"[{TaskNumber}] [Error] e.Users[0].Id is null");
                return;
            }

            if (e.Session == null)
            {
                this.logger.LogInformation($"[{TaskNumber}] [Error] e.Session is null");
                return;
            }

            if (e.Session.Id == null)
            {
                this.logger.LogInformation($"[{TaskNumber}] [Error] e.Session.Id is null");
                return;
            }

            var userId = e.Users[0].Id.ToString();
            this.logger.LogInformation($"[{TaskNumber}] Playback Started : {userId}");

            var activeStreamsForUser = this.sessionManager.Sessions.Count(s => s.UserId == Guid.Parse(userId) && s.NowPlayingItem != null);
            this.logger.LogInformation($"[{TaskNumber}] Streaming Active : {activeStreamsForUser}");

            var userDataKey = userId.Replace("-", string.Empty);
            var maxStreamsAllowed = 0;
            LoadUserData();
            if (this.userData?.TryGetValue(userDataKey, out var value) is true)
            {
                maxStreamsAllowed = value;
                this.logger.LogInformation($"[{TaskNumber}] Streaming Limit  : {maxStreamsAllowed} [Y]");
            } else {
                this.logger.LogInformation($"[{TaskNumber}] Streaming Limit  : {maxStreamsAllowed} [N]");
            }

            if (maxStreamsAllowed > 0) {
                if (activeStreamsForUser > maxStreamsAllowed)
                {
                this.sessionManager.SendPlaystateCommand(
                    e.Session.Id,
                    e.Session.Id,
                    new PlaystateRequest()
                    {
                        Command = PlaystateCommand.Stop,
                        ControllingUserId = e.Session.UserId.ToString(),
                        SeekPositionTicks = e.Session.PlayState?.PositionTicks,
                    },
                    CancellationToken.None)
                    .Wait();
                this.sessionManager.SendMessageCommand(
                    e.Session.Id,
                    e.Session.Id,
                    new MessageCommand()
                    {
                        Header = this.configuration?.MessageTitleToShow ?? "Stream Limit",
                        Text = this.configuration?.MessageTextToShow ?? "Active streams exceeded",
                    },
                    CancellationToken.None)
                .Wait();
                this.logger.LogInformation($"[{TaskNumber}] Limited          : Play Canceled");
                } else {
                    this.logger.LogInformation($"[{TaskNumber}] Not Limited      : Play Bypass");
                }
            } else {
                this.logger.LogInformation($"[{TaskNumber}] No Limit         : Play Bypass");
            }
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
            {
                this.logger.LogError($"[{TaskNumber}] inner exception");
                this.logger.LogError(ex.InnerException.Message, ex.InnerException);
            }
            this.logger.LogError($"[{TaskNumber}] e.Users: {e.Users}");
            this.logger.LogError($"[{TaskNumber}] e.PlaySessionId: {e.PlaySessionId}");
            this.logger.LogError($"[{TaskNumber}] e.Session: {e.Session}");
            this.logger.LogError($"[{TaskNumber}] StreamLimiter::" + ex.Message, ex);
        }
        this.logger.LogInformation($"[{TaskNumber}] ----------------[StreamLimit_End]----------------");
    }
}

