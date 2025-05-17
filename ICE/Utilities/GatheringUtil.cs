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
        { "YieldI", new GatheringActions
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
        { "YieldII", new GatheringActions
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
        { "BountifulYieldII", new GatheringActions
        {
            ActionName = "Bountiful Yield/Harvest II",
            BtnName = "",
            BtnActionId = 273,
            MinName = "",
            MinActionId = 272,
            StatusId = 1286,
            StatusName = "",
            RequiredGp = 100,
        }},
    };

   /* First things first, there's several types of missions for gathering
    * 1 Quantity Limited(Gather x amount on limited amount of nodes)
    * 2 Quantity(Gather x amount, gather more for increased score)
    * 3 Timed(Gather x amount in the time limit)
    * 4 Chain(Increase score based on chain)
    * 5 Gatherer's Boon (Increase score by hitting boon % chance)
    * 6 Chain + Boon(Get score from chain nodes + boon % chance)
    * 7 Collectables(This is going to be annoying)
    * 8 Time Steller Reduction(???) (Assuming Collectables -> Reducing for score...fuck)
    */
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
        // Btn Missions
        
        // D Rank
        { 406, new GatheringInfo { NodeSet = 3, Type = 1} },
        { 407, new GatheringInfo { NodeSet = 3, Type = 1} },
        { 408, new GatheringInfo { NodeSet = 2, Type = 3} },
        { 409, new GatheringInfo { NodeSet = 1, Type = 3} },
        { 410, new GatheringInfo { NodeSet = 2, Type = 2} },
        { 411, new GatheringInfo { NodeSet = 1, Type = 2} },

        // C Rank
        { 412, new GatheringInfo { NodeSet = 4, Type = 1} },
        { 413, new GatheringInfo { NodeSet = 5, Type = 3} },
        { 414, new GatheringInfo { NodeSet = 6, Type = 2} },
        { 415, new GatheringInfo { NodeSet = 5, Type = 4} },
        { 416, new GatheringInfo { NodeSet = 5, Type = 5} },
        { 417, new GatheringInfo { NodeSet = 6, Type = 6} },
        // { 418, new GatheringInfo { NodeSet = 6, Type = 7} }, // Collectable

        // Min Missions

        // D Rank

        { 361, new GatheringInfo { NodeSet = 1, Type = 1 } },
        { 362, new GatheringInfo { NodeSet = 1, Type = 1 } },
        { 363, new GatheringInfo { NodeSet = 2, Type = 3 } },
        { 364, new GatheringInfo { NodeSet = 3, Type = 3 } },
        { 365, new GatheringInfo { NodeSet = 2, Type = 2 } },
        { 366, new GatheringInfo { NodeSet = 3, Type = 2 } },

        // C Rank

        { 367, new GatheringInfo { NodeSet = 4, Type = 1 } },
        { 368, new GatheringInfo { NodeSet = 5, Type = 3 } },
        { 369, new GatheringInfo { NodeSet = 6, Type = 2 } },
        { 370, new GatheringInfo { NodeSet = 5, Type = 4 } },
        { 371, new GatheringInfo { NodeSet = 5, Type = 5 } },
        { 372, new GatheringInfo { NodeSet = 6, Type = 6 } },
        // { 373, new GatheringInfo { NodeSet = 6, Type = 7 } }, // Collectable

        // B Rank 
        { 374, new GatheringInfo { NodeSet = 7, Type = 1 } },
        { 375, new GatheringInfo { NodeSet = 8, Type = 3 } },
        { 376, new GatheringInfo { NodeSet = 9, Type = 4 } },
        { 377, new GatheringInfo { NodeSet = 8, Type = 4 } },
        { 378, new GatheringInfo { NodeSet = 9, Type = 5 } },
        { 379, new GatheringInfo { NodeSet = 8, Type = 6 } },
        // { 380, new GatheringInfo { NodeSet = 8, Type = 7 } },
        // { 381, new GatheringInfo { NodeSet = 9, Type = 8 } },

        // A Rank
        { 382, new GatheringInfo { NodeSet = 10, Type = 1} },
        { 383, new GatheringInfo { NodeSet = 6, Type = 3} },
        { 384, new GatheringInfo { NodeSet = 5, Type = 2} },
        { 385, new GatheringInfo { NodeSet = 3, Type = 4} },
        { 386, new GatheringInfo { NodeSet = 6, Type = 5} },
        { 387, new GatheringInfo { NodeSet = 4, Type = 1} },
        // { 388, new GatheringInfo { NodeSet = 2, Type = 8} }, // Cosmic Reduce
        // { 389, new GatheringInfo { NodeSet = } } // New Area
        // { 391, new GatheringInfo { NodeSet = 8, Type = 8} } // Cosmic Reduce
        { 393, new GatheringInfo { NodeSet = 1, Type = 1} },
        { 394, new GatheringInfo { NodeSet = 6, Type = 4} },
        { 395, new GatheringInfo { NodeSet = 5, Type = 2} },
        { 396, new GatheringInfo { NodeSet = 8, Type = 5} },
        // { 397, new GatheringInfo { NodeSet = 9, Type = 7} },
        { 398, new GatheringInfo { NodeSet = 6, Type = 2} },
        { 400, new GatheringInfo { NodeSet = 3, Type = 3} },
        // { 402, new GatheringInfo { } } // SW Sector
        // { 404, new GatheringInfo { } } // SW Sector
        // { 405, new GatheringInfo { } } // SW Sector
    };

    public static Dictionary<Vector2, uint> Nodeset = new()
    {
        // Miner Set
        { new Vector2(-119, -175), 1 },
        { new Vector2(-168, -181), 2 },
        { new Vector2(96, 259), 3 },
        { new Vector2(65, -431), 4 },
        { new Vector2(65, -431), 5 },
        { new Vector2(73, -482), 6 },
        { new Vector2(-463, -729), 7 },
        { new Vector2(-690, -752), 8 },
        { new Vector2(-669, -515), 9 },
        { new Vector2(-463, -729), 10 },

        // Botanist Set
        { new Vector2(-278, -13), 11 },
        { new Vector2(225, 83), 12 },
        { new Vector2(232, -50), 13 },
        { new Vector2(456, 221), 14 },
        { new Vector2(-121, 368), 15 },
        { new Vector2(455, 243), 16 },
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

        // Botanist

        #region (Set #11)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35168,
            Position = new Vector3 (-228.04f, 17.31f, -13.59f),
            LandZone = new Vector3 (-228.84f, 16.82f, -13.53f),
            GatheringType = 3,
            NodeSet = 1
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35167,
            Position = new Vector3 (-238.16f, 17.82f, -33.92f),
            LandZone = new Vector3 (-237.94f, 17.22f, -33.02f),
            GatheringType = 3,
            NodeSet = 11
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35166,
            Position = new Vector3 (-325.44f, 28.14f, -51.83f),
            LandZone = new Vector3 (-324.69f, 27.81f, -51.55f),
            GatheringType = 3,
            NodeSet = 11
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35165,
            Position = new Vector3 (-330.45f, 29.18f, -28.82f),
            LandZone = new Vector3 (-330.08f, 28.69f, -29.41f),
            GatheringType = 3,
            NodeSet = 11
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35169,
            Position = new Vector3 (-288.95f, 25.49f, 44.38f),
            LandZone = new Vector3 (-288.93f, 25.07f, 43.86f),
            GatheringType = 3,
            NodeSet = 11
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35170,
            Position = new Vector3 (-273.81f, 23.78f, 44.75f),
            LandZone = new Vector3 (-274.15f, 23.28f, 44.57f),
            GatheringType = 3,
            NodeSet = 11
        },

        #endregion

        #region (Set #12)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35159,
            Position = new Vector3 (223.44f, 20.26f, 8.42f),
            LandZone = new Vector3 (223.04f, 19.41f, 8.25f),
            GatheringType = 3,
            NodeSet = 12
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35160,
            Position = new Vector3 (223.99f, 20.31f, 15.83f),
            LandZone = new Vector3 (224.33f, 19.39f, 15.45f),
            GatheringType = 3,
            NodeSet = 12
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35163,
            Position = new Vector3 (285.3f, 18.88f, 108.14f),
            LandZone = new Vector3 (285.2f, 17.89f, 108.78f),
            GatheringType = 3,
            NodeSet = 12
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35164,
            Position = new Vector3 (286.34f, 19.03f, 114.67f),
            LandZone = new Vector3 (286.04f, 18.02f, 114.53f),
            GatheringType = 3,
            NodeSet = 12
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35162,
            Position = new Vector3 (207.68f, 18.92f, 145.46f),
            LandZone = new Vector3 (207.74f, 17.93f, 145.63f),
            GatheringType = 3,
            NodeSet = 12
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35161,
            Position = new Vector3 (186.24f, 19.58f, 152.27f),
            LandZone = new Vector3 (186.34f, 18.68f, 152.56f),
            GatheringType = 3,
            NodeSet = 12
        },

        #endregion

        #region (Set #13)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35137,
            Position = new Vector3 (221.91f, 20.14f, 1.22f),
            LandZone = new Vector3 (221.63f, 19.27f, 1.76f),
            GatheringType = 3,
            NodeSet = 13
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35136,
            Position = new Vector3 (214.32f, 20.07f, -9.12f),
            LandZone = new Vector3 (214.58f, 19.17f, -9.09f),
            GatheringType = 3,
            NodeSet = 13
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35141,
            Position = new Vector3 (212.42f, 18.82f, -23.42f),
            LandZone = new Vector3 (212.86f, 18.07f, -22.78f),
            GatheringType = 3,
            NodeSet = 13
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35140,
            Position = new Vector3 (231.74f, 19.9f, -27.72f),
            LandZone = new Vector3 (231.49f, 18.96f, -27.89f),
            GatheringType = 3,
            NodeSet = 13
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35139,
            Position = new Vector3 (244.55f, 21.27f, -39.45f),
            LandZone = new Vector3 (244.63f, 20.4f, -39.56f),
            GatheringType = 3,
            NodeSet = 13
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35138,
            Position = new Vector3 (251.86f, 22.5f, -54.28f),
            LandZone = new Vector3 (252.26f, 21.65f, -54.21f),
            GatheringType = 3,
            NodeSet = 13
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35142,
            Position = new Vector3 (238.31f, 21.42f, -64.92f),
            LandZone = new Vector3 (238.52f, 20.47f, -65.07f),
            GatheringType = 3,
            NodeSet = 13
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35135,
            Position = new Vector3 (225.6f, 20.73f, -83.05f),
            LandZone = new Vector3 (225.59f, 19.73f, -82.86f),
            GatheringType = 3,
            NodeSet = 13
        },

        #endregion

        // Miner

        #region (Set #1) (8 Chain Node)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35040,
            Position = new Vector3 (-50.68f, 19.41f, -208.97f),
            LandZone = new Vector3 (-50.64f, 18.41f, -209.93f),
            GatheringType = 2,
            NodeSet = 1
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35039,
            Position = new Vector3 (-73.1f, 20.16f, -204.29f),
            LandZone = new Vector3 (-73.45f, 19.12f, -205.25f),
            GatheringType = 2,
            NodeSet = 1
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35038,
            Position = new Vector3 (-90.95f, 22.52f, -194.11f),
            LandZone = new Vector3 (-91.03f, 21.15f, -194.67f),
            GatheringType = 2,
            NodeSet = 1
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35037,
            Position = new Vector3 (-109.77f, 24.82f, -187.37f),
            LandZone = new Vector3 (-109.83f, 23.74f, -188.08f),
            GatheringType = 2,
            NodeSet = 1
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35036,
            Position = new Vector3 (-129.53f, 27.94f, -170.34f),
            LandZone = new Vector3 (-129.85f, 26.59f, -170.81f),
            GatheringType = 2,
            NodeSet = 1
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35035,
            Position = new Vector3 (-135.65f, 28.2f, -156.82f),
            LandZone = new Vector3 (-135.94f, 27f, -157.07f),
            GatheringType = 2,
            NodeSet = 1
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35034,
            Position = new Vector3 (-142.32f, 27.14f, -139.7f),
            LandZone = new Vector3 (-142.57f, 25.67f, -140.04f),
            GatheringType = 2,
            NodeSet = 1
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35033,
            Position = new Vector3 (-153.94f, 24.31f, -124.47f),
            LandZone = new Vector3 (-154.36f, 23.29f, -124.61f),
            GatheringType = 2,
            NodeSet = 1
        },

        #endregion

        #region (Set #2) (Close to 8 chain)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35062,
            Position = new Vector3 (-110.86f, 20.37f, -226.34f),
            LandZone = new Vector3 (-110.73f, 19.3f, -225.31f),
            GatheringType = 2,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35061,
            Position = new Vector3 (-117.08f, 20.9f, -230.27f),
            LandZone = new Vector3 (-118.01f, 20f, -230.48f),
            GatheringType = 2,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35057,
            Position = new Vector3 (-226.12f, 29.29f, -176.71f),
            LandZone = new Vector3 (-225.34f, 28.75f, -176.68f),
            GatheringType = 2,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35058,
            Position = new Vector3 (-228.37f, 27.66f, -167.48f),
            LandZone = new Vector3 (-227.69f, 27.39f, -167.57f),
            GatheringType = 2,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35059,
            Position = new Vector3 (-170.4f, 20.94f, -116.41f),
            LandZone = new Vector3 (-170.99f, 20.28f, -116.77f),
            GatheringType = 2,
            NodeSet = 2
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35060,
            Position = new Vector3 (-162.69f, 22.16f, -124.16f),
            LandZone = new Vector3 (-162.84f, 21.93f, -125.09f),
            GatheringType = 2,
            NodeSet = 2
        },

        #endregion

        #region (Set #3) (FARRR Away)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35066,
            Position = new Vector3 (60.94f, 22.63f, 204.94f),
            LandZone = new Vector3 (60.95f, 21.53f, 205.38f),
            GatheringType = 2,
            NodeSet = 3
        },

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35065,
            Position = new Vector3 (40.95f, 21.06f, 208.7f),
            LandZone = new Vector3 (41.39f, 19.97f, 209.17f),
            GatheringType = 2,
            NodeSet = 3
        },

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35063,
            Position = new Vector3 (65.52f, 18.94f, 318.76f),
            LandZone = new Vector3 (65.6f, 18.21f, 318.18f),
            GatheringType = 2,
            NodeSet = 3
        },

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35064,
            Position = new Vector3 (75.12f, 19.39f, 322.87f),
            LandZone = new Vector3 (75.45f, 18.57f, 322.06f),
            GatheringType = 2,
            NodeSet = 3
        },

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35068,
            Position = new Vector3 (172.32f, 23.64f, 275.08f),
            LandZone = new Vector3 (172.16f, 22.95f, 274.95f),
            GatheringType = 2,
            NodeSet = 3
        },

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35067,
            Position = new Vector3 (174.37f, 23.41f, 267.84f),
            LandZone = new Vector3 (174.24f, 22.82f, 267.61f),
            GatheringType = 2,
            NodeSet = 3
        },


