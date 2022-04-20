using ICities;

namespace PopulationDemographics
{
    public class PopulationDemographicsThreading : ThreadingExtensionBase
    {
        /// <summary>
        /// called after every simulation tick, even when simulation is paused
        /// </summary>
        public override void OnAfterSimulationTick()
        {
            // do base processing
            base.OnAfterSimulationTick();

            // simulation tick processing is performed in the panel logic
            // OnAfterSimulationTick WILL be executed before the panel is created in OnLevelLoaded, so need to make sure panel exists first
            if (PopulationDemographicsLoading.panel != null)
            {
                PopulationDemographicsLoading.panel.SimulationTick();
            }
        }
    }
}
