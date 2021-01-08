using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace StaticArchiveServe
{
    [ApiController]
    [Route("{*.}")]
    public class StaticServeController : ControllerBase
    {
        private readonly ILogger<StaticServeController> _logger;

        public StaticServeController(ILogger<StaticServeController> logger)
        {
            _logger = logger;
        }

        async Task Send404()
        {
            Response.StatusCode = 404;
            await Response.Body.WriteAsync(ArchiveContent.Error404PageBytes, 0, ArchiveContent.Error404PageBytes.Length);
        }

        [HttpGet]
        public async Task Get()
        {
            string path = Request.Path + Request.QueryString;
            path = path.Split('#')[0];
            var entry = ArchiveContent.TryFetchEntry(path);
            if(entry == null)
            {
                await Send404();
                return;
            }

            var stream = await entry.FetchStream();
            if(stream == null)
            {
                await Send404();
                return;
            }

            Response.StatusCode = entry.status;
            if(entry.contentType != null) Response.Headers.Add("content-type", entry.contentType);
            if(entry.location != null) Response.Headers.Add("location", entry.location);
            await stream.CopyToAsync(Response.Body);
            stream.Close();
        }
    }
}