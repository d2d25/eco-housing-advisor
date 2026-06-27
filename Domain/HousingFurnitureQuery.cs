namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingFurnitureQuery
    {
        public HousingFurnitureQuery(string mode, string text, int page, int pageSize)
        {
            this.Mode = string.IsNullOrWhiteSpace(mode) ? "summary" : mode.Trim().ToLowerInvariant();
            this.Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            this.Page = page < 1 ? 1 : page;
            this.PageSize = pageSize < 1 ? 10 : pageSize;
        }

        public string Mode { get; }

        public string Text { get; }

        public int Page { get; }

        public int PageSize { get; }
    }
}
