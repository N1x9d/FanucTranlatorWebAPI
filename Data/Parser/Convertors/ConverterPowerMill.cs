using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using WebApplication2.Data.AdditionalStructs;
using WebApplication2.Data.Interfaces;
using WebApplication2.Data.Models;

namespace WebApplication2.Data.Convertor.Convertors
{
    
    
    public class ConverterPowerMill : ITranslator
    {
        private LSLineInfo lineLSR, prevLine;
        private coordinates _current, _previous;
        private gcode_variable _gcode;
        private int _pointcount;
        private List<string> header;
        private List<string> footer;
        private List<string> header2;
        private List<string> footer2;
        private bool _closed;
        private int _filepart;
        private bool _movement;
        private int _arcenabled;
        private coords coord = new coords();
        private positioner_variable positioner;
        private string _cUDP = "";
        public List<point> LayerPoints = new List<point>();

        public ConverterPowerMill(ParceReqestModel inputData)
        {
            this.InputData = inputData;
            header = new List<string>();
            footer = new List<string>();
            header2 = new List<string>();
            footer2 = new List<string>();
        }

        // не работает
        void coord_rotate(float angle_x, float angle_y, float angle_z)
        {
            Vector3 p = new Vector3(coord.x, coord.y, coord.z);
            Vector3 a = new Vector3((float) 0.0, (float) 0.0, (float) -150.0);
            Matrix4X4 m = new Matrix4X4();

            p -= a;
            m.Rotate(angle_z, new Vector3(0, 0, 1));
            m.Rotate(-angle_x, new Vector3(1, 0, 0));
            m.Rotate(-angle_y, new Vector3(0, 1, 0));
            p = TransformNormal(p, m);
            p += a;

            coord.x = (float) p.X;
            coord.y = (float) p.Y;
            coord.z = (float) p.Z;

            coord.w += angle_x;
            coord.p += angle_y;
            coord.r += -angle_z;
        }

        public Vector3 TransformNormal(Vector3 normal, Matrix4X4 matrix)
        {
            return new Vector3
            {
                X = normal.X * matrix[1, 1] + normal.Y * matrix[2, 1] + normal.Z * matrix[3, 1],
                Y = normal.X * matrix[1, 2] + normal.Y * matrix[2, 2] + normal.Z * matrix[3, 2],
                Z = normal.X * matrix[1, 3] + normal.Y * matrix[2, 3] + normal.Z * matrix[3, 3]
            };
        }

        void tool_rotate()
        {
            //LineF line;
            //line.setLine(_previous.x, _previous.y, _current.x, _current.y);
            //float angle = line.angle();
            //positioner.j2 = -(angle - 180);
        }

        
        private string conv;

        public string Conv
        {
            get { return conv.ToString(myCIintl); }
            set
            {
                conv = value;
                if (conv.Contains(","))
                    conv = conv.Replace(",", ".");
                if (!conv.Contains("."))
                    conv += ".0";
            }
        }

        CultureInfo myCIintl = new CultureInfo("es-ES", true);
        bool Prevstart = false;
        bool PrevEnd = false;
        coords prevP = new coords();
        coords curP = new coords();
        float prevX, prevY = 0;

