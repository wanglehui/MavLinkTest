using System;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MavLinkNet;

namespace WindowsFormsApplication1
{
    public partial class MainForm : Form
    {
        private MavLinkSerialPortTransport _mMavLink;
        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _mMavLink = new MavLinkSerialPortTransport
            {
                BaudRate = 115200,
                SerialPortName = "COM4",
                MavlinkSystemId = 0xFF,
                MavlinkComponentId = 0xBE
            };


            _mMavLink.OnPacketReceived += OnMavLinkPacketReceived;
            try
            {
                _mMavLink.Initialize();
            }
            catch (Exception)
            {
                return;
            }

            _mMavLink.BeginHeartBeatLoop();

            button1.Enabled = false;
            button2.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _mMavLink.Dispose();
            button1.Enabled = true;
            button2.Enabled = false;
        }

        private void OnMavLinkPacketReceived(object sender, MavLinkPacket packet)
        {
            if (packet.Message is UasCommandLong)
            {
                PrintCommandLong(packet.Message as UasCommandLong);
            }
            else
            {
                PrintStandardMessage(packet.Message);
            }
        }

        private void PrintCommandLong(UasCommandLong m)
        {
            var md = m.GetMetadata();
            WL("{0}: {1}", m, md.Description);

            foreach (UasFieldMetadata f in md.Fields)
            {
                if (f.Name.StartsWith("Param") && (m is UasCommandLong))
                {
                    WL("    {0}: {1} ({2})",
                        f.Name, GetFieldValue(f.Name, m),
                        GetCommandParamDescription((int)m.Command, f.Name.Substring(5)));
                    continue;
                }

                WL("  {0}: {1}  ({2})", f.Name, GetFieldValue(f.Name, m), f.Description);
            }
        }

        private string GetCommandParamDescription(int command, string paramIndexString)
        {
            int paramIndex;
            if (!Int32.TryParse(paramIndexString, out paramIndex)) return "";

            foreach (var entry in UasSummary.GetEnumMetadata("MavCmd").Entries)
            {
                if (command != entry.Value) continue;

                return entry.Params[paramIndex - 1];
            }

            return "";
        }

        private void PrintStandardMessage(UasMessage m)
        {
            if (m is UasAttitude)
            {
                DumpMsgMeta(m);
                return;
            }

            if (m is UasCommandAck)
            {
                WL("CmdACK: {0}", ((UasCommandAck) m).Result);
            }
            else if (m is UasMissionAck)
            {
                WL("MissionACK: {0}", ((UasMissionAck)m).Type);
            }
            else if (m is UasStatustext)
            {
                var sb = new StringBuilder();
                foreach (char c in ((UasStatustext)m).Text)
                {
                    if ('\0' == c)break;
                    sb.Append(c);
                }
                WL("Statustext: {0}", sb.ToString());
            }
            else if (m is UasPowerStatus)
            {
                WL("PowerStatus: {0}", ((UasPowerStatus)m).Vcc);
            }
            else if (m is UasHomePosition)
            {
                WL("**********HomePosition: {0}", ((UasHomePosition)m).X);
            }
            else if (m is UasHeartbeat)
            {
                var hb = (UasHeartbeat) m;
                WL("Heartbeat: {0} {1}", hb.Autopilot, hb.Type);
            }
            else if (m is UasLogEntry)
            {
                var log = (UasLogEntry)m;
                WL("UasLogEntry: {0} {1} {2}", log.Id, log.NumLogs, log.LastLogNum);
            }
            else if (m is UasMissionItem
                || m is UasMissionCount
                || m is UasMissionRequest
                || m is UasMissionCurrent
                || m is UasLogData)
            {
                var substring = m.GetType().ToString().Remove(0, 14);//remove prefix: "MavLinkNet.Uas"
                WL("MSG: {0}", substring);
                DumpMsgMeta(m);
            }
            else if (m is UasParamValue)
            {
                WL("MSG: UasParamValue");
                DumpMsgMeta(m);
                /*
                var md = m.GetMetadata();
                var sb = new StringBuilder();
                foreach (var f in md.Fields)
                {
                    sb.AppendLine(string.Format("  {0}: {1}  ({2})",
                        f.Name,
                        GetFieldValue(f.Name, m),
                        f.Description));
                }
                File.AppendAllText("UasParamValue.txt", sb.ToString());
                */
            }
            else
            {
                var substring = m.GetType().ToString().Remove(0, 14);//remove prefix: "MavLinkNet.Uas"
                WL("MSG： {0,-5} {1}", m.MessageId, substring);
            }
        }

