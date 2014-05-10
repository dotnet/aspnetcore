using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace MusicStore.Models
{
    public class Genre
    {
        public Genre()
        {
            Albums = new List<Album>();
        }

        public int GenreId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [JsonIgnore]
        public virtual ICollection<Album> Albums { get; set; }
    }
}