        void RobotAddMoveLinear()
        {
            string termination = "";

            coord.x = _current.x;
            coord.y = _current.y;
            coord.z = _current.z;
            coord.w = _current.a;
            coord.p = _current.b;
            coord.r = _current.c;

            if (_current.states.Contains("p")) termination = InputData.Tn;
            if (_current.states.Contains("P")) termination = InputData.Tw;

            if (!InputData.ArcDisable)
            {
                if (InputData.AutoArc)
                {
                    if (_current.states.Contains("p") && _previous.states.Contains("P"))
                    {
                        header.Add(": Arc End[1];");
                        if (InputData.RO)
                            header.Add(": RO[1]=OFF;");
                        if (InputData.Wave)
                            header.Add($": Weave End[{InputData.WaveVal}];");
                        PrevEnd = true;
                    }

                    if (_current.states.Contains("P") && _previous.states.Contains("p"))
                    {
                        header.Add(": Arc Start[1];");
                        if (InputData.RO)
                            header.Add(": RO[1]=ON;");
                        if (InputData.Wave)
                            header.Add($": Weave Sine[{InputData.WaveVal}];");
                        Prevstart = true;
                    }
                }

                if (_arcenabled == 1)
                {
                    header.Add(": Arc Start[1];");
                    if (InputData.Wave)
                        header.Add(": RO[1]=ON;");
                    if (InputData.Wave)
                        header.Add(": Weave Sine[2];");
                    _arcenabled = 0;
                    Prevstart = true;
                }

                if (_arcenabled == 2)
                {
                    header.Add(": Arc End[1];");
                    if (InputData.Wave)
                        header.Add(": RO[1]=OFF;");
                    if (InputData.Wave)
                        header.Add($": Weave End[{InputData.WaveVal}];");

                    _arcenabled = 0;
                    PrevEnd = true;
                }
            }

            float y = 180f + (getAngle(coord.r, coord.p));
            float x = (getAngle(coord.w, coord.r) + 90);
            if (y > 180)
                y -= 360;
            else if (y < -180)
                y += 360;
            if (x > 180)
                x -= 360;
            else if (x < -180)
                x += 360;
            bool _isNeed = false;
            if ((Math.Abs(prevY - y) > InputData.DefDegree || Math.Abs(prevX - x) > InputData.DefDegree) &&
                InputData.WieldShield)
            {
                header.Add(": Arc start [2];");
                _isNeed = true;
            }
            else if (_isNeed && InputData.WieldShield)
            {
                header.Add(": Arc Start[1];");
                if (InputData.Wave)
                    header.Add(": RO[1]=ON;");
                if (InputData.Wave)
                    header.Add($": Weave Sine[{InputData.WaveVal}];");
                _arcenabled = 0;
                Prevstart = true;
            }

            _pointcount++;
            if (_movement)
            {
                string line = _pointcount + ": L P[" + _pointcount + "] ";


                float value;
                value = _current.feedrate * (float) Convert.ToDouble(InputData.Speed);
                int feed = (int) Math.Round(value);

                if (InputData.WeldSpeed && (_current.states.Contains("A") || _current.states.Contains("P")))
                {
                    line += "WELD_SPEED ";
                }
                else
                {
                    line += 240;
                    line += InputData.EditUnit;
                    line += " ";
                }

                line += termination;
                line += " ";
                line += InputData.EditConfig;
                line += " ;";
                header.Add(line);
                //header2.Add(line);
                if (_cUDP != "")
                {
                    var udpVal = _cUDP.Substring(3);
                    udpVal = udpVal.Replace(',', '.');
                    header.Add($": Arc Start[{udpVal}];");
                    _cUDP = "";
                }

                line = "";
                footer.Add("P[" + _pointcount + "] {");
                footer.Add("   GP1:");
                line = "       UF : " + InputData.Uf + ", UT : " + InputData.Ut +
                       ",     CONFIG: 'N U T, 0, 0, 0',";
                footer.Add(line);
               
                tool_rotate();


                coord_rotate(positioner.j1, 0, positioner.j2);


                value = InputData.X + coord.x;
                Conv = Math.Round(value, 1).ToString(myCIintl);
                line = "      X = " + Conv + " mm, ";
                value = InputData.Y + coord.y;
                Conv = Math.Round(value, 1).ToString(myCIintl);
                line += "Y = " + Conv + " mm, ";
                value = InputData.Z + coord.z;
                Conv = Math.Round(value, 1).ToString(myCIintl);
                line += "Z = " + Conv + " mm,";
                footer.Add(line);
                
                line = "";
                value = InputData.W+ y;
                Conv = Math.Round(value, 1).ToString(myCIintl);
                line = "      W = " + Conv + " deg, ";
                value = InputData.P + x;
                Conv = Math.Round(value, 1).ToString(myCIintl);
                line += "P = " + Conv + " deg, ";
                value = InputData.R + (0);
                Conv = Math.Round(value, 1).ToString(myCIintl);
                line += "R = " + Conv + " deg ";
                footer.Add(line);
              
               

                footer.Add("   GP2:");
                footer.Add("       UF : 1, UT : 2,");
               
                value = positioner.j1;
                Conv = Math.Round(value, 1).ToString(myCIintl);
                if (!Conv.Contains(".")) Conv += ".0";
                line = "      J1 = " + Conv + " deg, ";

                value = positioner.j2;
                Conv = Math.Round(value, 1).ToString(myCIintl);
                if (!Conv.Contains(".")) Conv += ".0";
                line += "J2 = " + Conv + " deg ";

                footer.Add(line);
                footer.Add("};");
               
            }
        }
        private float getAngle(float x, float y)
        {
            var a = Math.Atan2(y, x);
            a = a * 180 / Math.PI;
            return (float) (a);
        }

