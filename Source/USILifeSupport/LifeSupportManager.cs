using System;
using System.Collections.Generic;
using System.Linq;
using KolonyTools;
using UnityEngine;



namespace LifeSupport
{
    public class LifeSupportManager : MonoBehaviour
    {
        // Static singleton instance
        private static LifeSupportManager instance;

        // Static singleton property
        public static LifeSupportManager Instance
        {
            get { return instance ?? (instance = new GameObject("LifeSupportManager").AddComponent<LifeSupportManager>()); }
        }

        //Backing variables
        private List<LifeSupportStatus> _LifeSupportInfo;
        private List<VesselSupplyStatus> _VesselSupplyInfo;

        public void ResetCache()
        {
            _LifeSupportInfo = null;
            _VesselSupplyInfo = null;
        }

        public List<LifeSupportStatus> LifeSupportInfo
        {
            get
            {
                if (_LifeSupportInfo == null)
                {
                    _LifeSupportInfo = new List<LifeSupportStatus>();
                    _LifeSupportInfo.AddRange(LifeSupportScenario.Instance.settings.GetStatusInfo());
                }
                return _LifeSupportInfo;
            }
        }

        public List<VesselSupplyStatus> VesselSupplyInfo
        {
            get
            {
                if (_VesselSupplyInfo == null)
                {
                    _VesselSupplyInfo = new List<VesselSupplyStatus>();
                    _VesselSupplyInfo.AddRange(LifeSupportScenario.Instance.settings.GetVesselInfo());
                }
                return _VesselSupplyInfo;
            }
        }

        public bool IsKerbalTracked(string kname)
        {
            //Does a node exist?
            return LifeSupportInfo.Any(n => n.KerbalName == kname);
        }

        public bool IsVesselTracked(string vesselId)
        {
            //Does a node exist?
            return VesselSupplyInfo.Any(n => n.VesselId == vesselId);
        }
        
        public void UntrackKerbal(string kname)
        {
            if (!IsKerbalTracked(kname))
                return;
            var kerbal = LifeSupportInfo.First(k => k.KerbalName == kname);
            LifeSupportInfo.Remove(kerbal);
            //For saving to our scenario data
            LifeSupportScenario.Instance.settings.DeleteStatusNode(kerbal);
        }
        public LifeSupportStatus FetchKerbal(ProtoCrewMember crew)
        {
            if (!IsKerbalTracked(crew.name))
            {
                var k = new LifeSupportStatus();
                k.KerbalName = crew.name;
                k.LastMeal = Planetarium.GetUniversalTime();
                k.LastOnKerbin = Planetarium.GetUniversalTime();
                k.MaxOffKerbinTime = Planetarium.GetUniversalTime() + 972000000;
                k.TimeInVessel = 0d;
                k.LastVesselId = "??UNKNOWN??";
                k.LastUpdate = Planetarium.GetUniversalTime();
                k.IsGrouchy = false;
                k.OldTrait = crew.experienceTrait.Title;
                TrackKerbal(k);
            }

            var kerbal = LifeSupportInfo.FirstOrDefault(k => k.KerbalName == crew.name);
            return kerbal;
        }

        public void TrackKerbal(LifeSupportStatus status)
        {
            LifeSupportStatus kerbInfo =
                LifeSupportInfo.FirstOrDefault(n => n.KerbalName == status.KerbalName);
            if (kerbInfo == null)
            {
                kerbInfo = new LifeSupportStatus();
                kerbInfo.KerbalName = status.KerbalName;
                LifeSupportInfo.Add(kerbInfo);
            }
            kerbInfo.LastMeal = status.LastMeal;
            kerbInfo.LastOnKerbin = status.LastOnKerbin;
            kerbInfo.MaxOffKerbinTime = status.MaxOffKerbinTime;
            kerbInfo.LastVesselId = status.LastVesselId;
            kerbInfo.TimeInVessel = status.TimeInVessel;
            kerbInfo.LastUpdate = status.LastUpdate;
            kerbInfo.IsGrouchy = status.IsGrouchy;
            kerbInfo.OldTrait = status.OldTrait;
            LifeSupportScenario.Instance.settings.SaveStatusNode(status);
        }

