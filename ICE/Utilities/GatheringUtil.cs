using System.Collections.Generic;

namespace ICE.Utilities;

public static unsafe class GatheringUtil
{

    public class GatheringActions
    {
        /// <summary>
        /// Internal name for myself to know wtf this is
        /// </summary>
        public string ActionName { get; set; }
        /// <summary>
        /// Sheet name
        /// </summary>
        public string BtnName { get; set; }
        /// <summary>
        /// Botanist Action ID
        /// </summary>
        public uint BtnActionId { get; set; }
        /// <summary>
        /// Sheet name
        /// </summary>
        public string MinName { get; set; }
        /// <summary>
        /// Miner Action ID
        /// </summary>
        public uint MinActionId { get; set; }
        /// <summary>
        /// If it has a status, the ID associated with it
        /// </summary>
        public uint StatusId { get; set; }
        /// <summary>
        /// The status name attached to it (personal use)
        /// </summary>
        public string StatusName { get; set; }
        /// <summary>
        /// The amount of GP required for the skill
        /// </summary>
        public int RequiredGp { get; set; }
    }

    public static Dictionary<string, GatheringActions> GathActionDict = new()
    {
        { "BoonIncrease1", new GatheringActions
        {
            ActionName = "Pioneer's Gift I",
            BtnName = "",
            BtnActionId = 21178,
            MinName = "",
            MinActionId = 21177,
            StatusId = 2666,
            StatusName = "Gift of the Land",
            RequiredGp = 50,
        }},
        { "BoonIncrease2", new GatheringActions
        {
            ActionName = "Pioneer's Gift II",
            BtnName = "",
            BtnActionId = 25590,
            MinName = "",
            MinActionId = 25589,
            StatusId = 759,
            StatusName = "Gift of the Land II",
            RequiredGp = 100,
        }},
        { "Tidings", new GatheringActions
        {
            ActionName = "Nophica's Tidings",
            BtnName = "",
            BtnActionId = 21204,
            MinName = "",
            MinActionId = 21203,
            StatusId = 2667,
            StatusName = "Gatherer's Bounty",
            RequiredGp = 200,
        }},
        { "Yield1", new GatheringActions
        {
            ActionName = "Blessed Harvest",
            BtnName = "",
            BtnActionId = 222,
            MinName = "",
            MinActionId = 239,
            StatusId = 219,
            StatusName = "Gathering Yield Up",
            RequiredGp = 400,
        }},
        { "Yield2", new GatheringActions
        {
            ActionName = "Blessed Harvest II",
            BtnName = "",
            BtnActionId = 224,
            MinName = "",
            MinActionId = 241,
            StatusId = 219,
            StatusName = "Gathering Yield Up",
            RequiredGp = 500,
        }},
        { "IntegrityIncrease", new GatheringActions
        {
            ActionName = "Ageless Words",
            BtnName = "",
            BtnActionId = 215,
            MinName = "",
            MinActionId = 232,
            RequiredGp = 300,
        }},
        { "BonusIntegrityChance", new GatheringActions
        {
            ActionName = "Wise of the World",
            BtnName = "",
            BtnActionId = 26522,
            MinName = "",
            MinActionId = 26521,
            StatusId = 2765,
            StatusName = "",
            RequiredGp = 0,
        }},
    };

    public class GatheringInfo
    {
        public uint NodeSet { get; set; } // which nodeset does this belong to
        public uint Type { get; set; } // What kind of mission does this belong in?
    }

    /// <summary>
    /// Key {uint} - The missionID #
    /// Nodeset {uint} - Which gathering path does it need to follow
    /// Type {uint} - What kind of mission is this? 
    /// </summary>
    public static Dictionary<uint, GatheringInfo> GatherMissionInfo = new Dictionary<uint, GatheringInfo>()
    {

    };

    public class GathNodeInfo
    {
        public Vector3 Position { get; set; }
        public Vector3 LandZone { get; set; }
        public uint NodeId { get; set; }
        public int GatheringType { get; set; }
        public int ZoneId { get; set; }
        public uint NodeSet { get; set; }
    }

