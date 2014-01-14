// -----------------------------------------------------------------------
// <copyright file="Tick.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// Holds data of a single tick (i.e. a single trade).

using System;

namespace QDMSServer
{
    public struct Tick
    {
        public DateTime DT;
        public decimal Price;
        public int Volume;

        public Tick(DateTime dt, decimal price, int volume)
        {
            DT = dt;
            Price = price;
            Volume = volume;
        }
    }
}
