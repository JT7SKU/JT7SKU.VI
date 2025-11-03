using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;
using JaniC3N7.Common.Models;
using System.Threading.Tasks;

namespace StreamKomponentUnit.WebChat
{
   public class WebChatCode : ComponentBase
    {

        public List<MessageModel> Messages { get; set; }
        
        protected override Task OnInitializedAsync()
        {
            return base.OnInitializedAsync();
        }
    }
}