#endregion

        #endregion

        #region C Rank Missions

        // Botanist

        #region (Set #14)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35143,
            Position = new Vector3 (421.1f, 33.1f, 189.18f),
            LandZone = new Vector3 (421.21f, 32.62f, 189.4f),
            GatheringType = 3,
            NodeSet = 14
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35150,
            Position = new Vector3 (441.48f, 33.91f, 186.57f),
            LandZone = new Vector3 (441.11f, 33.22f, 186.48f),
            GatheringType = 3,
            NodeSet = 14
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35147,
            Position = new Vector3 (439.68f, 34.32f, 211f),
            LandZone = new Vector3 (439.92f, 33.71f, 210.52f),
            GatheringType = 3,
            NodeSet = 14
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35148,
            Position = new Vector3 (460.41f, 34.31f, 206.39f),
            LandZone = new Vector3 (460.12f, 33.59f, 206.25f),
            GatheringType = 3,
            NodeSet = 14
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35149,
            Position = new Vector3 (468.54f, 34.92f, 216.16f),
            LandZone = new Vector3 (468.2f, 34.34f, 216.77f),
            GatheringType = 3,
            NodeSet = 14
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35144,
            Position = new Vector3 (459.23f, 34.89f, 234.63f),
            LandZone = new Vector3 (459.71f, 34.33f, 234.41f),
            GatheringType = 3,
            NodeSet = 14
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35145,
            Position = new Vector3 (454.67f, 34.75f, 254.14f),
            LandZone = new Vector3 (454.88f, 34.04f, 254.43f),
            GatheringType = 3,
            NodeSet = 14
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35146,
            Position = new Vector3 (461.14f, 35.08f, 268.28f),
            LandZone = new Vector3 (461.07f, 34.47f, 267.78f),
            GatheringType = 3,
            NodeSet = 14
        },

        #endregion

        #region (Set #15)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35176,
            Position = new Vector3 (-185.84f, 32.88f, 351.45f),
            LandZone = new Vector3 (-185.52f, 32.05f, 351.6f),
            GatheringType = 3,
            NodeSet = 15
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35175,
            Position = new Vector3 (-174.21f, 32.41f, 342.16f),
            LandZone = new Vector3 (-174.13f, 31.51f, 342.22f),
            GatheringType = 3,
            NodeSet = 15
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35171,
            Position = new Vector3 (-83.13f, 28.26f, 312.21f),
            LandZone = new Vector3 (-83.36f, 27.38f, 311.6f),
            GatheringType = 3,
            NodeSet = 15
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35172,
            Position = new Vector3 (-69.83f, 27.94f, 330.99f),
            LandZone = new Vector3 (-69.73f, 27.02f, 330.61f),
            GatheringType = 3,
            NodeSet = 15
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35173,
            Position = new Vector3 (-110.16f, 34.36f, 427.53f),
            LandZone = new Vector3 (-110.27f, 33.45f, 428.06f),
            GatheringType = 3,
            NodeSet = 15
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35174,
            Position = new Vector3 (-116.48f, 34.47f, 438.21f),
            LandZone = new Vector3 (-116.7f, 33.51f, 437.75f),
            GatheringType = 3,
            NodeSet = 15
        },

        #endregion

        #region (Set #16)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35177,
            Position = new Vector3 (490.19f, 36.57f, 174.08f),
            LandZone = new Vector3 (489.71f, 35.7f, 174.19f),
            GatheringType = 3,
            NodeSet = 16
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35178,
            Position = new Vector3 (441.94f, 36.48f, 176.07f),
            LandZone = new Vector3 (441.74f, 35.5f, 176.29f),
            GatheringType = 3,
            NodeSet = 16
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35180,
            Position = new Vector3 (382.5f, 35.42f, 265.39f),
            LandZone = new Vector3 (383.06f, 35.01f, 264.97f),
            GatheringType = 3,
            NodeSet = 16
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35179,
            Position = new Vector3 (395.4f, 37.31f, 276.67f),
            LandZone = new Vector3 (395.65f, 36.48f, 276.62f),
            GatheringType = 3,
            NodeSet = 16
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35181,
            Position = new Vector3 (476.83f, 40.68f, 297.1f),
            LandZone = new Vector3 (477.28f, 39.93f, 297.15f),
            GatheringType = 3,
            NodeSet = 16
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35182,
            Position = new Vector3 (510.84f, 37.4f, 283.41f),
            LandZone = new Vector3 (510.55f, 36.63f, 283.3f),
            GatheringType = 3,
            NodeSet = 16
        },

        #endregion

        // Miner 

        #region (Set #4)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35041,
            Position = new Vector3 (70.23f, 35.63f, -370.83f),
            LandZone = new Vector3 (69.72f, 35.02f, -370.42f),
            GatheringType = 2,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35042,
            Position = new Vector3 (56.98f, 36.75f, -385.37f),
            LandZone = new Vector3 (57.49f, 35.95f, -384.87f),
            GatheringType = 2,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35043,
            Position = new Vector3 (78.47f, 39.5f, -424.33f),
            LandZone = new Vector3 (77.97f, 39.06f, -424.98f),
            GatheringType = 2,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35044,
            Position = new Vector3 (56.85f, 40.02f, -444.27f),
            LandZone = new Vector3 (57.26f, 39.27f, -444f),
            GatheringType = 2,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35045,
            Position = new Vector3 (56.59f, 40.24f, -453.96f),
            LandZone = new Vector3 (57.03f, 39.6f, -454.24f),
            GatheringType = 2,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35046,
            Position = new Vector3 (45.23f, 41.43f, -473.65f),
            LandZone = new Vector3 (45.55f, 40.41f, -473.4f),
            GatheringType = 2,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35047,
            Position = new Vector3 (49.7f, 41.83f, -481.26f),
            LandZone = new Vector3 (50.06f, 40.96f, -481.57f),
            GatheringType = 2,
            NodeSet = 4
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35048,
            Position = new Vector3 (60.49f, 43.1f, -499.58f),
            LandZone = new Vector3 (60.52f, 42.33f, -499.64f),
            GatheringType = 2,
            NodeSet = 4
        },

        #endregion

        #region (Set #5)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35072,
            Position = new Vector3 (-430.47f, 42.53f, 96.48f),
            LandZone = new Vector3 (-430.96f, 42.04f, 96.05f),
            GatheringType = 2,
            NodeSet = 5
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35071,
            Position = new Vector3 (-438.91f, 43.87f, 101.09f),
            LandZone = new Vector3 (-438.25f, 42.71f, 101.14f),
            GatheringType = 2,
            NodeSet = 5
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35074,
            Position = new Vector3 (-543.83f, 41.39f, 78.79f),
            LandZone = new Vector3 (-543.35f, 40.24f, 78.7f),
            GatheringType = 2,
            NodeSet = 5
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35073,
            Position = new Vector3 (-542.48f, 44.36f, 91.48f),
            LandZone = new Vector3 (-542.08f, 43.15f, 91.97f),
            GatheringType = 2,
            NodeSet = 5
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35070,
            Position = new Vector3 (-400.24f, 45f, 191.3f),
            LandZone = new Vector3 (-400.94f, 44.26f, 190.28f),
            GatheringType = 2,
            NodeSet = 5
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35069,
            Position = new Vector3 (-394.25f, 44.13f, 189.71f),
            LandZone = new Vector3 (-394.35f, 43.28f, 188.94f),
            GatheringType = 2,
            NodeSet = 5
        },

        #endregion

        #region (Set #6)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35078,
            Position = new Vector3 (77.56f, 39.48f, -424f),
            LandZone = new Vector3 (76.97f, 38.91f, -424.55f),
            GatheringType = 2,
            NodeSet = 6
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35077,
            Position = new Vector3 (87.1f, 39.77f, -424.88f),
            LandZone = new Vector3 (87.04f, 39.23f, -425.47f),
            GatheringType = 2,
            NodeSet = 6
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35076,
            Position = new Vector3 (142.59f, 47.46f, -491.28f),
            LandZone = new Vector3 (142.78f, 46.57f, -490.84f),
            GatheringType = 2,
            NodeSet = 6
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35075,
            Position = new Vector3 (135.91f, 47.36f, -500.54f),
            LandZone = new Vector3 (135.63f, 46.7f, -500.5f),
            GatheringType = 2,
            NodeSet = 6
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35082,
            Position = new Vector3 (-752.37f, 88.51f, -717.92f),
            LandZone = new Vector3 (-751.33f, 87.55f, -718.4f),
            GatheringType = 2,
            NodeSet = 8
        },

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35080,
            Position = new Vector3 (15.94f, 44.39f, -526.71f),
            LandZone = new Vector3 (15.66f, 43.36f, -526.49f),
            GatheringType = 2,
            NodeSet = 6
        },

        #endregion

        #endregion

        #region B Rank Mission

        // BTN

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

        // Miner

        #region (Set #7)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35056,
            Position = new Vector3 (-419.94f, 66.8f, -692.3f),
            LandZone = new Vector3 (-420.07f, 66.15f, -691.43f),
            GatheringType = 2,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35055,
            Position = new Vector3 (-428.39f, 67.89f, -704.01f),
            LandZone = new Vector3 (-428.84f, 67.15f, -703.4f),
            GatheringType = 2,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35054,
            Position = new Vector3 (-447.28f, 68.51f, -707.15f),
            LandZone = new Vector3 (-446.41f, 67.61f, -707.17f),
            GatheringType = 2,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35053,
            Position = new Vector3 (-461.66f, 69.71f, -713.83f),
            LandZone = new Vector3 (-462.18f, 68.99f, -713.69f),
            GatheringType = 2,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35052,
            Position = new Vector3 (-462.91f, 71.27f, -731.6f),
            LandZone = new Vector3 (-463.43f, 70.55f, -731.26f),
            GatheringType = 2,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35051,
            Position = new Vector3 (-467.54f, 73.48f, -747.74f),
            LandZone = new Vector3 (-468f, 72.64f, -747.89f),
            GatheringType = 2,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35050,
            Position = new Vector3 (-469.04f, 76.77f, -770.29f),
            LandZone = new Vector3 (-469.29f, 76.02f, -769.88f),
            GatheringType = 2,
            NodeSet = 7
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35049,
            Position = new Vector3 (-492.64f, 78.8f, -777f),
            LandZone = new Vector3 (-492.64f, 78.01f, -776.5f),
            GatheringType = 2,
            NodeSet = 7
        },

        #endregion

        #region (Set #8)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35086,
            Position = new Vector3 (-635.22f, 73.97f, -704.67f),
            LandZone = new Vector3 (-635.61f, 73.15f, -704.04f),
            GatheringType = 2,
            NodeSet = 8
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35085,
            Position = new Vector3 (-621.59f, 75.08f, -715.89f),
            LandZone = new Vector3 (-621.8f, 74.06f, -716.89f),
            GatheringType = 2,
            NodeSet = 8
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35084,
            Position = new Vector3 (-671.18f, 93.37f, -819.39f),
            LandZone = new Vector3 (-670.57f, 92.57f, -819.02f),
            GatheringType = 2,
            NodeSet = 8
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35083,
            Position = new Vector3 (-679.34f, 91.67f, -804.68f),
            LandZone = new Vector3 (-678.57f, 90.89f, -804.33f),
            GatheringType = 2,
            NodeSet = 8
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35082,
            Position = new Vector3 (-752.37f, 88.51f, -717.92f),
            LandZone = new Vector3 (-751.92f, 87.59f, -717.87f),
            GatheringType = 2,
            NodeSet = 8
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35081,
            Position = new Vector3 (-758.07f, 88.73f, -707.39f),
            LandZone = new Vector3 (-757.45f, 87.93f, -707.14f),
            GatheringType = 2,
            NodeSet = 8
        },

        #endregion

        #region (Set #9)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35091,
            Position = new Vector3 (-642.11f, 69.73f, -572.83f),
            LandZone = new Vector3 (-641.41f, 68.81f, -572.6f),
            GatheringType = 2,
            NodeSet = 9
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35092,
            Position = new Vector3 (-652.4f, 71.72f, -564.69f),
            LandZone = new Vector3 (-652.33f, 70.89f, -564.22f),
            GatheringType = 2,
            NodeSet = 9
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35089,
            Position = new Vector3 (-731.63f, 79.66f, -509.58f),
            LandZone = new Vector3 (-731.27f, 78.7f, -509.72f),
            GatheringType = 2,
            NodeSet = 9
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35090,
            Position = new Vector3 (-727.81f, 79.13f, -503.01f),
            LandZone = new Vector3 (-727.68f, 78.05f, -502.8f),
            GatheringType = 2,
            NodeSet = 9
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35087,
            Position = new Vector3 (-640.96f, 60.56f, -463.86f),
            LandZone = new Vector3 (-640.66f, 59.77f, -463.8f),
            GatheringType = 2,
            NodeSet = 9
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35088,
            Position = new Vector3 (-637.53f, 59.94f, -456.5f),
            LandZone = new Vector3 (-637.1f, 58.91f, -456.96f),
            GatheringType = 2,
            NodeSet = 9
        },


        #endregion

        #region

        #endregion

        #endregion

        #region A Rank Missions

        // BTN

        // MIN

        #region (Set #10)

        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35056,
            Position = new Vector3 (-419.94f, 66.8f, -692.3f),
            LandZone = new Vector3 (-420.31f, 66.21f, -691.75f),
            GatheringType = 2,
            NodeSet = 10
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35055,
            Position = new Vector3 (-428.39f, 67.89f, -704.01f),
            LandZone = new Vector3 (-428.94f, 67.19f, -703.73f),
            GatheringType = 2,
            NodeSet = 10
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35054,
            Position = new Vector3 (-447.28f, 68.51f, -707.15f),
            LandZone = new Vector3 (-447.13f, 67.63f, -707.23f),
            GatheringType = 2,
            NodeSet = 10
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35053,
            Position = new Vector3 (-461.66f, 69.71f, -713.83f),
            LandZone = new Vector3 (-462.13f, 69f, -713.8f),
            GatheringType = 2,
            NodeSet = 10
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35052,
            Position = new Vector3 (-462.91f, 71.27f, -731.6f),
            LandZone = new Vector3 (-463.28f, 70.55f, -731.19f),
            GatheringType = 2,
            NodeSet = 10
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35051,
            Position = new Vector3 (-467.54f, 73.48f, -747.74f),
            LandZone = new Vector3 (-467.68f, 72.61f, -747.86f),
            GatheringType = 2,
            NodeSet = 10
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35050,
            Position = new Vector3 (-469.04f, 76.77f, -770.29f),
            LandZone = new Vector3 (-469.35f, 76.03f, -769.95f),
            GatheringType = 2,
            NodeSet = 10
        },
        new GathNodeInfo
        {
            ZoneId = 1237,
            NodeId = 35049,
            Position = new Vector3 (-492.64f, 78.8f, -777f),
            LandZone = new Vector3 (-492.66f, 78.02f, -776.67f),
            GatheringType = 2,
            NodeSet = 10
        },


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
