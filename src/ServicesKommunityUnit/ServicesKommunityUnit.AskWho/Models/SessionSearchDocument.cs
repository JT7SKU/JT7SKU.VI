using JaniC3N7.Common.Models;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ServicesKommunityUnit.AskWho.Models
{
    [SerializePropertyNamesAsCamelCase]
    public abstract class SearchDocument
    {
        public SearchDocument(string id)
        {
            this.Id = id;
        }
        [Key]
        [IsRetrievable(true)]
        public string Id { get; set; }
    } 

    [SerializePropertyNamesAsCamelCase]
   public class SessionSearchDocument : SearchDocument
    {
        public SessionSearchDocument(string id, DateTime timestampUtc, string task, AskQuestionRequest request, AskQuestionResponse response) : base(id)
        {
            this.SessionId = request.Sessiond;
            this.ConversationId = request.ConversationId;
            this.UserId = request.UserId;
            this.Question = request.Question;
            this.Task = task;
            this.Topics = request.Topics;
            this.TimestampUtc = timestampUtc;

            if (response.Results?.Count() > 0)
            {
                this.AnswerTitle = response.Results[0].Title;
                this.AnswerContent = response.Results[0].Answer;
                this.AnswerImageUrl = response.Results[0].ImageUrl;
                this.AnswerSource = response.Results[0].Source;
                this.AnswerUrl = response.Results[0].Url;
                this.AnswerUrlDisplayName = response.Results[0].UrlDisplayName;
            }
            this.IsAnswered = false;
        }
        public SessionSearchDocument(string id) : base(id)
        {

        }

        [IsRetrievable(true), IsFilterable, IsFacetable]
        public string SessionId { get; set; }
        [IsRetrievable(true),IsFilterable, IsFacetable]
        public string ConversationId { get; set; }
        [IsRetrievable(true), IsFilterable, IsFacetable]
        public string UserId { get; set; }
        [IsRetrievable(true), IsSearchable]
        public string Question { get; set; }
        [IsRetrievable(true), IsFilterable, IsFacetable]
        public string Task { get; set; }
        [IsRetrievable(true),IsFilterable, IsFacetable]
        public string[] Topics { get; set; }
        [IsRetrievable(true),IsFilterable,IsSortable,IsFacetable]
        public DateTimeOffset? TimestampUtc { get; set; }
        [IsRetrievable(true), IsSearchable]
        public string AnswerTitle { get; set; }
        [IsRetrievable(true), IsSearchable]
        public string AnswerContent { get; set; }
        [IsRetrievable(true)]
        public string AnswerImageUrl { get; set; }
        [IsRetrievable(true),IsFilterable, IsFacetable]
        public string AnswerSource { get; set; }
        [IsRetrievable(true)]
        public string AnswerUrl { get; set; }
        [IsRetrievable(true)]
        public string AnswerUrlDisplayName { get; set; }
        [IsRetrievable(true),IsFilterable, IsFacetable]
        public bool IsAnswered { get; set; }
    }
}
