using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Elm;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.WebUtilities;
using Newtonsoft.Json;

namespace LoggingWebSite
{
    public class ElmLogSerializerMiddleware
    {
        private readonly RequestDelegate _next;

        public ElmLogSerializerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context, ElmStore elmStore)
        {
            var currentRequest = context.Request;

            var logActivities = GetLogDetails(elmStore);

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";

            var serializer = JsonSerializer.Create();
            using (var writer = new JsonTextWriter(new StreamWriter(stream: context.Response.Body,
                                                                    encoding: Encoding.UTF8,
                                                                    bufferSize: 1024,
                                                                    leaveOpen: true)))
            {
                serializer.Serialize(writer, logActivities);
            }

            return Task.FromResult(true);
        }

        private IEnumerable<ActivityContextDto> GetLogDetails(ElmStore elmStore)
        {
            var activities = new List<ActivityContextDto>();
            foreach (var activity in elmStore.GetActivities().Reverse())
            {
                var rootScopeNodeDto = new ScopeNodeDto();
                CopyScopeNodeTree(activity.Root, rootScopeNodeDto);

                activities.Add(new ActivityContextDto()
                {
                    RequestInfo = GetRequestInfoDto(activity.HttpInfo),
                    Id = activity.Id,
                    RepresentsScope = activity.RepresentsScope,
                    Root = rootScopeNodeDto
                });
            }

            return activities;
        }

        private RequestInfoDto GetRequestInfoDto(HttpInfo httpInfo)
        {
            if (httpInfo == null) return null;

            return new RequestInfoDto()
            {
                ContentType = httpInfo.ContentType,
                Cookies = httpInfo.Cookies.ToArray(),
                Headers = httpInfo.Headers.ToArray(),
                Query = httpInfo.Query.Value,
                Host = httpInfo.Host.Value,
                Method = httpInfo.Method,
                Path = httpInfo.Path.Value,
                Protocol = httpInfo.Protocol,
                RequestID = httpInfo.RequestID,
                Scheme = httpInfo.Scheme,
                StatusCode = httpInfo.StatusCode
            };
        }

        private LogInfoDto GetLogInfoDto(LogInfo logInfo)
        {
            return new LogInfoDto()
            {
                EventID = logInfo.EventID,
                Exception = logInfo.Exception,
                LoggerName = logInfo.Name,
                LogLevel = logInfo.Severity,
                State = logInfo.State,
                StateType = logInfo.State?.GetType()
            };
        }

        private void CopyScopeNodeTree(ScopeNode root, ScopeNodeDto rootDto)
        {
            rootDto.LoggerName = root.Name;
            rootDto.State = root.State;
            rootDto.StateType = root.State?.GetType();

            foreach (var logInfo in root.Messages)
            {
                rootDto.Messages.Add(GetLogInfoDto(logInfo));
            }

            foreach (var scopeNode in root.Children)
            {
                ScopeNodeDto childDto = new ScopeNodeDto();

                CopyScopeNodeTree(scopeNode, childDto);

                rootDto.Children.Add(childDto);
            }
        }
    }
}