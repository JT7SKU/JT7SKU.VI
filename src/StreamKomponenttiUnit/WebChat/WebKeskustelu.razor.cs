using Microsoft.AspNetCore.Components;
using StreamKomponenttiUnit.Yhteinen.Mallit;
using System;
using System.Collections.Generic;
using System.Text;

using System.Threading.Tasks;

namespace StreamKomponentUnit.WebChat
{
   public class WebChatCode : ComponentBase
    {

        public List<ViestiMalli> Messages { get; set; }
        
        protected override Task OnInitializedAsync()
        {
            return base.OnInitializedAsync();
        }
    }
}
