using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Dash.Api.Core.Domain.InsightClient
{
    public class Blocks
    {
        public Block[] Items { get; set; }
    }

    public class Block
    {
        public long Height { get; set; }
    }
}
