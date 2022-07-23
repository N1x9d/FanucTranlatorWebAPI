using WebApplication2.Data.Models;

namespace WebApplication2.Data.Interfaces
{
    public interface ITranslator
    {
        public ParceReqestModel InputData { get; set; }
        public void Process();
    }
}