using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using JaniC3N7.Common.Models;
using System.Linq;
using ServicesKommunityUnit.AskWho.Utilities;
using ServicesKommunityUnit.AskWho.Models;

namespace ServicesKommunityUnit.AskWho
{
    public static class AskWhoTask
    {
        [FunctionName(nameof(AskWho))]
        public static async Task<HttpResponseMessage> AskWho(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "AskWho")] HttpRequestMessage req,
            [Table("Session")]ICollector<SessionTableEntity> sessionTable ,ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            MediaTypeHeaderValue contentType = req.Content.Headers.ContentType;
            if (contentType == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Missing content-type header.");
            }
            else if (contentType.MediaType.Contains("application/json")== false)
            {
                return req.CreateResponse(HttpStatusCode.UnsupportedMediaType, $"Request's content type, {contentType.MediaType} is not supported");
            }
            AskQuestionRequest requestBody = await req.Content.ReadAsAsync<AskQuestionRequest>();
            if (string.IsNullOrEmpty(requestBody.Question)==true)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Question is missing from the request content.");
            }
            else if (string.IsNullOrEmpty(requestBody.Sessiond)==true)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Session id is missing from the request content");
            }            
            AskQuestionResponse response = await AnswerQuestion(requestBody, sessionTable);

            return req.CreateResponse(HttpStatusCode.OK, response);
        }

        private static async Task<AskQuestionResponse> AnswerQuestion(AskQuestionRequest request, ICollector<SessionTableEntity> sessionTable)
        {
            string id = Guid.NewGuid().ToString();
            DateTime timestampUtc = DateTime.UtcNow;
            request.Topics = await ServicesUtility.GetTopics(request.Question, request.Topics);
            string queryWithTopics = request.Topics?.Count() > 0 ? string.Join(" ", request.Topics).Trim() : request.Question;
            

            AskQuestionResponse response = new AskQuestionResponse()
            {
                Id = id,
                Results = new AskQuestionResult[0]
            };
            double confidenceScoreThreshold = 20;
            QnAMakerSearchResult qnaResult = await ServicesUtility.QnAMaker.GenerateAnswersAsync(queryWithTopics, SettingsUtility.QnAMakerWhoTaskKnowledgeBaseId);
            if (qnaResult.Answers?.Where(a => a.Score >= confidenceScoreThreshold).Count() >0)
            {
                response.Results = ServicesUtility.GetAskQuestionResults(qnaResult, confidenceScoreThreshold, true);
            }
            else
            {
                BingWebSearchResult bingWebSearchResult = await ServicesUtility.BingSearch.SearchWebAsync(query: request.Question, count: SettingsUtility.MaxResultsCount);
                if (bingWebSearchResult.WebPagesResult?.Values?.Count() >0)
                {
                    response.Results = ServicesUtility.GetAskQuestionResults(bingWebSearchResult);
                }
            }
            sessionTable.Add(new SessionTableEntity(id, timestampUtc, "who", request, response));
            return response;
        }
    }
}