        private void DumpMsgMeta(UasMessage m)
        {
            var md = m.GetMetadata();
            foreach (var f in md.Fields)
            {
                WL("  {0}: {1}  ({2})", f.Name, GetFieldValue(f.Name, m), f.Description);
            }
        }

        private void WL(string msg, params object[] args)
        {
            MyShell.WriteLine(msg, args);
        }


        private object GetFieldValue(string fieldName, UasMessage m)
        {
            PropertyInfo p = m.GetType().GetProperty(fieldName);

            if (p == null)
            {
                WL("MISSING FIELD: {0} on {1}", fieldName, m.GetType());
                return "";
            }

            object result = p.GetValue(m, null);

            var c = result as char[];
            if (c != null) return new String(c);

            return result;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var msg = new UasParamRequestList();
            _mMavLink.SendMessage(msg);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var msg = new UasMissionClearAll
            {
                TargetComponent = 1,
                TargetSystem = 1
            };
            _mMavLink.SendMessage(msg);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var cmd = new UasCommandLong
            {
                Command = MavCmd.GetHomePosition,
            };

            _mMavLink.SendMessage(cmd);
        }

        /*
            <enum name="MAV_DATA_STREAM">
               <entry value="0" name="MAV_DATA_STREAM_ALL">
                    <description>Enable all data streams</description>
               </entry>
               <entry value="1" name="MAV_DATA_STREAM_RAW_SENSORS">
                    <description>Enable IMU_RAW, GPS_RAW, GPS_STATUS packets.</description>
               </entry>
               <entry value="2" name="MAV_DATA_STREAM_EXTENDED_STATUS">
                    <description>Enable GPS_STATUS, CONTROL_STATUS, AUX_STATUS</description>
               </entry>
               <entry value="3" name="MAV_DATA_STREAM_RC_CHANNELS">
                    <description>Enable RC_CHANNELS_SCALED, RC_CHANNELS_RAW, SERVO_OUTPUT_RAW</description>
               </entry>
               <entry value="4" name="MAV_DATA_STREAM_RAW_CONTROLLER">
                    <description>Enable ATTITUDE_CONTROLLER_OUTPUT, POSITION_CONTROLLER_OUTPUT, NAV_CONTROLLER_OUTPUT.</description>
               </entry>
               <entry value="6" name="MAV_DATA_STREAM_POSITION">
                    <description>Enable LOCAL_POSITION, GLOBAL_POSITION/GLOBAL_POSITION_INT messages.</description>
               </entry>
               <entry value="10" name="MAV_DATA_STREAM_EXTRA1">
                    <description>Dependent on the autopilot</description>
               </entry>
               <entry value="11" name="MAV_DATA_STREAM_EXTRA2">
                    <description>Dependent on the autopilot</description>
               </entry>
               <entry value="12" name="MAV_DATA_STREAM_EXTRA3">
                    <description>Dependent on the autopilot</description>
               </entry>
          </enum>
         */
        private void button6_Click(object sender, EventArgs e)
        {
            var msg = new UasRequestDataStream
            {
                StartStop = 0,
                ReqStreamId = 0,
            };
            _mMavLink.SendMessage(msg);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var msg = new UasRequestDataStream
            {
                StartStop = 1,
                ReqStreamId = 10,
                ReqMessageRate = 500,
            };
            _mMavLink.SendMessage(msg);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var msg = new UasAutopilotVersionRequest();
            _mMavLink.SendMessage(msg);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            byte[] temp = Encoding.ASCII.GetBytes("Mission Planner " + Application.ProductVersion + "\0");
            Array.Resize(ref temp, 50);

            var msg = new UasStatustext
            {
                Text = Encoding.ASCII.GetChars(temp),
                Severity = MavSeverity.Info
            };
            _mMavLink.SendMessage(msg);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            /*
            <param index="1">Use current (1=use current location, 0=use specified location)</param>
            <param index="2">Empty</param>
            <param index="3">Empty</param>
            <param index="4">Empty</param>
            <param index="5">Latitude</param>
            <param index="6">Longitude</param>
            <param index="7">Altitude</param>
             */
            var msg = new UasSetHomePosition
            {
                Latitude = 0,
                Altitude = 100,
                Longitude = 0
            };

            _mMavLink.SendMessage(msg);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(null != _mMavLink)
                _mMavLink.Dispose();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            var msg1 = new UasMissionRequestList();
            MyShell.WriteLine("警告" + "Send UasMissionRequestList");
            _mMavLink.SendMessage(msg1);

            Thread.Sleep(500);
            MyShell.WriteLine("警告" + "Send UasMissionRequest");
            var msg2 = new UasMissionRequest {Seq = 0};
            _mMavLink.SendMessage(msg2);

            //var msg1 = new UasMissionItem {Seq = 1};
            //_mMavLink.SendMessage(msg1);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            var msg = new UasMissionCount {Count = 1};
            _mMavLink.SendMessage(msg);
            Thread.Sleep(500);

            var msg1 = new UasMissionItemInt
            {
                Seq = 0,
                Command = MavCmd.NavWaypoint,
                X = 7900004,
                Y = 3400004
            };
            _mMavLink.SendMessage(msg1);
            Thread.Sleep(500);

            //var msg2 = new UasMissionItem
            //{
            //    Seq = 1,
            //    Command = MavCmd.NavTakeoff,
            //    X = 79,
            //    Y = 34
            //};
            //_mMavLink.SendMessage(msg2);
            Thread.Sleep(500);

            //msg2 = new UasMissionItem
            //{
            //    Seq = 2,
            //    Command = MavCmd.NavWaypoint,
            //    X = 79.0002f,
            //    Y = 34.0002f
            //};
            //_mMavLink.SendMessage(msg2);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            var cmd = new UasCommandLong
            {
                Command = MavCmd.NavTakeoff,
                Param1 = 0,
                Param2 = 0,
                Param3 = 0,
                Param4 = 0,
                Param5 = 0,
                Param6 = 0,
                Param7 = 0
            };

            _mMavLink.SendMessage(cmd);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            var msg = new UasMissionSetCurrent {Seq = 0};
            _mMavLink.SendMessage(msg);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            var msg = new UasSetMode
            {
                BaseMode = 1,
                CustomMode = 0
            };

            _mMavLink.SendMessage(msg);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void button16_Click(object sender, EventArgs e)
        {
            var msg1 = new UasMissionItem
            {
                Seq = 2,
                Command = MavCmd.NavWaypoint,
                X = 43.39101f,
                Y = 46.74377f,
                Z = 300
            };
            _mMavLink.SendMessage(msg1);
            //Thread.Sleep(500);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            //Set Parameter
            char[] paramId = "WPNAV_SPEED".ToCharArray();
            Array.Resize(ref paramId, 16);

            var paramSet = new UasParamSet
            {
                ParamId = paramId,
                ParamValue = 345.67f
            };

            _mMavLink.SendMessage(paramSet);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            char[] paramId = "FLTMODE6".ToCharArray();
            Array.Resize(ref paramId, 16);

            //Get Parameter
            unchecked
            {
                var paramgGet = new UasParamRequestRead
                {
                    ParamId = paramId,
                    ParamIndex = (short)65535,
                };
                _mMavLink.SendMessage(paramgGet);
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            var msg = new UasDigicamControl {Shot = 1};
            _mMavLink.SendMessage(msg);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            var msg = _mMavLink.UavState.Get("FLTMODE6");
            DumpMsgMeta(msg);
        }

        private void button21_Click(object sender, EventArgs e)
        {
            // 下载日志
            var msg = new UasLogRequestList
            {
                Start = 0,
                End = 0xFFFF
            };

            _mMavLink.SendMessage(msg);
        }

        private void button22_Click(object sender, EventArgs e)
        {
            var msg = new UasLogRequestData
            {
                Id = 4,
                Count = 100,
                Ofs = 0
            };

            _mMavLink.SendMessage(msg);
        }

        private void button23_Click(object sender, EventArgs e)
        {
            var msg = new UasLogErase();
            _mMavLink.SendMessage(msg);
        }
    }

}