        private bool checkStartsStops(coords p1, coords p2)
        {
            if (InputData.Chechs)
            {
                var dist = Math.Pow((p1.x - p2.x), 2);
                dist += Math.Pow((p1.y - p2.y), 2);
                dist += Math.Pow((p1.z - p2.z), 2);
                dist = Math.Sqrt(dist);
                if (dist <= InputData.CheckDist)
                    return true;
            }

            return false;
        }

        void robot_flush_to_file()
        {
            string outname = Path.GetFileNameWithoutExtension(InputData.InputFilePath) + "_" + _filepart;

            if (!System.IO.Directory.Exists(InputData.OutputFile))
            {
                Directory.CreateDirectory(InputData.OutputFile);
            }

            StreamWriter sw = new StreamWriter(InputData.OutputFile + outname + ".ls");
            //FileStream sw1 = new FileStream(_form.OutputFile + outname + ".ls", FileMode.Create);
            //StreamWriter sw = new StreamWriter(sw1, System.Text.Encoding.UTF8);

            List<string> starter = new List<string>();

            string line = "/PROG " + Path.GetFileName(InputData.InputFilePath);
            if (_filepart > 0) line += "_" + _filepart;
            starter.Add(line);
            starter.Add("/ATTR");
            starter.Add("OWNER       = MNEDITOR;");
            starter.Add("CREATE      = DATE 100-11-20  TIME 09:43:21;");
            starter.Add("MODIFIED    = DATE 100-12-05  TIME 05:26:29;");
            //if (_form.SecondPass)
            //    line = "LINE_COUNT = " + _pointcount * 2 + ";";
            //else
            line = "LINE_COUNT = " + _pointcount + ";";
            starter.Add(line);
            starter.Add("PROTECT     = READ_WRITE;");
            starter.Add("TCD:  STACK_SIZE    = 0,");
            starter.Add("      TASK_PRIORITY = 50,");
            starter.Add("      TIME_SLICE    = 0,");
            starter.Add("      BUSY_LAMP_OFF = 0,");
            starter.Add("      ABORT_REQUEST = 0,");
            starter.Add("      PAUSE_REQUEST = 0;");
            starter.Add("DEFAULT_GROUP   = 1,1,*,*,*;");
            starter.Add("CONTROL_CODE    = 00000000 00000000;");
            starter.Add("/MN");

            for (int i = 0; i < starter.Count; ++i)
                sw.WriteLine(starter[i]);
            for (int i = 0; i < header.Count; ++i)
                sw.WriteLine(header[i]);
            //if (_form.SecondPass)
            //{
            //    for (int i = 0; i < header.Count; ++i)
            //        sw.WriteLine(header2[i]);
            //}
            if (!_closed)
            {
                var numb = _filepart + 1;
                //  line = ": CALL " + _form.FName + "_" + numb + ";";
                // sw.WriteLine(line);
            }

            sw.WriteLine(": Arc End[1];");
            sw.WriteLine("/POS");
            for (int i = 0; i < footer.Count; ++i)
            {
                sw.WriteLine(footer[i]);
            }
            //if (_form.SecondPass)
            //{
            //    for (int i = 0; i < footer.Count; ++i)
            //    {
            //        sw.WriteLine(footer2[i]);

            //    }
            //}
            sw.WriteLine("/END");

            sw.Close();

            header.Clear();
            footer.Clear();

            _filepart++;
        }

        private bool ferstLine = true;

        private LSLineInfo PrevLine;

