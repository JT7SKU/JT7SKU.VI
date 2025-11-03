using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using JaniC3N7.Common.Models;

namespace ServicesKommunityUnit.AskWho.Models
{
    public class SessionTableEntity: TableEntity
    {
        public SessionTableEntity(string id, DateTime timestampUtc, string questionType, AskQuestionRequest request, AskQuestionResponse response)
        {
            this.PartitionKey = request.Sessiond;
            this.RowKey = id;
            this.ConversationId = request.ConversationId;
            this.UserId = request.UserId;
            this.Question = request.Question;
            this.QuestionType = questionType;
            this.Topics = request.Topics?.Count() >0 ?string.Join(";",request.Topics) : string.Empty;
            this.Timestamp = timestampUtc;
            this.ResponseJson = JsonConvert.SerializeObject(response);
        }
        public string ConversationId { get; set; }
        public string Question { get; set; }
        public string QuestionType { get; set; }
        public string ResponseJson { get; set; }
        public string Topics { get; set; }
        public string UserId { get; set; }
    }
}
