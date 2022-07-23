namespace WebApplication2.Data.Models
{
    public class ParceReqestModel
    {
        //File settings
        public string InputFilePath { get; set; }
        public string OutputFile { get; set; }

        public string EditConfig { get; set; } //Additional Config String
        public string EditUnit { get; set; } // Система измерения скорасти (def cm/min)

        //чекбоксы во втором столбце
        public bool Chechs { get; set; } //check_startStop_Distance

        public uint CheckDist { get; set; } // дистанция в милиметрах

        //чекбоксы в третьем столбце
        public bool WieldShield { get; set; }
        public bool RO { get; set; }
        public bool Wave { get; set; }
        
        public uint WaveVal { get; set; }
        public uint DefDegree { get; set; }

        // code offset 
        public float W { get; set; }
        public float P { get; set; }
        public float R { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        public float Z { get; set; }

        //Layers split
        public bool LaserPass { get; set; }
        public bool AutoSplitLayer { get; set; }

        public int SplitLayers { get; set; }

        //чекбоксы в первом столбце
        public bool WeldSpeed { get; set; }
        public bool AutoArc { get; set; }
        public bool ArcDisable { get; set; }


        //Default frame
        public uint Uf { get; set; }
        public uint Ut { get; set; }

        public uint Speed { get; set; }

        public string Tw { get; set; }
        public string Tn { get; set; }
    }
}