using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        //list of blocks needed by the script, change the name in the quote to match your blocks
        string mainLCDname = "TH01 - LCD Jump";
        string statusLCDname = "TH01 - LCD Jump Status";
        string cockpitName = "TH01 - Pilot Seat _liftoff";
        string jumpDriveName = "TH01 - Jump Drive Nav";
        string remoteName = "TH01 - Remote Control";
        string gyroName = "TH01 - Gyroscope";




        //Changing anything below this line will void the non-existing warranty !

        IMyCockpit cockpit;
        IMyTextPanel mainLCD;
        IMyTextPanel statusLCD;
        IMyJumpDrive Jumper;
        IMyRemoteControl remote;
        List<MyWaypointInfo> waypoints = new List<MyWaypointInfo>();
        List<IMyGyro> allgyros = new List<IMyGyro>();
        List<IMyGyro> gyros = new List<IMyGyro>();

        Vector3D Target;
        float RadToDeg = (float)(180 / Math.PI);
        float targetDistance = 0, distancePercent, MaxDistance, time;
        int selection = 0, counter = 0, numJumps, pageCount, pageSelect;
        double yaw, pitch;
        string wayList, MaxDist, systemStatus = "", targetInfo, chargeTime, errors;

        bool Aligning = false;
        bool Aligned = false;
        bool Charging = false;
        bool Online = true;
        bool denied = false;
        bool error = true;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument)
        {
            if (error) { InitializationCheck(); }
            else
            {

                statusLCD.WriteText(systemStatus);

                remote.GetWaypointInfo(waypoints); //Get the list of waypoints

                switch (argument)//Check for argument and do something
                {
                    case "down":
                        selection++;
                        if (selection > waypoints.Count - 1) { selection = 0; }
                        Aligning = false;
                        break;
                    case "up":
                        selection--;
                        if (selection < 0) { selection = waypoints.Count - 1; }//could in theory go to -1 but should be okay
                        Aligning = false;
                        break;
                    case "align":
                        Aligning = !Aligning;
                        break;
                    case "off":
                        systemStatus = "Offline";
                        Me.ApplyAction("OFF");
                        break;
                    default:
                        Echo("Unrecognized argument");
                        break;
                }

                wayList = "-Destination selection-\n\n";//Get target and build waypoints selection screen
                if (waypoints.Count > 0)
                {
                    if (selection > waypoints.Count - 1)//Make sure selection doesn't go out of bounds
                    {
                        selection = waypoints.Count - 1;
                    }

                    Target = waypoints[selection].Coords;

                    pageCount = (int)Math.Floor((decimal)(waypoints.Count / 8));
                    pageSelect = (int)Math.Floor((decimal)(selection / 8));
                    Echo(pageCount.ToString());
                    if (selection > 7)
                    {
                        wayList += "^ ^ ^ ^ ^\n";
                    }
                    else
                    {
                        wayList += "\n";
                    }

                    for (int i = 0 + 8 * pageSelect; i < 8 + 8 * pageSelect; i++)
                    {
                        if (i < waypoints.Count)
                        {
                            if (i == selection)
                            {
                                wayList = wayList + "-> " + waypoints[i].Name + " <-\n";
                            }
                            else
                            {
                                wayList = wayList + waypoints[i].Name + "\n";
                            }
                        }
                        else
                        {
                            wayList += "\n";
                        }
                    }
                    if (selection < (pageCount * 8))
                    {
                        wayList += "v v v v v";
                    }
                }
                else
                    wayList += "No waypoints available";


                //Check if we're trying to align while charging
                if (Aligning && Charging)
                    denied = true;
                if (denied)
                {
                    counter++;
                    if (counter > 60) { denied = false; }
                }
                else
                    counter = 0;

                //Get chargetime left and max jump distance from Jump Drive detailed info
                String info = Jumper.DetailedInfo; //Stuff detailed info on the Jump Drive into a string
                chargeTime = info.Substring(info.LastIndexOf("in:") + 4, 6);
                chargeTime = chargeTime.Replace("\n", "");
                time = int.Parse(info.Substring(info.LastIndexOf("in:") + 4, 2));
                MaxDist = info.Substring(info.LastIndexOf("distance:") + 9, 4); //go to start of "distance:", move 9 characters forward and then take the 4 characters ahead, eg. max jump distance
                if (MaxDist.Contains("km"))//in case jump distance is less than 4 digits, cut out the "km" that will be included in the 4 characters
                    MaxDist = MaxDist.Substring(0, MaxDist.IndexOf('k') - 1);
                MaxDistance = float.Parse(MaxDist);//convert the cut out jump distance, from a string to a float

                if (time > 0) { Charging = true; Aligning = false; } else { Charging = false; }
                if (Jumper.Enabled) Online = true; else Online = false;

                //Determind status of drive

                

                if (Aligning)
                {
                    mainLCD.ContentType = ContentType.TEXT_AND_IMAGE;
                    mainLCD.Alignment = TextAlignment.LEFT;
                    mainLCD.BackgroundColor = denied ? Color.Red : Color.Black;
                    mainLCD.FontColor = denied ? Color.Yellow : Color.Gray;
                    mainLCD.WriteText(targetInfo);
                    statusLCD.WriteText(systemStatus);

                    if (Aligned)
                    {
                        systemStatus = "Ready to jump";
                        statusLCD.BackgroundColor = Color.Green;

                    }
                    else
                    {
                        systemStatus = "Aligning..."; mainLCD.ContentType = ContentType.TEXT_AND_IMAGE;
                        statusLCD.BackgroundColor = Color.Black;
                    }
                }
                else 
                {
                    if (Online)
                    {
                        if (Charging)
                        {
                            systemStatus = "Charging - " + chargeTime;
                            statusLCD.BackgroundColor = Color.Red;
                        }
                        else
                        {
                            systemStatus = "Standby";
                            statusLCD.BackgroundColor = Color.Black;
                        }
                    }
                    else
                    {
                        systemStatus = "!! Offline !!";
                    }
                    mainLCD.ContentType = ContentType.TEXT_AND_IMAGE;
                    mainLCD.Alignment = TextAlignment.CENTER;
                    mainLCD.WriteText(wayList);
                    statusLCD.WriteText(systemStatus);
                }

                //Do the alignment, set distance on jump drive and build targetinfo text.
                if (Aligning)
                {
                    GetOrientation();
                    foreach (var item in gyros)
                    {
                        item.GyroOverride = true;
                        item.Pitch = (float)(pitch / 20);
                        item.Yaw = (float)(yaw / 20);
                        item.Roll = (float)(-1 * item.Pitch);
                    }
                    targetDistance = (float)((cockpit.GetPosition() - Target).Length()) / 1000;
                    distancePercent = ((targetDistance - 5) / (MaxDistance - 5)) * 100; //Convert the desired distance to a percentage of the max distance, taking into account that 0% = 5km
                    Jumper.SetValueFloat("JumpDistance", distancePercent);
                    numJumps = (int)Math.Ceiling(targetDistance / MaxDistance);
                    targetInfo = "\n\nDestination: " + waypoints[selection].Name + "\n\nDistance: " + targetDistance + " KM" + "\nNumber of Jumps: " + numJumps + "\n\n-Coordinates-\n" + waypoints[selection].Coords;
                }
                else
                {
                    foreach (var item in gyros)
                    {
                        item.GyroOverride = false;
                    }
                    targetInfo = "";
                }
                if (pitch < 0.005 && pitch > -0.005 && yaw < 0.005 && yaw > -0.005)
                    Aligned = true;
                else
                    Aligned = false;

            }

        }

        public void GetOrientation()//Function for getting heading and elevation in relation to target,
        {
            Vector3D DirVect = Vector3D.TransformNormal(Target - cockpit.GetPosition(), MatrixD.Transpose(cockpit.WorldMatrix));
            yaw = Math.Asin(DirVect.X / DirVect.Length()) * RadToDeg;
            pitch = Math.Asin(DirVect.Y / DirVect.Length()) * RadToDeg;
        }
        public void InitializationCheck()//Checks if all blocks exists, assigns them if they do or throws an error message and loops if they do not.
        {
            error = false;
            systemStatus = "STANDBY";
            errors = "ERROR\n";
            if (GridTerminalSystem.GetBlockWithName(mainLCDname) as IMyTextPanel != null) { mainLCD = GridTerminalSystem.GetBlockWithName(mainLCDname) as IMyTextPanel; } else { Echo(mainLCDname + " missing!"); error = true; }
            if (GridTerminalSystem.GetBlockWithName(statusLCDname) as IMyTextPanel != null) { statusLCD = GridTerminalSystem.GetBlockWithName(statusLCDname) as IMyTextPanel; } else { Echo(statusLCDname + " missing!"); error = true; }
            if (GridTerminalSystem.GetBlockWithName(cockpitName) as IMyCockpit != null) { cockpit = GridTerminalSystem.GetBlockWithName(cockpitName) as IMyCockpit; } else { errors += cockpitName + " missing!"; error = true; }
            if (GridTerminalSystem.GetBlockWithName(jumpDriveName) as IMyJumpDrive != null) { Jumper = GridTerminalSystem.GetBlockWithName(jumpDriveName) as IMyJumpDrive; } else { errors += "\n" + jumpDriveName + " missing!"; error = true; }
            if (GridTerminalSystem.GetBlockWithName(remoteName) as IMyRemoteControl != null) { remote = GridTerminalSystem.GetBlockWithName(remoteName) as IMyRemoteControl; } else { errors += "\n" + remoteName + " missing!"; error = true; }
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(allgyros, filterThis);
            if (allgyros.Count > 0)
            {
                for (int i = 0; i < allgyros.Count; i++)
                {
                    if (allgyros[i].CustomName.IndexOf(gyroName) > -1)
                    {
                        gyros.Add(allgyros[i]);
                    }
                }
                if (allgyros.Count == 0)
                {
                    errors += "no Gyroscope blocks with name including" + gyroName + "\n";
                }
            }
            else
            {
                errors += "no Gyroscope blocks found\n";
            }

            statusLCD.Alignment = TextAlignment.CENTER;
            statusLCD.ContentType = ContentType.TEXT_AND_IMAGE;

            if (error && (GridTerminalSystem.GetBlockWithName(mainLCDname) as IMyTextPanel != null) && (GridTerminalSystem.GetBlockWithName(statusLCDname) as IMyTextPanel != null))
            {
                mainLCD.BackgroundColor = Color.Blue;
                mainLCD.FontColor = Color.Yellow;
                mainLCD.WriteText(errors);
                systemStatus = "ERROR";
            }
        }
        bool filterThis(IMyTerminalBlock block)
        {
            return block.CubeGrid == Me.CubeGrid;
        }
    }
}
