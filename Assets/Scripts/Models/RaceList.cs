using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.Models
{
    public class RaceList
    {
        public Race[] data { get; set; }
        public string result { get; set; }
    }

    public class Race
    {
        public string created_at { get; set; }
        public object ended_at { get; set; }
        public string id { get; set; }
        public string lobby_name { get; set; }
        public string room_name { get; set; }
        public object started_at { get; set; }
        public string status { get; set; }
        public string track_id { get; set; }
        public int track_level_id { get; set; }
    }
}