        public void TrackVessel(VesselSupplyStatus status)
        {
            VesselSupplyStatus vesselInfo =
                VesselSupplyInfo.FirstOrDefault(n => n.VesselId == status.VesselId);
            if (vesselInfo == null)
            {
                vesselInfo = new VesselSupplyStatus();
                vesselInfo.VesselId = status.VesselId;
                VesselSupplyInfo.Add(vesselInfo);
            }
            vesselInfo.VesselName = status.VesselName;
            vesselInfo.LastFeeding = status.LastFeeding;
            vesselInfo.LastUpdate = status.LastUpdate;
            vesselInfo.NumCrew = status.NumCrew;
            vesselInfo.RecyclerMultiplier = status.RecyclerMultiplier;
            vesselInfo.CrewCap = status.CrewCap;
            vesselInfo.ExtraHabSpace = status.ExtraHabSpace;
            vesselInfo.VesselHabMultiplier = status.VesselHabMultiplier;
            vesselInfo.SuppliesLeft = status.SuppliesLeft;
            LifeSupportScenario.Instance.settings.SaveVesselNode(vesselInfo);
        }

        public void UntrackVessel(string vesselId)
        {
            if (!IsVesselTracked(vesselId))
                return;
            var vInfo = VesselSupplyInfo.First(v => v.VesselId == vesselId);
            VesselSupplyInfo.Remove(vInfo);
            //For saving to our scenario data
            LifeSupportScenario.Instance.settings.DeleteVesselNode(vInfo);
        }
        public VesselSupplyStatus FetchVessel(string vesselId)
        {
            if (!IsVesselTracked(vesselId))
            {
                var v = new VesselSupplyStatus();
                v.LastFeeding = Planetarium.GetUniversalTime();
                v.LastUpdate = Planetarium.GetUniversalTime();
                v.NumCrew = 0;
                v.RecyclerMultiplier = 1;
                v.CrewCap = 0;
                v.VesselHabMultiplier = 0;
                v.ExtraHabSpace = 0;
                v.SuppliesLeft = 0f;
                v.VesselId = vesselId;
                v.VesselName = "??loading??";
                TrackVessel(v);
            }

            var vInfo = VesselSupplyInfo.FirstOrDefault(k => k.VesselId == vesselId);
            return vInfo;
        }


        public static bool isVet(string kName)
        {
            var firstname = kName.Replace(" Kerman", "");
            return (LifeSupportSetup.Instance.LSConfig.VetNames.Contains(firstname));
        }


        internal void UpdateVesselStats()
        {
            //Clear stuff that is gone.
            var badIDs = new List<string>();
            foreach (var vInfo in LifeSupportManager.Instance.VesselSupplyInfo)
            {
                var vsl = FlightGlobals.Vessels.FirstOrDefault(v => v.id.ToString() == vInfo.VesselId);
                if(vsl == null || vInfo.NumCrew == 0)
                {
                    badIDs.Add(vInfo.VesselId);
                }
            }

            foreach (var id in badIDs)
            {
                LifeSupportManager.Instance.UntrackVessel(id);
            }
        }

        private static int GetColonyCrewCount(Vessel vsl)
        {
            var crewCount = vsl.GetCrewCount();
            var vList = LogisticsTools.GetNearbyVessels((float)LifeSupportSetup.Instance.LSConfig.HabRange, false, vsl, true);
            foreach (var v in vList)
            {
                crewCount += v.GetCrewCount();
            }
            return crewCount;
        }

