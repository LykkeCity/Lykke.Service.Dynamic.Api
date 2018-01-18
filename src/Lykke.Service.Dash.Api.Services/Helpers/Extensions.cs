using Lykke.Service.Dash.Api.Core.Domain.InsightClient;
using System.Linq;

namespace Lykke.Service.Dash.Api.Services.Helpers
{
    public static class Extensions
    {
        public static decimal GetAmount(this Tx self)
        {
            var amount = 0m;

            if (self.Vin != null && self.Vin.Any() &&
                self.Vout != null && self.Vout.Any())
            {
                var vinAddresses = self.Vin.Select(f => f.Addr);

                foreach (var vout in self.Vout)
                {
                    if (vout.ScriptPubKey != null && 
                        vout.ScriptPubKey.Addresses != null &&
                        vout.ScriptPubKey.Addresses.Count(f => vinAddresses.Contains(f)) == 0)
                    {
                        amount += vout.Value;
                    }
                }
            }

            return amount;
        }
    }
}
