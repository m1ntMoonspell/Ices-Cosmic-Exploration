using System.Collections.Generic;

using JobPairs = (string job, uint territoryId, float x, float y);
namespace ICE.Scheduler.Handlers
{
    using LocationEntry = KeyValuePair<string, (JobPairs first, JobPairs second)[]>;
    internal static unsafe class AnnouncementHandlers
    {
        private static readonly string Announcement = "WKSAnnounce";

        private static readonly Dictionary<string, (JobPairs first, JobPairs second)[]> sinusRedAlert = new()
        {
            {
                "流星雨",
                new(JobPairs first, JobPairs second)[]
                {
                    (
                        ("铸甲匠/雕金匠", 1237, 22.6f, 14.9f),
                        ("锻铁匠/制革匠/采矿工", 1237, 16.3f, 24.4f)
                    )
                }
            },
            {
                "孢子雾",
                new(JobPairs first, JobPairs second)[]
                {
                    (
                        ("烹调师/园艺工/捕鱼人", 1237, 24.5f, 16.8f),
                        ("刻木匠/制革匠/裁衣匠", 1237, 29.0f, 35.4f)
                    ),
                    (
                        ("锻铁匠/炼金术士", 1237, 32.2f, 22.2f),
                        ("刻木匠/裁衣匠/园艺工", 1237, 36.0f, 23.4f)
                    )
                }
            },
            {
                "磁暴",
                new(JobPairs first, JobPairs second)[]
                {
                    (
                        ("铸甲匠/雕金匠/炼金术士", 1237, 24.9f, 33.1f),
                        ("采矿工/捕鱼人", 1237, 19.2f, 15.0f)
                    ),
                    (
                        ("刻木匠/雕金匠/裁衣匠", 1237, 19.8f, 36.8f),
                        ("烹调师/采矿工/捕鱼人", 1237, 12.3f, 20.0f)
                    )
                }
            },
        };

        internal static LocationEntry CheckForRedAlert()
        {
            if (!PlayerHelper.IsInCosmicZone()) return default;
            try
            {
                if (AddonHelper.GetAtkTextNode(Announcement, 48)->IsVisible()) // Red Alert Preparation
                {
                    var description = AddonHelper.GetNodeText(Announcement, 47).ToLower();

                    Dictionary<string, (JobPairs first, JobPairs second)[]>? redAlert = default;
                    if (PlayerHelper.IsInSinusArdorum()) redAlert = sinusRedAlert; //Reassign based on Territory

                    if (redAlert == default) return default;
                    return redAlert.FirstOrDefault(location => description.Contains(location.Key));
                }
                else
                {
                    return default;
                }
            }
            catch (Exception ex)
            {
                return default;
            }
        }
    }
}