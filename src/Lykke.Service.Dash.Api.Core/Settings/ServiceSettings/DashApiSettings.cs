namespace Lykke.Service.Dash.Api.Core.Settings.ServiceSettings
{
    public class DashApiSettings
    {
        public DbSettings Db { get; set; }
        public string Network { get; set; }
        public string InsightApiUrl { get; set; }
        public ulong MinFee { get; set; }
        public ulong MaxFee { get; set; }
        public ulong FeePerByte { get; set; }
        public int MinConfirmations { get; set; }
    }
}
