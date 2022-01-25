using System;

namespace PlutoDAO.Gov.Worker.Entities
{
    public class WhitelistedAsset : IEquatable<WhitelistedAsset>, IEquatable<Asset>
    {
        public WhitelistedAsset(Asset asset, decimal multiplier)
        {
            Asset = asset;
            Multiplier = multiplier;
        }

        public Asset Asset { get; }
        public decimal Multiplier { get; }

        public bool Equals(Asset other)
        {
            return other is not null && Asset.Equals(other);
        }

        public bool Equals(WhitelistedAsset other)
        {
            return other is not null && Asset.Equals(other.Asset);
        }

        public override bool Equals(object obj)
        {
            return obj switch
            {
                WhitelistedAsset asset => Equals(asset),
                Asset asset => Equals(asset),
                _ => false
            };
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Asset, Multiplier);
        }
    }
}
