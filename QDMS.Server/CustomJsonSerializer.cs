// -----------------------------------------------------------------------
// <copyright file="CustomJsonSerializer.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;

namespace QDMS.Server
{
    public class CustomJsonSerializer : JsonSerializer
    {
        public CustomJsonSerializer()
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects;
        }
    }
}
