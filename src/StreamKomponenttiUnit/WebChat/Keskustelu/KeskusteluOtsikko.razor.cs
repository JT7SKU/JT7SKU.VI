using Microsoft.AspNetCore.Components;

namespace StreamKomponentUnit.WebChat.Keskustelu
{
    public partial class KeskusteluOtsikko :ComponentBase
    {
        [Parameter]
        public EventCallback OnNewChat { get; set; }
    }
}