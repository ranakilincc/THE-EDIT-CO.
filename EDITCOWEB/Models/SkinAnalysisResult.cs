namespace EDITCOWEB.Models
{
    public class SkinAnalysisResult
    {
        public double BrightnessScore { get; set; }
        public double RednessScore { get; set; }
        public double TextureScore { get; set; }

        public string Brightness { get; set; }
        public string Redness { get; set; }
        public string Texture { get; set; }

        public string SkinType { get; set; }
        public string Summary { get; set; }
        public string Routine { get; set; }
        public string RecommendedProduct { get; set; }
        public string RecommendedCategory { get; set; }
    }
}