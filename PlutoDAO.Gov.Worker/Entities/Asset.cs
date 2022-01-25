using System;

namespace PlutoDAO.Gov.Worker.Entities
{
    public class Asset : IEquatable<Asset>
    {
        public Asset(AccountAddress issuer, string code, bool isNative = false)
        {
            Code = string.IsNullOrWhiteSpace(code) ? throw new ArgumentNullException(nameof(code)) : code;
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
            IsNative = isNative;
        }

        public string Code { get; }
        public bool IsNative { get; }
        public AccountAddress Issuer { get; }

        public bool Equals(Asset asset)
        {
            if (asset is null) return false;
            if (asset.IsNative && IsNative)
                return true;

            return asset.Code == Code && Issuer.Address == asset.Issuer.Address;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Asset);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Code, IsNative, Issuer);
        }
    }
}
