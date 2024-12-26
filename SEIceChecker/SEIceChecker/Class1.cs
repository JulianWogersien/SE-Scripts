using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace SpaceEngineersScript
{
    public class Program : MyGridProgram
    {
        // CONFIG
        private const double ICE_WARNING_THRESHOLD = 10000;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            double totalIce = 0;
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);

            var mainGridId = Me.CubeGrid.EntityId;

            foreach (var block in blocks)
            {
                if (block.CubeGrid.EntityId != mainGridId) continue;
                if (!(block is IMyTerminalBlock)) continue;
                IMyInventory inventory = ((IMyTerminalBlock)block).GetInventory(0);
                if (inventory == null) continue;
                var items = new List<MyInventoryItem>();
                inventory.GetItems(items);
                foreach (var item in items)
                {
                    if (item.Type.SubtypeId.Contains("Ice"))
                    {
                        totalIce += (double)item.Amount;
                    }
                }
            }

            List<IMyLightingBlock> lights = new List<IMyLightingBlock>();
            GridTerminalSystem.GetBlocksOfType(lights);
            lights.RemoveAll(light => light.CubeGrid.EntityId != mainGridId);

            if (totalIce <= ICE_WARNING_THRESHOLD)
            {
                foreach (var light in lights)
                {
                    if (string.IsNullOrEmpty(light.CustomData))
                    {
                        light.CustomData = $"OriginalColor:{light.Color.R},{light.Color.G},{light.Color.B}";
                    }

                    light.Color = Color.Red;
                    light.BlinkLength = 50f;
                    light.BlinkIntervalSeconds = 0.5f;
                    light.Enabled = true;
                }
                Echo($"WARNING: Low ice level detected!\nCurrent ice: {totalIce:N0} kg\nThreshold: {ICE_WARNING_THRESHOLD:N0} kg");
            }
            else
            {
                foreach (var light in lights)
                {
                    if (!string.IsNullOrEmpty(light.CustomData))
                    {
                        string[] colorData = light.CustomData.Split(':')[1].Split(',');
                        if (colorData.Length == 3)
                        {
                            light.Color = new Color(
                                byte.Parse(colorData[0]),
                                byte.Parse(colorData[1]),
                                byte.Parse(colorData[2])
                            );
                        }
                        light.CustomData = "";
                    }
                    light.BlinkLength = 0f;
                    light.BlinkIntervalSeconds = 0f;
                }
                Echo($"Ice levels normal\nCurrent ice: {totalIce:N0} kg");
            }
        }
    }
}