namespace EcoWrap.Models.Request
{
    public class CreateCategoryRequest
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
    }
}