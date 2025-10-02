namespace EcoWrap.BLL.IServices
{
    public interface IEmbeddingService
    {
        Task<float[]> EmbedAsync(string text, string taskType = "RETRIEVAL_DOCUMENT");
        static double CosineSimilarity(float[] a, float[] b)
        {
            double dot = 0, na = 0, nb = 0;
            int n = Math.Min(a.Length, b.Length);
            for (int i = 0; i < n; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
            return (na == 0 || nb == 0) ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        }
    }
}
        