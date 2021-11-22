using libanvl.Depot;

namespace libanvl;

internal class DepotArguments
{
    public DepotArguments(DirectoryInfo depotRoot, bool createDepot, bool mergeDefaultSettings)
    {
        DepotRoot = depotRoot;
        CreateDepot = createDepot;
        MergeDefaultSettings = mergeDefaultSettings;
    }

    public DirectoryInfo DepotRoot { get; }

    public bool CreateDepot { get; }

    public bool MergeDefaultSettings { get; }

    public DepotContext GetDepotContext() => DepotContext.Create(DepotRoot.FullName, CreateDepot, MergeDefaultSettings);
}
