using System;
using System.Collections.Generic;
using System.Text;

namespace DmvAppointmentScheduler
{
    public class Teller
    {
        public string id { get; set; }
        public string specialtyType { get; set; }
        public string multiplier { get; set; }
    }

    public class TellerList
    {
        public List<Teller> Teller { get; set; }
    }
}
