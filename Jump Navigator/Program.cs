using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        string mainLCDname = "LCD Jump Main";
        string statusLCDname = "LCD Jump Status";
        string cockpitName = "Pilot Seat";
        string jumperName = "Jump Drive Nav";
        string remoteName = "Remote Control Nav";
        string gyroName = "Gyroscope";


        List<IMyCockpit> Cockpits = new List<IMyCockpit>();
        IMyCockpit cockpit;
        List<IMyTextPanel> LCDs = new List<IMyTextPanel>();
        IMyTextPanel mainLCD;
        IMyTextPanel statusLCD;
        List<IMyJumpDrive> Jumpers = new List<IMyJumpDrive>();
        IMyJumpDrive jumper;
        List<IMyRemoteControl> Remotes = new List<IMyRemoteControl>();
        IMyRemoteControl remote;
        List<IMyGyro> Allgyros = new List<IMyGyro>();
        List<IMyGyro> gyros = new List<IMyGyro>();
        List<MyWaypointInfo> waypoints = new List<MyWaypointInfo>();

        Vector3D Target;
        float targetDistance = 0, distancePercent, MaxDistance, time;
        int selection = 0, numJumps, pageCount, pageSelect;
        string wayList, MaxDist, systemStatus = "", targetInfo, chargeTime, errors;
        int scriptspeed = 1;
        double serverLimit;
        int heatLimit;
        double avg = 0;
        int cdCount = 0;
        double avgcd = 0;
        int listsize = 8;
        float gyropower = 1;
        string version = "";
        string ver = "20210103";

        bool aligning = false;
        bool aligned = false;
        bool charging = false;
        bool online = true;
        bool error = true;
        bool close = false;
        bool runtimeecho = true;

        int run = 0;
        string barr = "";

        public Program()
        {
            CustomData();
            switch (scriptspeed)
            {
                default:
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;
                    break;
                case 1:
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;
                    break;
                case 2:
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    break;
                case 3:
                    Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    break;
            }
        }

        public void Main(string argument)
        {
            RuntimeEcho();
            if (error)
            {
                InitializationCheck();
            }
            else
            {
                Input(argument);
                Waylist();
                JumpDriveRead();
                Align();
                Display();
            }
        }
        private void CustomData()
        {
            if (Me.CustomData.Length > 0)
            {
                CustomDataProcess(Me.CustomData);
            }
            else
            {
                Me.CustomData = "version = [" + ver + "]" + "\n"
                    + "To apply changes, please Recompile the script!" + "\n\n"
                    + "Main LCD Tag = [LCD Jump Main]" + "\n"
                    + "Status LCD Tag = [LCD Jump Status]" + "\n"
                    + "Cockpit Tag = [Pilot Seat]" + "\n"
                    + "Jump Drive Tag = [Jump Drive Nav]                     -- if there is more then one drive installed, select one which will be placed on the hotbar" + "\n"
                    + "Remote Control Tag = [Remote Control Nav]" + "\n"
                    + "Gyro Tag = [Gyroscope]                                        -- if there is more then one, each one need to contain this tag in its name" + "\n"
                    + "Gyro Power = [1]" + "\n"
                    + "Main LCD size = [8]                                                  -- how many rows can fit to the screen. The default is 8 which is good for 1x1 LCDs. The Status LCD can be a 'Corner LCD' " + "\n"
                    + "Script speed = [1]                                                      -- 1: UpdateFrequency1 | 2:UpdateFrequency10 | 3:UpdateFrequency1"
                    + "If you play on a multiplayer server and the runtime of the PB is limited, modify the next variables accordingly" + "\n"
                    + "Server execution time limit = [0.3] ms" + "\n"
                    + "Heat limit = [30] %                                                    -- This is the percentage of the Server limit which is available for this script" + "\n"
                    + "Runtime Echo = [true]" + "\n"
                    ;
                CustomData();
            }
        }

        private void CustomDataProcess(string customData)
        {
            List<string[]> customInput = new List<string[]>();
            string[] customDataA = customData.Split('\n');
            for (int i = 0; i < customDataA.Length; i++)
            {
                if (customDataA[i].Contains("="))
                {
                    customInput.Add(customDataA[i].Split('='));
                }
            }
            foreach (var item in customInput)
            {
                item[0] = item[0].Substring(0, item[0].Length - 1);
                item[1] = item[1].Split('[')[1].Split(']')[0];
            }
            foreach (var item in customInput)
            {
                switch (item[0])
                {
                    default:
                        Echo(item[0] + "\n");
                        break;
                    case "Main LCD Tag":
                        mainLCDname = item[1];
                        break;
                    case "Status LCD Tag":
                        statusLCDname = item[1];
                        break;
                    case "Cockpit Tag":
                        cockpitName = item[1];
                        break;
                    case "Jump Drive Tag":
                        jumperName = item[1];
                        break;
                    case "Remote Control Tag":
                        remoteName = item[1];
                        break;
                    case "Gyro Tag":
                        gyroName = item[1];
                        break;
                    case "Gyro Power":
                        gyropower = float.Parse(item[1]);
                        break;
                    case "Script speed":
                        scriptspeed = int.Parse(item[1]);
                        break;
                    case "Main LCD size":
                        listsize = int.Parse(item[1]);
                        break;
                    case "Server execution time limit":
                        serverLimit = double.Parse(item[1]);
                        break;
                    case "Heat limit":
                        heatLimit = int.Parse(item[1]);
                        break;
                    case "Runtime Echo":
                        runtimeecho = bool.Parse(item[1]);
                        break;
                    case "version":
                        version = (item[1]);
                        break;
                }
            }
            if (version != ver)
            {
                Me.CustomData = "";
                CustomData();
            }
        }

        private void RuntimeEcho()
        {
            if (runtimeecho)
            {
                switch (run)
                {
                    default:
                        break;
                    case 0:
                        barr = "Jump Navigator [|---]\n";
                        break;
                    case 100:
                        barr = "Jump Navigator [-|--]\n";
                        break;
                    case 200:
                        barr = "Jump Navigator [--|-]\n";
                        break;
                    case 300:
                        barr = "Jump Navigator [---|]\n";
                        break;
                    case 400:
                        barr = "Jump Navigator [--|-]\n";
                        break;
                    case 500:
                        barr = "Jump Navigator [-|--]\n";
                        run = -(int)Math.Pow(10, (scriptspeed - 1));
                        break;
                }

                run += (int)Math.Pow(10, (scriptspeed - 1));
                Echo(barr);
                Echo(ScriptSpeedManagement());
            }
        }
        private void Input(string argument)
        {
            switch (argument)                                                                       //Check for argument and do something
            {
                case "down":
                    selection++;
                    if (selection > waypoints.Count - 1) { selection = waypoints.Count - 1; }
                    aligning = false;
                    aligned = false;
                    close = false;
                    jumper.Enabled = true;
                    break;
                case "up":
                    selection--;
                    if (selection < 0) { selection = 0; }
                    aligning = false;
                    aligned = false;
                    close = false;
                    jumper.Enabled = true;
                    break;
                case "align":
                    if (!close) { aligning = !aligning; }
                    else { aligning = false; }
                    aligned = false;
                    close = false;
                    break;
                case "":
                    break;
                default:
                    Echo("Unrecognized argument");
                    break;
            }
        }
        private string ScriptSpeedManagement()
        {

            string scriptspeed = "";

            if (Math.Round(((avg / serverLimit) * 100), 1) > heatLimit)
            {
                cdCount = (int)Math.Round(Math.Pow(2, ((avg / serverLimit) * 10)));
            }
            avg = avg * 0.995 + Runtime.LastRunTimeMs * 0.005;
            avgcd = avgcd * 0.995 + cdCount * 0.005;
            scriptspeed += "Server limit: " + serverLimit + " ms\n";
            scriptspeed += "Avg runtime: " + Math.Round(avg, 3).ToString() + " ms\n";
            scriptspeed += "PB heat: " + Math.Round(((avg / serverLimit) * 100), 1) + "%\n";
            // scriptspeed += "Script performance: " + Math.Round((1 / (avgcd + 1)) * 100).ToString() + "%\n";

            return scriptspeed;
        }
        private void Waylist()
        {
            wayList = "-Destination selection-\n\n";                                            //Get target and build waypoints selection screen
            if (waypoints.Count > 0)
            {
                if (selection > waypoints.Count - 1)                                            //Make sure selection doesn't go out of bounds
                {
                    selection = waypoints.Count - 1;
                }

                Target = waypoints[selection].Coords;

                pageCount = (int)Math.Floor((decimal)(waypoints.Count / 8));
                pageSelect = (int)Math.Floor((decimal)(selection / 8));
                if (selection > listsize - 1)
                {
                    wayList += "^ ^ ^ ^ ^\n";
                }
                else
                {
                    wayList += "\n";
                }

                for (int i = 0 + listsize * pageSelect; i < listsize + listsize * pageSelect; i++)
                {
                    if (i < waypoints.Count)
                    {
                        if (i == selection)
                        {
                            wayList = wayList + "-->  " + waypoints[i].Name + "  <--\n";
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
                if (selection < (pageCount * listsize))
                {
                    wayList += "v v v v v";
                }
            }
            else
                wayList += "No waypoints available";
        }
        private void Align()
        {
            if (aligning)
            {
                targetDistance = (float)((cockpit.GetPosition() - Target).Length()) / 1000;

                if (targetDistance > 5)
                {
                    foreach (var gyro in gyros)
                    {
                        gyro.GyroOverride = true;
                        SetOrientation(gyro);
                    }
                    distancePercent = ((targetDistance - (float)5.1) / (MaxDistance - 5)) * 100;                                     //Convert the desired distance to a percentage of the max distance, taking into account that 0% = 5km
                    jumper.SetValueFloat("JumpDistance", distancePercent);
                    numJumps = (int)Math.Ceiling(targetDistance / MaxDistance);
                    targetInfo = "\n\nDestination: " + waypoints[selection].Name + "\n\nDistance: " + targetDistance + " KM" + "\nNumber of Jumps: " + numJumps + "\n\n-Coordinates-\n"
                            + waypoints[selection].Coords.ToString().Split(' ')[0] + "\n" + waypoints[selection].Coords.ToString().Split(' ')[1] + "\n" + waypoints[selection].Coords.ToString().Split(' ')[2] + "\n";

                    float sum = 0;
                    foreach (var gyro in gyros)
                    {
                        sum += Math.Abs(gyro.Yaw) + Math.Abs(gyro.Pitch) + Math.Abs(gyro.Roll);
                    }
                    if (sum < 0.005)
                    {
                        aligned = true;
                        foreach (var gyro in gyros)
                        {
                            gyro.Yaw = 0;
                            gyro.Roll = 0;
                            gyro.Pitch = 0;
                        }
                    }
                }
                else
                {
                    if (!aligned)
                    {
                        close = true;
                        aligning = false;
                    }
                    else
                    {
                        aligned = false;
                    }
                }
            }
            else
            {
                foreach (var item in gyros)
                {
                    item.GyroOverride = false;
                    item.GyroPower = gyropower;
                    item.Roll = 0;
                }
            }
        }
        private void Display()
        {
            if (aligning)
            {
                mainLCD.ContentType = ContentType.TEXT_AND_IMAGE;
                mainLCD.Alignment = TextAlignment.LEFT;
                mainLCD.FontColor = Color.Gray;
                mainLCD.FontSize = 1.3F;
                statusLCD.FontSize = 10;
                mainLCD.WriteText(targetInfo);

                if (aligned)
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
                if (online)
                {
                    if (charging)
                    {
                        systemStatus = "Charging";
                        statusLCD.BackgroundColor = Color.Red;
                        mainLCD.WriteText("Remaining time: " + chargeTime);
                    }
                    else if (close && !aligned)
                    {
                        mainLCD.Alignment = TextAlignment.CENTER;
                        systemStatus = "Warning";
                        statusLCD.BackgroundColor = Color.MediumVioletRed;
                        mainLCD.WriteText("\n\n" + waypoints[selection].Name + "\n is only\n" + Math.Round(((cockpit.GetPosition() - Target).Length()) / 1000, 2) + " km away\n\n Please select \nan other destination\n");

                    }
                    else
                    {
                        systemStatus = "Standby";
                        statusLCD.BackgroundColor = Color.Black;
                        mainLCD.ContentType = ContentType.TEXT_AND_IMAGE;
                        mainLCD.Alignment = TextAlignment.CENTER;
                        mainLCD.WriteText(wayList);
                    }
                }
                else
                {
                    systemStatus = "Offline";
                    statusLCD.BackgroundColor = Color.Black;
                    mainLCD.WriteText("");
                }
            }

            statusLCD.WriteText(systemStatus);
        }
        private void JumpDriveRead()
        {
            //Get chargetime left and max jump distance from Jump Drive detailed info
            String info = jumper.DetailedInfo;                                                         //Stuff detailed info on the Jump Drive into a string
            chargeTime = info.Substring(info.LastIndexOf("in:") + 4, 6);
            chargeTime = chargeTime.Replace("\n", "");
            time = int.Parse(info.Substring(info.LastIndexOf("in:") + 4, 2));
            MaxDist = info.Substring(info.LastIndexOf("distance:") + 9, 5);                            //go to start of "distance:", move 9 characters forward and then take the 4 characters ahead, eg. max jump distance
            if (MaxDist.Contains("km"))                                                                //in case jump distance is less than 4 digits, cut out the "km" that will be included in the 4 characters
                MaxDist = MaxDist.Split(' ')[0];
            MaxDistance = float.Parse(MaxDist);                                                          //convert the cut out jump distance, from a string to a float

            if (time > 0) { charging = true; aligning = false; } else { charging = false; }
            if (jumper.Enabled) online = true; else online = false;
        }

        private double round0(double d)
        {
            return Math.Abs(d) < 0.0001 ? 0 : d;
        }

        public void SetOrientation(IMyGyro gyro)
        {
            if (gyro.Enabled)
            {
                Vector3D worldRV;

                Vector3 pos = cockpit.GetPosition();
                Vector3 target = Target - pos;
                QuaternionD QRV = QuaternionD.CreateFromTwoVectors(target, cockpit.WorldMatrix.Forward);

                Vector3D axis;
                double angle;
                QRV.GetAxisAngle(out axis, out angle);
                worldRV = axis * Math.Log(1 + round0(angle), 2);

                Vector3D gyroRV = Vector3D.TransformNormal(worldRV, MatrixD.Transpose(gyro.WorldMatrix));

                gyro.Pitch = (float)gyroRV.X;
                gyro.Yaw = (float)gyroRV.Y;
                gyro.Roll = (float)gyroRV.Z;

            }
        }


        public void InitializationCheck()
        {
            CustomData();
            error = false;
            Echo(errors);
            errors = "ERROR\n";

            //LCDs
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(LCDs, filterThis);
            if (LCDs.Count > 0)
            {
                for (int i = 0; i < LCDs.Count; i++)
                {
                    if (LCDs[i].CustomName.IndexOf(mainLCDname) > -1)
                    {
                        mainLCD = LCDs[i];
                    }
                    if (LCDs[i].CustomName.IndexOf(statusLCDname) > -1)
                    {
                        statusLCD = LCDs[i];
                    }
                }
                if (mainLCD == null)
                {
                    Echo("no LCD blocks with name including [" + mainLCDname + "]\n");
                    error = true;
                }
                if (statusLCD == null)
                {
                    Echo("no LCD blocks with name including [" + statusLCDname + "]\n");
                    error = true;
                }
            }
            else
            {
                errors += "no LCD blocks found\n";
                error = true;
            }
            //Cockpit
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(Cockpits, filterThis);
            if (Cockpits.Count > 0)
            {
                for (int i = 0; i < Cockpits.Count; i++)
                {
                    if (Cockpits[i].CustomName.IndexOf(cockpitName) > -1)
                    {
                        cockpit = Cockpits[i];
                    }
                }
                if (cockpit == null)
                {
                    errors += "no Cockpit blocks with name including [" + cockpitName + "]\n";
                    error = true;
                }
            }
            else
            {
                errors += "no Cockpit blocks found\n";
                error = true;
            }
            //Jump drives
            GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(Jumpers, filterThis);
            if (Jumpers.Count > 0)
            {
                for (int i = 0; i < Jumpers.Count; i++)
                {
                    if (Jumpers[i].CustomName.IndexOf(jumperName) > -1)
                    {
                        jumper = Jumpers[i];
                    }
                }
                if (jumper == null)
                {
                    errors += "no Jump drive blocks with name including [" + jumperName + "]\n";
                    error = true;
                }
            }
            else
            {
                errors += "no Jump drive blocks found\n";
                error = true;
            }
            //Remote
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(Remotes, filterThis);
            if (Remotes.Count > 0)
            {
                for (int i = 0; i < Remotes.Count; i++)
                {
                    if (Remotes[i].CustomName.IndexOf(remoteName) > -1)
                    {
                        remote = Remotes[i];
                    }
                }
                if (remote == null)
                {
                    errors += "no Remote Control blocks with name including [" + remoteName + "]\n";
                    error = true;
                }
            }
            else
            {
                errors += "no Remote Control blocks found\n";
                error = true;
            }
            //Gyros
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(Allgyros, filterThis);
            if (Allgyros.Count > 0)
            {
                for (int i = 0; i < Allgyros.Count; i++)
                {
                    if (Allgyros[i].CustomName.IndexOf(gyroName) > -1)
                    {
                        gyros.Add(Allgyros[i]);
                    }
                }
                if (Allgyros.Count == 0)
                {
                    errors += "no Gyroscope blocks with name including [" + gyroName + "]\n";
                    error = true;
                }
            }
            else
            {
                errors += "no Gyroscope blocks found\n";
                error = true;
            }

            statusLCD.Alignment = TextAlignment.CENTER;
            statusLCD.ContentType = ContentType.TEXT_AND_IMAGE;
            statusLCD.FontColor = Color.Gray;
            statusLCD.BackgroundColor = Color.Black;
            remote.GetWaypointInfo(waypoints);

            if (error && (GridTerminalSystem.GetBlockWithName(mainLCDname) as IMyTextPanel != null) && (GridTerminalSystem.GetBlockWithName(statusLCDname) as IMyTextPanel != null))
            {
                mainLCD.WriteText(errors);
                systemStatus = "ERROR";
                Echo(errors);
                statusLCD.WriteText(systemStatus);
                statusLCD.BackgroundColor = Color.Red;
            }

        }
        bool filterThis(IMyTerminalBlock block)
        {
            return block.CubeGrid == Me.CubeGrid;
        }
    }
}
