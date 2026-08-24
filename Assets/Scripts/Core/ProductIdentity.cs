using System.IO;

namespace WalkingIntoNight.TRPG.Core
{
    public static class ProductIdentity
    {
        public const string CompanyName = "Seeunever";
        public const string ProductName = "Walking Into Night";
        public const string ChineseDisplayName = "走入夜境";
        public const string ProductVersion = "0.1.0-alpha.1";
        public const string ApplicationIdentifier =
            "com.seeunever.walkingintonight";

        public const string LegacyCompanyName = "DefaultCompany";
        public const string LegacyProductName = "WalkingIntoNight";
        public const string IdentityMigrationMarkerName =
            ".identity_migration_defaultcompany_v1.done";

        public static string GetLegacyStorageRoot(string currentStorageRoot)
        {
            if (string.IsNullOrWhiteSpace(currentStorageRoot)) return null;

            var companyDirectory = Directory.GetParent(currentStorageRoot);
            var platformRoot = companyDirectory?.Parent;
            if (platformRoot == null) return null;

            return Path.Combine(
                platformRoot.FullName,
                LegacyCompanyName,
                LegacyProductName);
        }
    }
}
