using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace StreamKomponentUnit.WebChat.Keskustelu
{
    public partial class KeskusteluViestiItemi : ComponentBase
    {
        private static readonly ConditionalWeakTable<ChatMessage, KeskusteluViestiItemi> SubscribersLookup = new();
        private static readonly Regex CitationRegex = new(@"<citation filename='(?<file>[^']*)' page_number='(?<page>\d*)'>(?<quote>.*?)</citation>", RegexOptions.NonBacktracking);

        private List<(string File, int? Page, string Quote)>? citations;

        [Parameter, EditorRequired]
        public required ChatMessage Message { get; set; }

        [Parameter]
        public bool InProgress { get; set; }

        protected override void OnInitialized()
        {
            SubscribersLookup.AddOrUpdate(Message, this);

            if (!InProgress && Message.Role == ChatRole.Assistant && Message.Text is { Length: > 0 } text)
            {
                ParseCitations(text);
            }
        }

        public static void NotifyChanged(ChatMessage source)
        {
            if (SubscribersLookup.TryGetValue(source, out var subscriber))
            {
                subscriber.StateHasChanged();
            }
        }

        private void ParseCitations(string text)
        {
            var matches = CitationRegex.Matches(text);
            citations = matches.Any()
                ? matches.Select(m => (m.Groups["file"].Value, int.TryParse(m.Groups["page"].Value, out var page) ? page : (int?)null, m.Groups["quote"].Value)).ToList()
                : null;
        }
    }
}