        //обработка gcode(lsr from powermill)
        void gcode_process(string line)
        {
            try
            {
                prevLine = lineLSR;
                string lin = line;
                string[] a = lin.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (lin.Contains("UDP"))
                    lineLSR = new LSLineInfo(a[0], a[1], a[2], a[3], a[4], a[5], a[6], a[7], a[8], a[9], a[10], a[11],
                        a[13], a[12]);
                else
                    lineLSR = new LSLineInfo(a[0], a[1], a[2], a[3], a[4], a[5], a[6], a[7], a[8], a[9], a[10], a[11],
                        a[12]);

                //if (_gcode.command == "M" && (_gcode.commandvalue == 800))
                //{
                //    //Positioner move
                //    if (_gcode.flags.Contains("a"))
                //    {
                //        positioner.j1 = _gcode.a;
                //    }

                //    if (_gcode.flags.Contains("b"))
                //    {
                //        positioner.j2 = _gcode.b;
                //    }
                //}

                if (ferstLine)
                {
                    // Linear move
                    _previous = _current;
                    _movement = false;
                    _current.x = lineLSR.getFloat(lineLSR.StartX);
                    _current.y = lineLSR.getFloat(lineLSR.StartY);
                    _current.z = lineLSR.getFloat(lineLSR.StartZ);
                    _current.a = lineLSR.getFloat(lineLSR.ArcX);
                    _current.b = lineLSR.getFloat(lineLSR.ArcY);
                    _current.c = lineLSR.getFloat(lineLSR.ArcZ);
                    _movement = true;
                    _current.feedrate = (int) Math.Round(lineLSR.getFloat(lineLSR.Speed));

                    if (InputData.AutoArc)
                    {
                        _current.states = "p";
                    }

                    point c;
                    PrevLine = lineLSR;
                    c.coord = _current;
                    c.movment = _movement;
                    c.positioner = positioner;
                    LayerPoints.Add(c);
                    RobotAddMoveLinear();
                    ferstLine = false;
                }
                else if (lineLSR.comparePrevPoint(PrevLine, InputData.Chechs, Convert.ToInt32(InputData.CheckDist)))
                {
                    _cUDP = prevLine.UDP;
                    try
                    {
                        _previous = _current;
                        _movement = false;
                        _current.x = lineLSR.getFloat(lineLSR.StartX);
                        _current.y = lineLSR.getFloat(lineLSR.StartY);
                        _current.z = lineLSR.getFloat(lineLSR.StartZ);
                        _current.a = lineLSR.getFloat(lineLSR.ArcX);
                        _current.b = lineLSR.getFloat(lineLSR.ArcY);
                        _current.c = lineLSR.getFloat(lineLSR.ArcZ);
                        _movement = true;
                        _current.feedrate = (int) Math.Round(lineLSR.getFloat(lineLSR.Speed));
                    }
                    catch (Exception e)
                    {
                       
                    }

                    if (InputData.AutoArc)
                    {
                        _current.states = "P";
                    }

                    point c;
                    PrevLine = lineLSR;
                    c.coord = _current;
                    c.movment = _movement;
                    c.positioner = positioner;
                    LayerPoints.Add(c);
                    RobotAddMoveLinear();
                }
                else
                {
                    _cUDP = PrevLine.UDP;
                    _previous = _current;
                    _movement = false;
                    _current.x = PrevLine.getFloat(PrevLine.EndX);
                    _current.y = PrevLine.getFloat(PrevLine.EndY);
                    _movement = true;
                    _current.z = PrevLine.getFloat(PrevLine.EndZ);
                    _current.a = lineLSR.getFloat(lineLSR.ArcX);
                    _current.b = lineLSR.getFloat(lineLSR.ArcY);
                    _current.c = lineLSR.getFloat(lineLSR.ArcZ);
                    _current.feedrate = (int) Math.Round(lineLSR.getFloat(PrevLine.Speed));

                    if (InputData.AutoArc)
                    {
                        _current.states = "P";
                    }

                    point c;
                    c.coord = _current;
                    c.movment = _movement;
                    c.positioner = positioner;
                    LayerPoints.Add(c);
                    RobotAddMoveLinear();

                    _previous = _current;
                    _movement = false;
                    _current.x = lineLSR.getFloat(lineLSR.StartX);
                    _current.y = lineLSR.getFloat(lineLSR.StartY);
                    _current.z = lineLSR.getFloat(lineLSR.StartZ);
                    _current.a = lineLSR.getFloat(lineLSR.ArcX);
                    _current.b = lineLSR.getFloat(lineLSR.ArcY);
                    _current.c = lineLSR.getFloat(lineLSR.ArcZ);
                    _movement = true;
                    _current.feedrate = (int) Math.Round(lineLSR.getFloat(lineLSR.Speed));

                    if (InputData.AutoArc)
                    {
                        _current.states = "p";
                    }

                    c.coord = _current;
                    c.movment = _movement;
                    c.positioner = positioner;
                    LayerPoints.Add(c);
                    RobotAddMoveLinear();
                    if (_pointcount >= (float) Convert.ToDouble(InputData.SplitLayers) && !InputData.AutoSplitLayer)
                    {
                        robot_flush_to_file();
                        _pointcount = 0;
                    }

                    PrevLine = lineLSR;
                    //_previous = _current;
                    //_movement = false;
                    //_current.x = lineLSR.getFloat(lineLSR.EndX);
                    //_current.y = lineLSR.getFloat(lineLSR.EndY);
                    //_current.z = lineLSR.getFloat(lineLSR.EndZ);
                    //_movement = true;
                    //_current.feedrate = (int)Math.Round(lineLSR.getFloat(lineLSR.Speed));

                    //if (_form.AutoArc)
                    //{
                    //    _current.states = "P";
                    //}

                    // robot_add_move_linear();
                }
            }
            catch (Exception e)
            {
                _current.states = "p";
                ferstLine = true;
                
            }
        }


