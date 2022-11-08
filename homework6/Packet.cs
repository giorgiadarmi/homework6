using System;

namespace homework6
{
    internal class Packet
    {
        public int number;
        public double time;
        public string source;
        public string destination;
        public string protocol;
        public int length;
        public string info;

        public Packet(int no, double t, string sa, string da, string pr, int len, string inf)
        {
            number = no;
            time = t;
            source = sa;
            destination = da;
            protocol = pr;
            length = len;
            info = inf;
        }
    }
}