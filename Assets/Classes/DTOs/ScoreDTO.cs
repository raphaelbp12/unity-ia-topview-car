using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Classes.DTOs
{
    public class ScoreDTO
    {
        public int Id { get; set; }
        public int TrackId { get; set; }
        public float Value { get; set; }
        public int Genereation { get; set; }
    }
}
