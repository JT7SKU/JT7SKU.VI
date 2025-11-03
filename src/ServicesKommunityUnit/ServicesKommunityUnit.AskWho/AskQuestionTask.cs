using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using JaniC3N7.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using ServicesKommunityUnit.AskWho.Models;
using ServicesKommunityUnit.AskWho.Utilities;

namespace ServicesKommunityUnit.AskWho
{
    public static class AskQuestionTask
    {
        [FunctionName(nameof(AskQuestion))]
        public static async Task<HttpResponseMessage> AskQuestion([HttpTrigger(AuthorizationLevel.Function, "post", Route = "AskQuestion")]HttpRequestMessage req,
            [Table("Session")]ICollector<SessionTableEntity> sessionTable, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            MediaTypeHeaderValue contentType = req.Content.Headers.ContentType;
            if (contentType==null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "missing content-type from header.");
            }
            else if (contentType.MediaType.Contains("application/json")==false)
            {
                return req.CreateErrorResponse(HttpStatusCode.UnsupportedMediaType, $"Request's content type, {contentType.MediaType} is not supported.");
            }
            AskQuestionRequest requestBody = await req.Content.ReadAsAsync<AskQuestionRequest>();

            if (string.IsNullOrEmpty(requestBody.Question)==true)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Question is missing from the request content.");
            }
            else if (string.IsNullOrEmpty(requestBody.Sessiond)==true)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Session id ismissing from the request content.");
            }
            AskQuestionResponse response = await AnswerQuestion(requestBody, sessionTable);

            return req.CreateResponse(HttpStatusCode.OK, response);
        }

        public static async Task<AskQuestionResponse> AnswerQuestion(AskQuestionRequest request, ICollector<SessionTableEntity> sesseionTable)
        {
            string id = Guid.NewGuid().ToString();
            DateTime timestampUtc = DateTime.UtcNow;
            request.Topics = await ServicesUtility.GetTopics(request.Question, request.Topics);
            string queryWithTopics = request.Topics?.Count() > 0 ? string.Join(" ", request.Topics).Trim() : request.Question;
            BingWebSearchResult bingWebSearchResult = await ServicesUtility.BingSearch.SearchWebAsync(query: request.Question, count: SettingsUtility.MaxResultsCount);
            AskQuestionResponse response = new AskQuestionResponse()
            {
                Id = id,
                Results = new AskQuestionResult[0]
            };
            if (bingWebSearchResult.WebPagesResult?.Values?.Count()>0)
            {
                response.Results = ServicesUtility.GetAskQuestionResults(bingWebSearchResult);
            }
            await ServicesUtility.UploadDocumentToSearchService(SettingsUtility.AzureSearchIndexName, new SessionSearchDocument(id, timestampUtc, "AskQuestion", request, response));
            sesseionTable.Add(new SessionTableEntity(id, timestampUtc, "Queastion", request, response));
            return response;
        }
    }
}
