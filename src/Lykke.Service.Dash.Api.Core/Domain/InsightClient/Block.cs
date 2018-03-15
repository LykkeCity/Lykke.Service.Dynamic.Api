namespace Lykke.Service.Dash.Api.Core.Domain.InsightClient
{
    public class BlocksInfo
    {
        public Block[] Blocks { get; set; }
    }

    public class Block
    {
        public long Height { get; set; }
    }
}