        internal static double GetRecyclerMultiplier(Vessel vessel)
        {
            if (!LifeSupportSetup.Instance.LSConfig.EnableRecyclers)
                return 1d;

            var recyclerCap = 0f;
            var recyclerVal = 1f;
            var crewCount = GetColonyCrewCount(vessel);

            foreach (var r in vessel.FindPartModulesImplementing<ModuleLifeSupportRecycler>())
            {
                if (r.IsActivated)
                {
                    if (r.RecyclePercent > recyclerCap)
                        recyclerCap = r.RecyclePercent;
                    var recPercent = r.RecyclePercent;
                    if (r.CrewCapacity < crewCount)
                        recPercent *= r.CrewCapacity/(float) crewCount;

                    recyclerVal *= (1f - recPercent);
                }
            }

            var vList = LogisticsTools.GetNearbyVessels((float)LifeSupportSetup.Instance.LSConfig.HabRange, false, vessel, true);
            foreach (var v in vList)
            {
                foreach (var r in v.FindPartModulesImplementing<ModuleLifeSupportRecycler>())
                {
                    if (r.IsActivated)
                    {
                        if (r.RecyclePercent > recyclerCap)
                            recyclerCap = r.RecyclePercent;
                        var recPercent = r.RecyclePercent;
                        if (r.CrewCapacity < crewCount)
                            recPercent *= r.CrewCapacity / (float)crewCount;

                        recyclerVal *= (1f - recPercent);
                    }
                }
            } 
            return Math.Max(recyclerVal, (1f - recyclerCap));
        }


        internal static double GetTotalHabTime(VesselSupplyStatus sourceVessel)
        {
            var vsl = FlightGlobals.Vessels.FirstOrDefault(v => v.id.ToString() == sourceVessel.VesselId);
            double totHabSpace = (LifeSupportSetup.Instance.LSConfig.BaseHabTime * sourceVessel.CrewCap) + sourceVessel.ExtraHabSpace;
            double totHabMult = sourceVessel.VesselHabMultiplier;
            double totCurCrew = sourceVessel.NumCrew;
            double totMaxCrew = sourceVessel.CrewCap;

            var vList = LogisticsTools.GetNearbyVessels((float)LifeSupportSetup.Instance.LSConfig.HabRange, false, vsl, true);
            foreach (var v in vList)
            {
                var curVsl = LifeSupportManager.Instance.FetchVessel(v.id.ToString());
                //Hab time starts with our baseline of the crew hab plus extra hab.
                //We then multiply it out based on the crew ratio, our global multiplier, and the vessel's multipler.
                //First - crew capacity. 
                totHabSpace += (LifeSupportSetup.Instance.LSConfig.BaseHabTime * curVsl.CrewCap) + curVsl.ExtraHabSpace;
                totCurCrew += curVsl.NumCrew;
                totMaxCrew += curVsl.CrewCap;
                totHabMult += curVsl.VesselHabMultiplier;
            }
            double habTotal = totHabSpace / totCurCrew * (totHabMult + 1) * LifeSupportSetup.Instance.LSConfig.HabMultiplier;
            //print(String.Format("THS: {0} TC:{1} THM: {2} HM: {3}", totHabSpace, totCurCrew, totHabMult, LifeSupportSetup.Instance.LSConfig.HabMultiplier));

            return habTotal * (60d * 60d * 6d * 30d);
        }

        internal static double GetRecyclerMultiplierForParts(List<Part> pList, int crewCount)
        {
            if (!LifeSupportSetup.Instance.LSConfig.EnableRecyclers)
                return 1d;

            var recyclerCap = 0f;
            var recyclerVal = 1f;

            foreach (var p in pList)
            {
                var mod = p.FindModuleImplementing<ModuleLifeSupportRecycler>();
                if (mod == null) 
                    continue;
                
                if (mod.RecyclePercent > recyclerCap)
                    recyclerCap = mod.RecyclePercent;
                var recPercent = mod.RecyclePercent;
                if (mod.CrewCapacity < crewCount)
                    recPercent *= mod.CrewCapacity / (float)crewCount;

                recyclerVal *= (1f - recPercent);
            }
            
            return Math.Max(recyclerVal, (1f - recyclerCap));
        }
        public static bool IsOnKerbin(Vessel v)
        {
            return (v.mainBody == FlightGlobals.GetHomeBody() && v.altitude < LifeSupportSetup.Instance.LSConfig.HomeWorldAltitude);
        }
    }
}

