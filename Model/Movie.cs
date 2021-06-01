using System.Collections.Generic;

namespace MoviesDotNetCore.Model
{
    public class Movie
    {
        public string Title { get; }

        public IEnumerable<Person> Cast { get; }

        public long? Released { get;  }

        public string Tagline { get; }

        public long? Votes { get; }

        public Movie(string title, IEnumerable<Person> cast = null, long? released = null, string tagline = null, long? votes = null)
        {
            Title = title;
            Cast = cast;
            Released = released;
            Tagline = tagline;
            Votes = votes;
        }
    }
}
