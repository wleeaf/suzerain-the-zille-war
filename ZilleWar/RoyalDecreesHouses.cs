using SuzerainModdingKit;

namespace ZilleWar;

/// <summary>
/// House standing for the war decrees, and the HUD lines of the vanilla decree edits (VanillaDecreesAll).
/// House standing is rebuilt from scratch at every story fragment (Calculation_Relations_Houses resets the three Houses and
/// re-adds the HouseOpinions_* rows), so a "Country_House... +=" in a decree script is erased at the next scene. The war decrees'
/// House effects are therefore rows of HouseOpinions_DecreesSeven, keyed on their saved ZilleWar.Decree_* flags.
/// The vanilla-edit lines charge only decrees signed under this build (ZilleWar.VDec_*_New, set by VanillaDecreesAll's watchers),
/// so older saves keep vanilla's behavior for decrees already signed.
/// </summary>
internal static partial class WarDecrees
{
    /// <summary>Set when the Wehzek prisoners are released after Wraclawitz paid a ransom for them.</summary>
    internal const string PrisonersRansomedVar = "ZilleWar.Decree_ReleasePrisoners_Ransomed";

    private const string HouseRows = "HouseOpinions_DecreesSeven";

    private static void RegisterRoyalHouses()
    {
        Variables.Register(PrisonersRansomedVar);
        Vanilla.Expose(PrisonersRansomedVar);

        foreach ((string key, string instr) in new[]
                 {
                     (Reserves, "RiziaDLC.Country_HouseSazon += 1;"),
                     (StandDown, "RiziaDLC.Country_HouseSazon += 1;"),
                     (Intern, "RiziaDLC.Country_HouseSazon -= 1;"),
                     (ReleaseInterned, "RiziaDLC.Country_HouseSazon += 1;"),
                     (Pensions, "RiziaDLC.Country_HouseAzaro += 1;"),
                     (CivilAdminKey, "RiziaDLC.Country_HouseAzaro -= 1;"),
                     (LoyaltyOathKey, "RiziaDLC.Country_HouseSazon += 1;"),
                     (WehzekSchoolsKey, "RiziaDLC.Country_HouseSazon -= 1;"),
                     (RansomSoldiersKey, "RiziaDLC.Country_HouseAzaro += 1;"),
                     (CourtMartialKey, "RiziaDLC.Country_HouseAzaro -= 2;"),
                     (InquiryKey, "RiziaDLC.Country_HouseAzaro += 1;"),
                     (SuspendReparationsKey, "RiziaDLC.Country_HouseSazon += 1;"),
                 })
        {
            AddRow(HouseRows, $"{EnactedVar(key)} == true", instr);
        }

        AddRow(HouseRows, $"{PrisonersRansomedVar} == true", "RiziaDLC.Country_HouseAzaro -= 1;");

        const string budget = "Rizia_HUDStat_Budget";
        AddLine(new("Rizia_Budget_Modifier_Royal_LandOwnersTax", budget, "ZilleWar.Dec_LandOwnersTax",
            "+{0} per turn from Land Owners Tax Registers",
            () => Variables.GetBool("RiziaDLC.Policy_RD_Economy_LandOwnersTax") && Variables.GetBool("ZilleWar.VDec_LandTax_New") ? 1 : 0));
        AddLine(new("Rizia_Budget_Modifier_Royal_LevyUpkeep", budget, "ZilleWar.Dec_LevyUpkeep",
            "-{0} per turn from Provincial Levy Upkeep",
            () => Variables.GetBool("RiziaDLC.Policy_RD_Order_IncreaseProvincialLevies") &&
                  !Variables.GetBool("RiziaDLC.Policy_RD_Order_RemoveProvincialLevies") &&
                  Variables.GetBool("ZilleWar.VDec_Levies_New") ? -1 : 0));
        AddLine(new("Rizia_Budget_Modifier_Royal_ReligiousExemptions", budget, "ZilleWar.Dec_ReligiousExemptions",
            "-{0} per turn from Religious Tax Exemptions",
            () => Variables.GetBool("RiziaDLC.Policy_RD_Religion_TaxCutsForReligiousOrgs") && Variables.GetBool("ZilleWar.VDec_TaxCuts_New") ? -1 : 0));
    }
}