        public ParceReqestModel InputData { get; set; }

        public void Process()
        {
            _current.a = 0;
            _current.b = 0;
            _current.c = 0;
            _current.x = 0;
            _current.y = 0;
            _current.z = 0;
            _current.e = 0;
            _current.feedrate = 0;
            _current.states = "p";
            point c;
            c.movment = false;
            c.coord = _current;

            positioner.j1 = 0;
            positioner.j2 = 0;
            c.positioner = positioner;
            LayerPoints.Add(c);
            _previous = _current;
            header.Clear();
            footer.Clear();
            _pointcount = 0;

            string inputFile = InputData.InputFilePath;
            _closed = false;
            _filepart = 0;

            StreamReader sr = new StreamReader(inputFile);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                if (!line.Contains("#"))
                {
                    gcode_process(line);

                    if (_pointcount >= (float) Convert.ToDouble(InputData.SplitLayers) && !InputData.AutoSplitLayer)
                    {
                        robot_flush_to_file();
                        _pointcount = 0;
                    }
                }
            }

            sr.Close();
            robot_flush_to_file();
            _closed = true;
            ferstLine = true;
            string outDir = InputData.OutputFile.Substring(0, InputData.OutputFile.LastIndexOf(Path.PathSeparator));
            outDir = outDir.Substring(0, outDir.LastIndexOf(Path.PathSeparator) + 1) + "layer";
            // if (_form.AutoSplitLayer)
            // {
            //     ProcessStartInfo psipy = new ProcessStartInfo();
            //     psipy.CreateNoWindow = false;
            //     psipy.WindowStyle = ProcessWindowStyle.Normal;
            //     string cmdString = @$"/k ""python Scrypts\Slicer.py {_form.OutputFile} {outDir}""";
            //     
            //     //cmdString += outDir;
            //     if (_form.LaserPass)
            //         cmdString += " d";
            //     Process Slice = new Process();
            //     psipy.FileName = "cmd";
            //     psipy.Arguments = cmdString;
            //     Slice.StartInfo = psipy;
            //     Slice.Start();
            // }
            //     MessageBox.Show("done");
            //     Process PrFolder = new Process();
            //     ProcessStartInfo psi = new ProcessStartInfo();
            //     string file = _form.OutputFile;
            //     psi.CreateNoWindow = true;
            //     psi.WindowStyle = ProcessWindowStyle.Normal;
            //     psi.FileName = "explorer";
            //     psi.Arguments = @"/n, /select, " + outDir;
            //     PrFolder.StartInfo = psi;
            //     PrFolder.Start();
        }
    }
}