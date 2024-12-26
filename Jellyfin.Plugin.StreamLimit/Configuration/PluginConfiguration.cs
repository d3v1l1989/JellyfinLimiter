namespace Jellyfin.Plugin.StreamLimit.Configuration;

using System.Collections.Generic;
using System.Xml.Serialization;
using MediaBrowser.Model.Plugins;

/// <summary>
/// Plugin configuration.
/// </summary>
[XmlRoot("StreamLimiterConfiguration")]
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        UserNumericValues = string.Empty;
        MessageTitleToShow = "Stream Limit";
        MessageTextToShow = "Active streams exceeded";
    }

    /// <summary>
    /// Gets or sets the JSON string containing user stream limits.
    /// Format: Dictionary of user IDs to maximum allowed streams.
    /// </summary>
    [XmlElement(ElementName = "UserStreamLimits")]
    public string UserNumericValues { get; set; }

    /// <summary>
    /// Gets or sets the title of the message shown when stream limit is exceeded.
    /// </summary>
    [XmlElement(ElementName = "MessageTitle")]
    public string MessageTitleToShow { get; set; }

    /// <summary>
    /// Gets or sets the text of the message shown when stream limit is exceeded.
    /// </summary>
    [XmlElement(ElementName = "MessageText")]
    public string MessageTextToShow { get; set; }
}
