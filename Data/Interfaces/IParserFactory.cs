using WebApplication2.Data.Models;

namespace WebApplication2.Data.Interfaces
{
    public interface IParserFactory
    {
        public void Parce(ParceReqestModel inputParceReqestModel);
    }
}