    public static List<GathNodeInfo> MoonNodeInfoList = new()
    {
        new GathNodeInfo // Template for how it should be kept
        {
            Position = new Vector3(0, 0, 0), // The stored coords of the node, rounded up 2
            LandZone = new Vector3(0, 0, 0), // The position/place where you want to stand to gather
            NodeId = 0, // The dataId of said node
            GatheringType = 0, // What type is it? 2 = Miner, 3 = Btn
            ZoneId = 0, // Matters moreso for future moon... moons? Just safety profing
            NodeSet = 0 // Which set of gathering points does this belong to? Ties together all the nodes into one for documentation
                        // Going to see if I can tie it by missionId set maybe... things to dig into
        },

        #region D Rank Missions

        #region (224, 82, 100)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35159,
            Position = new Vector3 (223.44f, 20.26f, 8.42f),
            LandZone = new Vector3 (222.75f, 19.4f, 8.04f),
            GatheringType = 3,
            NodeSet = 1
        },

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35160,
            Position = new Vector3 (223.99f, 20.31f, 15.83f),
            LandZone = new Vector3 (224.33f, 19.39f, 15.45f),
            GatheringType = 3,
            NodeSet = 1
        },

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35163,
            Position = new Vector3 (285.3f, 18.88f, 108.14f),
            LandZone = new Vector3 (285.2f, 17.89f, 108.78f),
            GatheringType = 3,
            NodeSet = 1
        },

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35164,
            Position = new Vector3 (286.34f, 19.03f, 114.67f),
            LandZone = new Vector3 (286.04f, 18.02f, 114.53f),
            GatheringType = 3,
            NodeSet = 1
        },

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35162,
            Position = new Vector3 (207.68f, 18.92f, 145.46f),
            LandZone = new Vector3 (207.74f, 17.93f, 145.63f),
            GatheringType = 3,
            NodeSet = 1
        },

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35161,
            Position = new Vector3 (186.24f, 19.58f, 152.27f),
            LandZone = new Vector3 (186.34f, 18.68f, 152.56f),
            GatheringType = 3,
            NodeSet = 1
        },

        #endregion

        #region (231, -50, 100)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35137,
            Position = new Vector3 (221.91f, 20.14f, 1.22f),
            LandZone = new Vector3 (221.63f, 19.27f, 1.76f),
            GatheringType = 3,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35136,
            Position = new Vector3 (214.32f, 20.07f, -9.12f),
            LandZone = new Vector3 (214.58f, 19.17f, -9.09f),
            GatheringType = 3,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35141,
            Position = new Vector3 (212.42f, 18.82f, -23.42f),
            LandZone = new Vector3 (212.86f, 18.07f, -22.78f),
            GatheringType = 3,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35140,
            Position = new Vector3 (231.74f, 19.9f, -27.72f),
            LandZone = new Vector3 (231.49f, 18.96f, -27.89f),
            GatheringType = 3,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35139,
            Position = new Vector3 (244.55f, 21.27f, -39.45f),
            LandZone = new Vector3 (244.63f, 20.4f, -39.56f),
            GatheringType = 3,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35138,
            Position = new Vector3 (251.86f, 22.5f, -54.28f),
            LandZone = new Vector3 (252.26f, 21.65f, -54.21f),
            GatheringType = 3,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35142,
            Position = new Vector3 (238.31f, 21.42f, -64.92f),
            LandZone = new Vector3 (238.52f, 20.47f, -65.07f),
            GatheringType = 3,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35135,
            Position = new Vector3 (225.6f, 20.73f, -83.05f),
            LandZone = new Vector3 (225.59f, 19.73f, -82.86f),
            GatheringType = 3,
            NodeSet = 2
        },

        #endregion

        #region (-278 -13 100)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35168,
            Position = new Vector3 (-228.04f, 17.31f, -13.59f),
            LandZone = new Vector3 (-228.84f, 16.82f, -13.53f),
            GatheringType = 3,
            NodeSet = 3
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35167,
            Position = new Vector3 (-238.16f, 17.82f, -33.92f),
            LandZone = new Vector3 (-237.94f, 17.22f, -33.02f),
            GatheringType = 3,
            NodeSet = 3
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35166,
            Position = new Vector3 (-325.44f, 28.14f, -51.83f),
            LandZone = new Vector3 (-324.69f, 27.81f, -51.55f),
            GatheringType = 3,
            NodeSet = 3
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35165,
            Position = new Vector3 (-330.45f, 29.18f, -28.82f),
            LandZone = new Vector3 (-330.08f, 28.69f, -29.41f),
            GatheringType = 3,
            NodeSet = 3
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35169,
            Position = new Vector3 (-288.95f, 25.49f, 44.38f),
            LandZone = new Vector3 (-288.93f, 25.07f, 43.86f),
            GatheringType = 3,
            NodeSet = 3
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35170,
            Position = new Vector3 (-273.81f, 23.78f, 44.75f),
            LandZone = new Vector3 (-274.15f, 23.28f, 44.57f),
            GatheringType = 3,
            NodeSet = 3
        },

        #endregion

        #endregion

        #region C Rank Missions

        #region (-120, 368, 100)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35176,
            Position = new Vector3 (-185.84f, 32.88f, 351.45f),
            LandZone = new Vector3 (-185.52f, 32.05f, 351.6f),
            GatheringType = 3,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35175,
            Position = new Vector3 (-174.21f, 32.41f, 342.16f),
            LandZone = new Vector3 (-174.13f, 31.51f, 342.22f),
            GatheringType = 3,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35171,
            Position = new Vector3 (-83.13f, 28.26f, 312.21f),
            LandZone = new Vector3 (-83.36f, 27.38f, 311.6f),
            GatheringType = 3,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35172,
            Position = new Vector3 (-69.83f, 27.94f, 330.99f),
            LandZone = new Vector3 (-69.73f, 27.02f, 330.61f),
            GatheringType = 3,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35173,
            Position = new Vector3 (-110.16f, 34.36f, 427.53f),
            LandZone = new Vector3 (-110.27f, 33.45f, 428.06f),
            GatheringType = 3,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35174,
            Position = new Vector3 (-116.48f, 34.47f, 438.21f),
            LandZone = new Vector3 (-116.7f, 33.51f, 437.75f),
            GatheringType = 3,
            NodeSet = 4
        },

        #endregion

        #region (455, 243, 100)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35177,
            Position = new Vector3 (490.19f, 36.57f, 174.08f),
            LandZone = new Vector3 (489.71f, 35.7f, 174.19f),
            GatheringType = 3,
            NodeSet = 5
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35178,
            Position = new Vector3 (441.94f, 36.48f, 176.07f),
            LandZone = new Vector3 (441.74f, 35.5f, 176.29f),
            GatheringType = 3,
            NodeSet = 5
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35180,
            Position = new Vector3 (382.5f, 35.42f, 265.39f),
            LandZone = new Vector3 (383.06f, 35.01f, 264.97f),
            GatheringType = 3,
            NodeSet = 5
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35179,
            Position = new Vector3 (395.4f, 37.31f, 276.67f),
            LandZone = new Vector3 (395.65f, 36.48f, 276.62f),
            GatheringType = 3,
            NodeSet = 5
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35181,
            Position = new Vector3 (476.83f, 40.68f, 297.1f),
            LandZone = new Vector3 (477.28f, 39.93f, 297.15f),
            GatheringType = 3,
            NodeSet = 5
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35182,
            Position = new Vector3 (510.84f, 37.4f, 283.41f),
            LandZone = new Vector3 (510.55f, 36.63f, 283.3f),
            GatheringType = 3,
            NodeSet = 5
        },

        #endregion

        #region (456, 221, 100)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35143,
            Position = new Vector3 (421.1f, 33.1f, 189.18f),
            LandZone = new Vector3 (421.21f, 32.62f, 189.4f),
            GatheringType = 3,
            NodeSet = 6
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35150,
            Position = new Vector3 (441.48f, 33.91f, 186.57f),
            LandZone = new Vector3 (441.11f, 33.22f, 186.48f),
            GatheringType = 3,
            NodeSet = 6
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35147,
            Position = new Vector3 (439.68f, 34.32f, 211f),
            LandZone = new Vector3 (439.92f, 33.71f, 210.52f),
            GatheringType = 3,
            NodeSet = 6
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35148,
            Position = new Vector3 (460.41f, 34.31f, 206.39f),
            LandZone = new Vector3 (460.12f, 33.59f, 206.25f),
            GatheringType = 3,
            NodeSet = 6
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35149,
            Position = new Vector3 (468.54f, 34.92f, 216.16f),
            LandZone = new Vector3 (468.2f, 34.34f, 216.77f),
            GatheringType = 3,
            NodeSet = 6
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35144,
            Position = new Vector3 (459.23f, 34.89f, 234.63f),
            LandZone = new Vector3 (459.71f, 34.33f, 234.41f),
            GatheringType = 3,
            NodeSet = 6
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35145,
            Position = new Vector3 (454.67f, 34.75f, 254.14f),
            LandZone = new Vector3 (454.88f, 34.04f, 254.43f),
            GatheringType = 3,
            NodeSet = 6
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35146,
            Position = new Vector3 (461.14f, 35.08f, 268.28f),
            LandZone = new Vector3 (461.07f, 34.47f, 267.78f),
            GatheringType = 3,
            NodeSet = 6
        },

        #endregion

        #endregion

        #region B Rank Mission

        #region (506, 682, 100)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35151,
            Position = new Vector3 (559.91f, 55.79f, 672.96f),
            LandZone = new Vector3 (559.79f, 55.21f, 672.69f),
            GatheringType = 3,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35158,
            Position = new Vector3 (536.1f, 55.63f, 679.65f),
            LandZone = new Vector3 (536.37f, 54.9f, 679.87f),
            GatheringType = 3,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35154,
            Position = new Vector3 (520.14f, 56.14f, 694.22f),
            LandZone = new Vector3 (520.91f, 55.61f, 693.97f),
            GatheringType = 3,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35155,
            Position = new Vector3 (502.04f, 56.63f, 680.56f),
            LandZone = new Vector3 (502.08f, 55.68f, 680.68f),
            GatheringType = 3,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35157,
            Position = new Vector3 (481.21f, 56.12f, 660.01f),
            LandZone = new Vector3 (481.17f, 55.23f, 660.35f),
            GatheringType = 3,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35156,
            Position = new Vector3 (489.9f, 56.31f, 671.44f),
            LandZone = new Vector3 (490.2f, 55.49f, 671.69f),
            GatheringType = 3,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35152,
            Position = new Vector3 (464.35f, 55.87f, 661.57f),
            LandZone = new Vector3 (464.23f, 55.16f, 661.27f),
            GatheringType = 3,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35153,
            Position = new Vector3 (452.62f, 56.02f, 663.78f),
            LandZone = new Vector3 (452.79f, 55.22f, 663.5f),
            GatheringType = 3,
            NodeSet = 7
        },

        #endregion

        #region B-2 ?? ?? ??

        #endregion

        #endregion
    };

    public class VislandInfo
    {
        public Vector3 MapCords { get; set; }
        public Vector3 StartPosition { get; set; }
        public string VBase64 { get; set; }
    }

    public static Dictionary<uint, VislandInfo> VislandDict = new()
    {
        {1, new VislandInfo
        {

        } },
        {2, new VislandInfo
        {
            MapCords = new Vector3 (224, 82, 100),
            StartPosition = new Vector3 (222, 19.5f, 9.5f),
            VBase64 = "H4sIAAAAAAAACu2WUW+bMBDHv0p0z56FbYyN37qsrbIpWdZEypppD25xAxLYGZhOVZTvPhlI12x7zMuiPPnuMHd/3/1k2MFMVwYU3LnWmxEdvRu9X84AwW3t2i0oWBS2bUZXdebqtgIEN85loCIEU21bXXbmUtcb42+1z0098abqgiv9snWF9Q2obzuYu6bwhbOgdvAVFKUMx0zGCO5B0QhTnhCBYA1K4pgSLtkewdpZM/kAilAmENzprGgbUBSH4u7ZVMb6rtJc+/ypsBkoX7cGwcR6U+tHvyp8/jkkYJzw9Dg+nLpsm3z0bDbG66ButNX+MYfjrX9Ij0L9+2Fdd+seQZO7n4eXCmcbUE+6bN6I6RIQBNeV892pIgShV4N51e0YnC+tafxbe2F+9D13D0N44d127Gw2KIsQfCrKcuzaoSfdOIdDAoJxrv3YVZUOXQqBoHelC/9baPBuXH2cNASXRWWmzZF7vfy7GXsEk2aea+td9Zo0jAaUbcsSwcyYrJn2CofHPTKF3SxftgYUCylmLjOHSQb7o3sARcQe/ROiVApxgIiRKDhrUIRjyeI4OTFESZjZBaLzgkhyTFORdgqIxFJwNkAUSUzi6OQQBYEXiM4MogQzxpMeohRHVKS8h4jEOBGEnxqi+ALR2UEUCZzI6PUmSinlw00UcxwnND41RPQC0blBRGSCKQvfmP4m4kLQwz8RxTRJ5akhIheI3H8N0ff9LxqewCwFDgAA"
        } },
        {3, new VislandInfo
        {
            MapCords = new Vector3 (231, -50, 100),
            StartPosition = new Vector3 (223.1f, 19.3f, 1),
            VBase64 = "H4sIAAAAAAAACu2XTU/cMBCG/8pqzsaKPxLHvtEtoG3FlsJKFKoeDDEkUmJvE4cKof3vyImhUK65dJWTxxNn/HrmUcZ5grVuDCg4d703C7Y4WHzarAHBSev6LSi4qGzfLQ7bwrV9AwiOnStAJQhOte11PZgb3d4bf6J9adqVN83gvNSPW1dZ34H6+QRnrqt85SyoJ/gBilKCJSEMwRUommDCGYJrUARTShKZ7hBcO2tWn0ERygSCc11UfQeK4rC1ezCNsX7Y50z78q6yBSjf9gbBynrT6lt/WfnyWwjAUhIivPXHM9d9Vy4ezL3xOmhbbLW/LeH90n+EJ2H/qzheD+MOQVe6Py8vVc52oO503b0RMwQgCI4a54dTJQhCpqJ5OKyIk++96fxb+8L8HjPubqL7wrvt0tkiKksQfK3qeun6mJOhmPGQgGBZar90TaNDloIj6L3Ulf8rNMyOXfs+aHBuqsacdu+mR5uPydghWHVnpbbeNa9BQ2lA2b6uEayNKbrTUWF8PAJT2fvN49aAYiHE2hXmpZLB/uJuQBGxQx8RIhwzIrIXhBJBmBxqciAxoYwKMjVF2UzR/lFEMSd5OiggOc4pS9KRIsowpyxhE1PEyUzR3lHECBYsy0eKJM5lNvazAyqwoGzqfsZDyWaG9oshznHK83glIphmgsUvEZOY80yKqfuZnCnaO4pSgvNUkpEiirnMMjFSlHJMhUiyqSnKZ4r2jiKWYxYZIphTzvnIUMaxJLngU/czOjO0dwzRFGcJf/0/EzSTsZ/lDCcpzyfvZ+lMkfuvKfq1ewZ86qJBkBIAAA=="
        } },
    };
}
