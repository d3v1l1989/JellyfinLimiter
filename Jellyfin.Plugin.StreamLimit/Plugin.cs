namespace Jellyfin.Plugin.StreamLimit;

using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.StreamLimit.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{

    /// <inheritdoc />
    public override string Name => "StreamLimiter";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("d98fbe02-daf3-4c09-a832-4b4e1d07326c");
    public override string ConfigurationFileName => "StreamLimiter";

    public override string Description => "Stream Limiter";

    private readonly ILogger<Plugin> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISessionManager _sessionManager;
    private readonly IHttpContextAccessor _authenticationManager;
    private readonly string? userName;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// <param name="sessionManager">Instance. </param>
    /// <param name="authenticationManager">Auth Manager.</param>
    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ISessionManager sessionManager,
        IHttpContextAccessor authenticationManager,
        ILoggerFactory loggerFactory)
        : base(applicationPaths, xmlSerializer)
    {
        _sessionManager = sessionManager;
        _authenticationManager = authenticationManager;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<Plugin>();
        Instance = this;
        userName = authenticationManager?.HttpContext?.User?.Identity?.Name;
        _logger.LogInformation("Stream limiter started");
    }

    

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Get plug in info.
    /// </summary>
    /// <returns>PluginInfo.</returns>
    public override PluginInfo GetPluginInfo()
    {
        var pluginInfo = new PluginInfo(Name, Version,
            "Stream Limiter", Id, false)
        { HasImage = false, Status = PluginStatus.Active };
        return pluginInfo;
    }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        };
    }
}
