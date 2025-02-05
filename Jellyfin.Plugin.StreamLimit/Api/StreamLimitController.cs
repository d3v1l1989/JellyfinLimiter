using System;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.StreamLimit.Api
{
    using System.Collections.Generic;
    using System.Net.Mime;
    using MediaBrowser.Controller.Resolvers;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Authorize]
    [Route("[controller]/[action]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class StreamLimitController : ControllerBase
    {
        private readonly ILogger<StreamLimitController> _logger;
        private readonly IUserManager _userManager;

        public StreamLimitController(ILogger<StreamLimitController> logger, IUserManager userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult GetUserStreamLimit(string userId)
        {
            try
            {
                var userById = _userManager.GetUserById(Guid.Parse(userId));
                if (userById == null)
                {
                    return this.NotFound("User does not exist");
                }

                var userData = JsonConvert.DeserializeObject<Dictionary<string, int>>(Plugin.Instance.Configuration.UserStreamLimits);
                if (userData != null)
                {
                    int streamsAllowed = userData[userId.Replace("-", string.Empty)];
                    return Ok(new
                    {
                        userId = userId,
                        streamsAllowed = streamsAllowed
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return StatusCode(500, ex.Message);
            }

            return BadRequest();
        }

        [HttpPost]
        public IActionResult SetUserStreamLimit(string userId, int streamsAllowed)
        {
            try
            {
                var userById = _userManager.GetUserById(Guid.Parse(userId));
                if (userById == null)
                {
                    return this.NotFound("User does not exist");
                }

                var userData = JsonConvert.DeserializeObject<Dictionary<string, int>>(Plugin.Instance.Configuration.UserStreamLimits);
                if (userData != null)
                {
                    userData[userId.Replace("-", string.Empty)] = streamsAllowed;
                    Plugin.Instance.Configuration.UserStreamLimits = JsonConvert.SerializeObject(userData);
                    Plugin.Instance.SaveConfiguration();
                    return Ok("user's stream limit set");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return StatusCode(500, ex.Message);
            }

            return BadRequest();
        }

        [HttpPost]
        public IActionResult SetAlertMessage(string alertMessage, string title)
        {
            try
            {
                Plugin.Instance.Configuration.MessageText = alertMessage;
                //Plugin.Instance.Configuration.MessageTimeShowInSeconds = duration;
                Plugin.Instance.Configuration.MessageTitle = title;
                Plugin.Instance.SaveConfiguration();
                return Ok("Message updated:" + title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return StatusCode(500, ex.Message);
            }

            return BadRequest();
        }
    }
}
