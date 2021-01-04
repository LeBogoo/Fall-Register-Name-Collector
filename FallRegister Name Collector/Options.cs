using System.Runtime.Serialization;

namespace FallRegister_Name_Collector
{
    [DataContract]
    internal class Options
    {
        [DataMember]
        public string userToken;
        [DataMember]
        public bool hideConsole;
        [DataMember]
        public bool setupDone;
    }
}
