namespace Jellyfin.Plugin.StreamLimit.Configuration;

using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        // set default options here
        this.UserNumericValues = string.Empty;
    }

    /// <summary>
    /// contains the json of user and its number of screens values
    /// </summary>
    public string UserNumericValues { get; set; }

    //public int MessageTimeShowInSeconds { get; set; } = 5;
    public string MessageTitleToShow { get; set; } = "Stream Limit";
    public string MessageTextToShow { get; set; } = "Active streams exceeded";

}
