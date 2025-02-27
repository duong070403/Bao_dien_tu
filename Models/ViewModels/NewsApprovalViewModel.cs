using Microsoft.AspNetCore.Mvc;

namespace BTL_Xay_dung_bao_dien_tu.Models.ViewModels
{
    public class NewsApprovalViewModel
    {
        public int NewsId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public bool? IsApproved { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
