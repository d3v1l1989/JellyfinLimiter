using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Jellyfin.Plugin.StreamLimit.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
[XmlRoot("StreamLimiterConfiguration")]
public class PluginConfiguration : MediaBrowser.Model.Plugins.BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        UserStreamLimits = string.Empty;
        MessageTitle = "Stream Limit";
        MessageText = "Active streams exceeded";
    }

    /// <summary>
    /// Gets or sets the JSON string containing user stream limits.
    /// Format: Dictionary of user IDs to maximum allowed streams.
    /// </summary>
    [XmlElement(ElementName = "UserStreamLimits")]
    public string UserStreamLimits { get; set; }

    /// <summary>
    /// Gets or sets the title of the message shown when stream limit is exceeded.
    /// </summary>
    [XmlElement(ElementName = "MessageTitle")]
    public string MessageTitle { get; set; }

    /// <summary>
    /// Gets or sets the text of the message shown when stream limit is exceeded.
    /// </summary>
    [XmlElement(ElementName = "MessageText")]
    public string MessageText { get; set; }
}
