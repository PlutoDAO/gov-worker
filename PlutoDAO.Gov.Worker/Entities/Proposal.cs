using System.Collections.Generic;

namespace PlutoDAO.Gov.Worker.Entities
{
    public class Proposal
    {
        public Proposal(IEnumerable<Option> options, IEnumerable<WhitelistedAsset> whitelistedAssets)
        {
            Options = options;
            WhitelistedAssets = whitelistedAssets;
        }

        public IEnumerable<Option> Options { get; }
        public IEnumerable<WhitelistedAsset> WhitelistedAssets { get; }
    }
}
