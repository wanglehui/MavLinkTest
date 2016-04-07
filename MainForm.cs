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
                SerialPortName = "COM10",                
                MavlinkSystemId = 0xFF,
                MavlinkComponentId = 0xBE
            };


            _mMavLink.OnPacketReceived += OnMavLinkPacketReceived;
            _mMavLink.Initialize();
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
            UasMessageMetadata md = m.GetMetadata();

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

            foreach (UasEnumEntryMetadata entry in UasSummary.GetEnumMetadata("MavCmd").Entries)
            {
                if (command != entry.Value) continue;

                return entry.Params[paramIndex - 1];
            }

            return "";
        }

        private void PrintStandardMessage(UasMessage m)
        {
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
            else if (m is UasHeartbeat)
            {
                var hb = (UasHeartbeat) m;
                WL("Heartbeat: {0} {1}", hb.Autopilot, hb.Type);
            }
            else if (m is UasMissionItem || m is UasMissionCount || m is UasMissionRequest)
            {
                DumpMsgMeta(m);
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
            var cmd = new UasCommandLong {Command = MavCmd.GetHomePosition};

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
                ReqStreamId = 0,
                ReqMessageRate = 100,
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
            var msg = new UasSetHomePosition {Latitude = 0};

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
            _mMavLink.SendMessage(msg1);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            var msg = new UasMissionCount {Count = 2};
            _mMavLink.SendMessage(msg);

            var msg2 = new UasMissionItem
            {
                Seq = 0,
                Command = MavCmd.NavTakeoff
            };
            _mMavLink.SendMessage(msg2);

            Thread.Sleep(500);
            msg2 = new UasMissionItem
            {
                Seq = 1,
                Command = MavCmd.NavWaypoint
            };
            _mMavLink.SendMessage(msg2);
        }
    }

}
