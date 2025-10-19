using Microsoft.AspNetCore.Http;

namespace EcoWrap.Models.Templates
{
    public enum DimUnit { mm, cm, @in } // @in để tránh từ khóa

    public sealed class CreateTemplateVersionFormDto
    {
        
        public IFormFile? File { get; set; }

      
        public int? Width { get; set; }        
        public int? Height { get; set; }       
        public string? Unit { get; set; }      
        

        public decimal? Bleed { get; set; }
        public decimal? SafeArea { get; set; }
        public string? LayersCsv { get; set; }
        public string? Model3dUrl { get; set; }
    }
}
