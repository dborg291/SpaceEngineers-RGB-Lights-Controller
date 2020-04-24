using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        /*
         * groupname; mode
         * groupname; mode; radius;intensity

         * Examples:
         * All Lights; normal
         * All Lights; rgb-strobe; 20;5
        */

        /*int[] RED = { 255, 0, 0 };
        int[] GREEN = { 0, 255, 0 };
        int[] BLUE = { 0, 0, 255 };*/

        int[,] colors = { { 255, 0, 0 }, { 255, 125, 0 }, { 255, 255, 0 }, { 0, 255, 0 }, { 0, 255, 255 }, { 0, 0, 255 }, { 255, 0, 255 } };

        public void Main(string args, UpdateType updateSource)
        {
            string[] arg;

            if (Me.CustomData.Equals(""))
            {
                arg = args.Split(';');
                Me.CustomData = args;
            }
            else
            {
                arg = Me.CustomData.Split(';');
            }

            // Check arguments
            if (arg.Length < 2)
            {
                Echo("Error!\nNot enough arguments!");
                return;
            }

            // Get group
            var group = GridTerminalSystem.GetBlockGroupWithName(arg[0]);
            if (group == null)
            {
                Echo("Error!\nLight group not found:\n'" + arg[0] + "'");
            }

            // Get color
            var mode = arg[1].ToLower().Trim();
            Echo("Mode: " + mode);

            // Get radius
            float radius = 0;
            if (arg.Length >= 3)
            {
                float.TryParse(arg[2], out radius);
            }

            // Get intensity
            float intensity = 0;
            if (arg.Length >= 4)
            {
                float.TryParse(arg[3], out intensity);
            }

            // Lists to store interior lights, coner lights and spotlights
            List<IMyInteriorLight> interiorlights = new List<IMyInteriorLight>();
            List<IMyInteriorLight> cornerlights = new List<IMyInteriorLight>();
            List<IMyReflectorLight> spotlights = new List<IMyReflectorLight>();

            // Store lights into Lists
            group.GetBlocksOfType<IMyInteriorLight>(interiorlights, l => !l.BlockDefinition.SubtypeName.ToLower().Contains("corner"));
            group.GetBlocksOfType<IMyInteriorLight>(cornerlights, c => c.BlockDefinition.SubtypeName.ToLower().Contains("corner"));
            group.GetBlocksOfType<IMyReflectorLight>(spotlights);

            int totalLights = 0;

            if (mode.Equals("normal"))
            {
                Echo("Normal Mode");
                totalLights = NormalLights(interiorlights, cornerlights, spotlights, radius, intensity);
            }
            else if (mode.Equals("rgb-strobe"))
            {
                totalLights = Stobe(interiorlights, cornerlights, spotlights, radius, intensity, colors);
            }else if(mode.Equals("rgb-fade"))
            {
                totalLights = Fade(interiorlights, cornerlights, spotlights, radius, intensity, colors);
            }

            Echo("Finished Changing Lights");

            // Echo status
            if (totalLights != 0) Echo("Found:");
            if (interiorlights.Count != 0) Echo("- " + interiorlights.Count + " interior lights");
            if (cornerlights.Count != 0) Echo("- " + cornerlights.Count + " corner lights");
            if (spotlights.Count != 0) Echo("- " + spotlights.Count + " spotlights");

            if (totalLights != 0)
            {
                Echo("\nApplied:");
                Echo("- Mode: " + mode);
                if (radius != 0) Echo("- Radius: " + radius);
                if (intensity != 0) Echo("- Intensity: " + intensity);
            }
            else
            {
                Echo("No lights found in group:\n'" + arg[0] + "'");
            }
        }


        public int NormalLights(List<IMyInteriorLight> interiorlights, List<IMyInteriorLight> cornerlights, List<IMyReflectorLight> spotlights, float radius, float intensity)
        {
            int totalLights = 0;
            Color color = new Color(255, 255, 255);
            
            foreach(var light in interiorlights)
            {
                ChangeSetColor(light, color, radius, intensity, totalLights, "normal");
                totalLights++;
            }

            foreach (var light in cornerlights)
            {
                ChangeSetColor(light, color, radius, intensity, totalLights, "normal");
                totalLights++;
            }

            foreach (var light in spotlights)
            {
                ChangeSetColor(light, color, radius, intensity, totalLights, "normal");
                totalLights++;
            }

            return totalLights;
        }


        public int Stobe(List<IMyInteriorLight> interiorlights, List<IMyInteriorLight> cornerlights, List<IMyReflectorLight> spotlights, float radius, float intensity, int[,] colors)
        {
            int totalLights = 0;
            Random rand = new Random();

            foreach(var light in interiorlights)
            {
                ChangeRandomColor(light, rand, colors, radius, intensity, totalLights, "strobe");
                totalLights++;
            }

            foreach (var light in cornerlights)
            {
                ChangeRandomColor(light, rand, colors, radius, intensity, totalLights, "strobe");
                totalLights++;
            }

            foreach (var light in spotlights)
            {
                ChangeRandomColor(light, rand, colors, radius, intensity, totalLights, "strobe");
                totalLights++;
            }

            return totalLights;
        }


        public int Fade(List<IMyInteriorLight> interiorlights, List<IMyInteriorLight> cornerlights, List<IMyReflectorLight> spotlights, float radius, float intensity, int[,] colors)
        {
            int totalLights = 0;
            int lightIndex = 0;
            foreach (var light in interiorlights)
            {
                Color color = light.Color;
                string[] customData =  light.CustomData.Split(';');
                /*Echo(customData.Length.ToString() + " = CUSTOMDATA LENGTH");*/
                if(!customData[1].Equals("fade")) // Need to turn lights to fade mode 
                {
                    int colorIndex = lightIndex;
                    Echo(colorIndex.ToString() + " = COLOR INDEX");
                    Echo(colorIndex.ToString() + " = COLOR INDEX");
                    Color fadeColor = new Color(colors[colorIndex, 0], colors[colorIndex, 1], colors[colorIndex, 2]);
                    ChangeSetColor(light, fadeColor, radius, intensity, totalLights, "fade;" + colorIndex);

                }
                else // Light is already in fade mode
                {
                    int deltaR = 0;
                    int deltaG = 0;
                    int deltaB = 0;
                    int nextColorIndex = lightIndex + 1;
                    if (nextColorIndex == 7)
                    {
                        nextColorIndex = 0;
                    }
                    Color nextColor = new Color(colors[nextColorIndex, 0], colors[nextColorIndex, 1], colors[nextColorIndex, 2]);
                    if(color.R < nextColor.R)// Adjust Red
                    {
                        deltaR = 1;
                    }else if(color.R > nextColor.R)
                    {
                        deltaR = -1;
                    }

                    if (color.G < nextColor.G)// Adjust Green
                    {
                        deltaG = 1;
                    }
                    else if (color.G > nextColor.G)
                    {
                        deltaG = -1;
                    }

                    if (color.B < nextColor.B)// Adjust Blue
                    {
                        deltaG = 1;
                    }
                    else if (color.B > nextColor.B)
                    {
                        deltaG = -1;
                    }

                    Color fadedColor = new Color(color.R + deltaR, color.G + deltaG, color.B + deltaB);
                    if(fadedColor.Equals(nextColor))
                    {
                        ChangeSetColor(light, fadedColor, radius, intensity, totalLights, "fade;" + nextColorIndex);
                    }
                    else
                    {
                        ChangeSetColor(light, fadedColor, radius, intensity, totalLights, "fade;" + customData[2]);
                    }

                }



                lightIndex++;
                if (lightIndex > 6)
                {
                    lightIndex = 0;
                }
                totalLights++;
            }

            return totalLights;
        }

        public void ChangeRandomColor (IMyInteriorLight light, Random rand, int[,] colors, float radius, float intensity, int numLights, string mode)
        {
            int colorIndex = rand.Next(colors.GetLength(0));
            light.Color = new Color(colors[colorIndex, 0], colors[colorIndex, 1], colors[colorIndex, 2]);
            light.CustomData = light.Color.ToString() + ";" + mode + ";" + colorIndex; 
            if (radius != 0) light.Radius = radius;
            if (intensity != 0) light.Intensity = intensity;
        }

        public void ChangeRandomColor(IMyReflectorLight light, Random rand, int[,] colors, float radius, float intensity, int numLights, string mode)
        {
            int colorIndex = rand.Next(colors.GetLength(0));
            light.Color = new Color(colors[colorIndex, 0], colors[colorIndex, 1], colors[colorIndex, 2]);
            light.CustomData = light.Color.ToString() + ";" + mode + ";" + colorIndex;
            if (radius != 0) light.Radius = radius;
            if (intensity != 0) light.Intensity = intensity;
        }

        public void ChangeSetColor(IMyInteriorLight light, Color color, float radius, float intensity, int numLights, string mode)
        {
            light.Color = color;
            light.CustomData = color.ToString() + ";" + mode;
            if (radius != 0) light.Radius = radius;
            if (intensity != 0) light.Intensity = intensity;
        }

        public void ChangeSetColor(IMyReflectorLight light, Color color, float radius, float intensity, int numLights, string mode)
        {
            light.Color = color;
            light.CustomData = color.ToString() + ";" + mode;
            if (radius != 0) light.Radius = radius;
            if (intensity != 0) light.Intensity = intensity;
        }

    }
}
