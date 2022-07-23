using System;
using System.IO;
using WebApplication2.Data.Convertor.Convertors;
using WebApplication2.Data.Interfaces;
using WebApplication2.Data.Models;

namespace WebApplication2.Data.Convertor
{
    public class ParceFactory: IParserFactory
    {
        public void Parce(ParceReqestModel inputParceReqestModel)
        {

            ITranslator translator = Path.GetExtension(inputParceReqestModel.InputFilePath) switch
            {
                ".gcode" => new ConverterGcode(inputParceReqestModel),
                 ".lsr" => new ConverterPowerMill(inputParceReqestModel),
                _ => throw new FormatException("Source file with such extension is not supported")
            };
            
            translator.Process();
        }
    }
}