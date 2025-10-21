using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car.Shared
{
   public sealed record CarModel( // sealed to prevent inheritance and record to get value-based equality and immutability
    
        DateTimeOffset Timestamp,
        int Rpm,              //800 - 6000
        double SpeedKph,      //0 - 120
        double FuelPercent,   //0-100
        double EngineTempC    //80 - 100
    );
}
