using System;
using System.Globalization;

namespace WebApplication2.Data.AdditionalStructs
{
    public struct LSLineInfo
    {
        public string power;
        public string ArcX;
        public string ArcY;
        public string ArcZ;
        public string StartX;
        public string StartY;
        public string StartZ;
        public string EndX;
        public string EndY;
        public string EndZ;
        public string Rad;
        public string Speed;
        public string Time;
        public string UDP;

        public LSLineInfo(string power, string arcX, string arcY, string arcZ, string startX, string startY, string startZ, string endX, string endY, string endz, string rad, string speed, string time, string udp="")
        {
            try
            {
                this.power = power;
                ArcX = arcX;
                ArcY = arcY;
                ArcZ = arcZ;
                StartX = startX;
                StartY = startY;
                StartZ = startZ;
                EndX = endX;
                EndY = endY;
                EndZ = endz;
                Rad = rad;
                Speed = speed+0;
                Time = time;
                UDP = udp;
            }
            catch(Exception e)
            {
               
                this.power = power;
                ArcX = arcX;
                ArcY = arcY;
                ArcZ = arcZ;
                StartX = startX;
                StartY = startY;
                StartZ = startZ;
                EndX = endX;
                EndY = endY;
                EndZ = endz;
                Rad = rad;
                Speed = speed;
                Time = time;
                UDP = udp;
            }
        }
        private float CalkDist(LSLineInfo line)
        {
            var dist =Math.Sqrt(Math.Pow(getFloat(line.EndX)- getFloat(StartX),2)+ Math.Pow(getFloat(line.EndY) - getFloat(StartY), 2)+ Math.Pow(getFloat(line.EndZ) - getFloat(StartZ), 2)) ;
            return getFloat(dist);
        }
        public bool comparePrevPoint(LSLineInfo line, bool SkipSmal = false, int skipDist=0)
        {

            if (getFloat(line.EndX) == getFloat(StartX) && getFloat(line.EndY) == getFloat(StartY) && getFloat(line.EndZ) == getFloat(StartZ))
                return true;            
            else
            if (CalkDist(line) <= skipDist && SkipSmal)
                return true;
            return false;
        }
        public float getFloat(string param )
        {
            //    if (param.Contains("--"))
            //    {
            //       param=param.Substring(1);
            //    }
            return float.Parse(param, CultureInfo.InvariantCulture.NumberFormat);
        }
        public float getFloat(double param)
        {
            return (float)param;
        }
    }
}